// Copyright (c) Strange Loop Games. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Eco.Gameplay.Gravity
{
    using Eco.Gameplay.Items;
    using Eco.Gameplay.Objects;
    using Eco.Shared.Logging;
    using Eco.Shared.Math;
    using Eco.Shared.Utils;
    using Eco.World.Blocks;
    using Eco.World;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System;

    public struct IntegrityConfig
    {
        public double Resistance;
        public double Overhang;
        public double Weight;

        public IntegrityConfig(double r, double o, double w)
        {
            this.Resistance = r;
            this.Overhang = o;
            this.Weight = w;
        }
    }

    public static class GravityService
    {
        // public static ByteColor[,,] ChangedColorPositions = new ByteColor[1,1,1];
        public static int[,,] WorldIntegrities = null!;

        private static readonly ByteColor IntegrityOk = new ByteColor(0, 0, 255, 255);
        private static readonly ByteColor IntegrityFine = new ByteColor(0, 255, 0, 255);
        private static readonly ByteColor IntegrityWarning = new ByteColor(255, 255, 0, 255);
        private static readonly ByteColor IntegrityAlert = new ByteColor(128, 0, 128, 255);
        private static readonly ByteColor IntegrityFatal = new ByteColor(255, 0, 0, 255);
        private static readonly HashSet<WrappedWorldPosition3i> ToSkip = new HashSet<WrappedWorldPosition3i>();

        /*public static void ClearColorIntegrities()
        {
            foreach (var changedColorPosition in ChangedColorPositions)
            {
                BlockColorManager.Obj.SetColor(changedColorPosition.Key, changedColorPosition.Value);
            }

            ChangedColorPositions.Clear();
        }*/

        private static bool IsRock(Block block)
        {
            return block.Is<Minable>();
        }

        private static bool IsBedRock(Block block)
        {
            return block.Is<Impenetrable>();
        }

        private static bool ShouldNotBeConsidered(Block block)
        {
            return block.Is<Transient>();
        }

        private static bool IsEmpty(Block block)
        {
            return block == Block.Empty;
        }

        public static void HandleBlockChange(WrappedWorldPosition3i blockPosition)
        {
            if (ToSkip.Contains(blockPosition))
            {
                ToSkip.Remove(blockPosition);
                if (GravityPlugin.Obj.Config.Debug) Log.WriteLineLoc($"Skipped HandleBlockChange for {blockPosition}");
                return;
            }

            var block = World.GetBlock(blockPosition);

            if (ShouldNotBeConsidered(block))
            {
                return;
            }

            if (GravityPlugin.Obj.Config.Debug) Log.WriteLineLoc($"HandleBlockChange: {blockPosition} which is {block.GetType().Name}");

            if (IsEmpty(block))
            {
                HandleBlockRemoved(blockPosition);
            }
            else
            {
                HandleBlockAdded(blockPosition, block);
            }
        }

        private static void HandleBlockRemoved(WrappedWorldPosition3i blockPosition)
        {
            WorldIntegrities[blockPosition.X, blockPosition.Y, blockPosition.Z] = -1;
            HandleNeighbors(blockPosition);
        }

        private static void HandleBlockAdded(WrappedWorldPosition3i blockPosition, Block block)
        {
            var integrity = CalculateIntegrities(blockPosition, block);

            if (integrity <= 0)
            {
                DeleteBlock(blockPosition, block);

                return;
            }

            WorldIntegrities[blockPosition.X, blockPosition.Y, blockPosition.Z] = integrity;

            HandleNeighbors(blockPosition);
        }

        private static void HandleNeighbors(WrappedWorldPosition3i startPos)
        {
            var toVisit = new Queue<(WrappedWorldPosition3i Pos, int Depth)>();
            var toVisitSet = new HashSet<WrappedWorldPosition3i>();
            var visited = new HashSet<WrappedWorldPosition3i>();

            visited.Add(startPos);
            startPos.XYZNeighbors().ForEach(n =>
            {
                toVisit.Enqueue((n, 1));
                toVisitSet.Add(n);
            });

            while (toVisit.TryDequeue(out var tuple))
            {
                var (blockPos, depth) = tuple;
                //Log.WriteLineLoc($"Dequeue {blockPos} with depth {depth}");

                toVisitSet.Remove(blockPos);
                visited.Add(blockPos);

                var block = World.GetBlock(blockPos);
                if (IsEmpty(block) || ShouldNotBeConsidered(block))
                {
                    if (GravityPlugin.Obj.Config.Debug) Log.WriteLineLoc($"{blockPos} is empty or not considered");
                    continue;
                }

                var newIntegrity = CalculateIntegrities(blockPos, block, depth);

                if (newIntegrity <= 0)
                {
                    DeleteBlock(blockPos, block);

                    // We add all neighbors to be checked again
                    foreach (var n in blockPos.XYZNeighbors())
                    {
                        visited.Remove(n);

                        // Not if they are already to be visited
                        if (toVisitSet.Contains(n)) continue;

                        // We re-start the depth from here
                        toVisit.Enqueue((n, 1));
                        toVisitSet.Add(n);
                    }

                    continue;
                }

                var previousIntegrity = WorldIntegrities[blockPos.X, blockPos.Y, blockPos.Z];

                if (previousIntegrity != newIntegrity)
                {
                    if (GravityPlugin.Obj.Config.Debug) Log.WriteLineLoc($"{blockPos} had int {previousIntegrity} and is now {newIntegrity}");

                    WorldIntegrities[blockPos.X, blockPos.Y, blockPos.Z] = newIntegrity;

                    if (previousIntegrity == -1 && depth >= GravityPlugin.Obj.Config.MaxDepth)
                    {
                        if (GravityPlugin.Obj.Config.Debug) Log.WriteLineLoc($"HandleNeighbors stop due to MaxDepth {depth}");
                    }
                    else
                    {
                        blockPos.XYZNeighbors().Where(n => !visited.Contains(n) && !toVisitSet.Contains(n)).ForEach(n =>
                        {
                            toVisit.Enqueue((n, depth + 1));
                            toVisitSet.Add(n);
                        });
                    }
                }
            }
        }

        public static void ResetWorldIntegrities(bool recalculate = false)
        {
            Log.WriteLineLoc($"ResetWorldIntegrities...");

            if (WorldIntegrities is null)
            {
                WorldIntegrities = new int[Eco.Shared.Voxel.World.WrappedVoxelSize.X, Eco.Shared.Voxel.World.WrappedVoxelSize.Y, Eco.Shared.Voxel.World.WrappedVoxelSize.Z];
            }

            for (int x = 0; x < WorldIntegrities.GetLength(0); x++)
            for (int y = 0; y < WorldIntegrities.GetLength(1); y++)
            for (int z = 0; z < WorldIntegrities.GetLength(2); z++)
                WorldIntegrities[x, y, z] = -1;

            Log.WriteLineLoc($"Reset ok");
            Log.WriteLineLoc($"End of ResetWorldIntegrities");
        }

        public static int CalculateIntegrities(WrappedWorldPosition3i pos, Block bloc, int depth = 0)
        {
            var physic = GetPhysic(bloc);
            double hDistance = GetHintDistance(pos, bloc);
            double blocksBelow = GetNumberOfBlocksBelow(pos);
            var hInt = (int)Math.Clamp((1 - (hDistance / physic.Overhang) + (blocksBelow / GravityPlugin.Obj.Config.SupportDistanceForMaxEfficiency)) * 100, 0, 100);

            double weight = GetUpWeight(pos);

            var vInt = physic.Resistance == int.MaxValue ? 100 : Math.Clamp((1 - (weight / physic.Resistance)) * 100, 0, 100);
            var integrity = hInt + vInt - 100;

            if (GravityPlugin.Obj.Config.Debug) Log.WriteLineLoc($"{new string(' ', depth * 2)}For {pos}: hDistance {hDistance}, weight {weight} so hInt {hInt} and vInt {vInt} and integrity {integrity}%");

            return (int)Math.Clamp(integrity, 0, 100);
        }

        /*
         * Is Column if one of the block under him is an infinite block (or bedrock)
         * Is not if any of the block above is empty
         */
        private static bool IsColumn(WrappedWorldPosition3i pos, bool considerRocksAreColumns = true)
        {
            do
            {
                var block = World.GetBlock(pos);
                if (IsEmpty(block) || ShouldNotBeConsidered(block)) return false;
                if (considerRocksAreColumns && IsRock(block)) break;
                if (IsBedRock(block)) break;
            } while (pos.TryDecreaseY(1, out pos));

            return true;
        }

        // By ChatGPT
        private static int GetHintDistance(WrappedWorldPosition3i startPos, Block bloc)
        {
            var dist  = new Dictionary<WrappedWorldPosition3i, int>(capacity: 1024)
            {
                [startPos] = 0
            };
            var inQ   = new HashSet<WrappedWorldPosition3i>();
            var queue = new Queue<WrappedWorldPosition3i>();
            queue.Enqueue(startPos);
            inQ.Add(startPos);

            int best = int.MaxValue;

            while (queue.Count > 0)
            {
                var pos = queue.Dequeue();
                inQ.Remove(pos);
                int dHere = dist[pos];

                if (dHere >= best) continue;

                // If the current block is a rock, we need to make sure this rock has a path to bedrock, otherwise it might be flying
                if (IsColumn(pos, !IsRock(bloc)))
                {
                    best = dHere;
                    continue;
                }

                foreach (var (n, dir) in pos.XYZNeighborsWithDirection())
                {
                    var neighborBlock = World.GetBlock(n);
                    if (IsEmpty(neighborBlock) || ShouldNotBeConsidered(neighborBlock)) continue;

                    int w = dir switch
                    {
                        Direction.Up    => 2,
                        Direction.Down  => 0, // anciennement -1
                        _               => 1
                    };

                    int cand = dHere + w;
                    if (cand >= best) continue;

                    if (!dist.TryGetValue(n, out var old) || cand < old)
                    {
                        dist[n] = cand;
                        if (inQ.Add(n))
                            queue.Enqueue(n);
                    }
                }
            }

            return best;
        }

        private static int GetNumberOfBlocksBelow(WrappedWorldPosition3i pos)
        {
            var nb = 0;

            while (pos.TryDecreaseY(1, out pos))
            {
                var block = World.GetBlock(pos);

                if (IsEmpty(block) || ShouldNotBeConsidered(block)) break;

                nb += 1;
            }

            return nb;
        }

        private static int GetUpWeight(WrappedWorldPosition3i pos)
        {
            var weight = 0d;

            while (pos.TryIncreaseY(1, out pos))
            {
                var block = World.GetBlock(pos);

                if (IsEmpty(block) || ShouldNotBeConsidered(block)) break;

                weight += GetPhysic(block).Weight;
            }

            return (int)weight;
        }

        private static IntegrityConfig GetPhysic(Block block)
        {
            IntegrityConfig? physic = GravityPlugin.Obj.Config.PhysicConfiguration.TryGetValue(block.GetType().Name, out var value) ? value : null;

            if (physic is null && block is IRepresentsItem representsItem)
            {
                physic = GravityPlugin.Obj.Config.PhysicConfiguration.TryGetValue(representsItem.RepresentedItemType.Name, out var val) ? val : null;
            }

            physic ??= new IntegrityConfig(GravityPlugin.Obj.Config.DefaultMaxResistance, GravityPlugin.Obj.Config.DefaultMaxOverhang, GravityPlugin.Obj.Config.DefaultWeight);

            IntegrityConfig? formPhysic = block.Is<IsFormAttribute>() ? GravityPlugin.Obj.Config.PhysicConfiguration.TryGetValue(block.GetType().Name, out var v) ? v : null : null;

            if (formPhysic is not null)
            {
                return new IntegrityConfig(
                    physic.Value.Resistance + formPhysic.Value.Resistance,
                    physic.Value.Overhang + formPhysic.Value.Overhang,
                    physic.Value.Weight + formPhysic.Value.Weight
                );
            }

            return (IntegrityConfig)physic;
        }

        private static void AddBlock(WrappedWorldPosition3i pos, Type blockType, bool stopPropagation = true)
        {
            if (stopPropagation) ToSkip.Add(pos);
            if (GravityPlugin.Obj.Config.Debug) Log.WriteLineLoc($"Add block {blockType} at position {pos}");
            World.SetBlock(blockType, pos);
        }

        private static void DeleteBlock(WrappedWorldPosition3i pos, Block block, bool stopPropagation = true)
        {
            if (stopPropagation) ToSkip.Add(pos);
            if (GravityPlugin.Obj.Config.Debug) Log.WriteLineLoc($"Delete position {pos}");
            WorldIntegrities[pos.X, pos.Y, pos.Z] = -1;
            World.DeleteBlock(pos);

            try
            {
                var rubbleType = block.GetType().HasAttribute<BecomesRubble>()
                    ? block.GetType()
                    : block is IRepresentsItem representsItem && representsItem.RepresentedItemType.HasAttribute<BecomesRubble>()
                        ? representsItem.RepresentedItemType
                        : World.GetBlock(WrappedWorldPosition3i.Create(pos.X, 2, pos.Z)).GetType();

                if (GravityPlugin.Obj.Config.Debug) Log.WriteLineLoc($"Rubble type {rubbleType}");

                RubbleObject.TrySpawnFromBlock(null, rubbleType, new Vector3(pos.X, pos.Y, pos.Z));
            }
            catch (Exception ex)
            {
                if (GravityPlugin.Obj.Config.Debug) Log.WriteErrorLineLoc($"Exception in rubble spawn: {ex.Message}");
            }
        }

        /*public static void MarkBlocks(Dictionary<WrappedWorldPosition3i, Block> construction)
        {
            foreach (var (location, block) in construction)
            {
                if (!WorldIntegrities.TryGetValue(location, out var integrity))
                {
                    integrity = CalculateIntegrities(location);
                }

                BlockColorManager.Obj.TryGetColorData(new Vector3i(location.X, location.Y, location.Z), out var initialColor);
                ChangedColorPositions.TryAdd(location, initialColor);

                ByteColor color;
                IntegrityConfig physic = GetPhysic(block);

                if (integrity.overhang == 0)
                {
                    color = IntegrityOk;
                }
                else if (integrity.overhang >= physic.overhang)
                {
                    color = IntegrityFatal;
                }
                else if (integrity.overhang >= physic.overhang - 1)
                {
                    color = IntegrityAlert;
                }
                else if (integrity.overhang >= physic.overhang / 2)
                {
                    color = IntegrityWarning;
                }
                else
                {
                    color = IntegrityFine;
                }

                BlockColorManager.Obj.SetColor(location, color);
            }
        }*/
    }
}
