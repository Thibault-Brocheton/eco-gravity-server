using Eco.Gameplay.Items;
using Eco.Mods.TechTree;
using Eco.Shared.Logging;
using Eco.Shared.Math;
using Eco.World;
using Eco.World.Blocks;
using Integrity = (int maxHorizontalSpan, int maxStackHeight);

namespace Gravity {
    public class GravityService
    {
        private const int DefaultMaxHorizontalSpan = 3;
        private const int DefaultMaxStackHeight = 6;

        private static readonly Dictionary<Type, Integrity> PhysicConfiguration = new Dictionary<Type, Integrity>()
        {
            // Adobe Default
            { typeof(AdobeItem), (3, 6) },
            // Adobe Specific
            { typeof(AdobeCubeBlock), (3, 6) },
            // HewnLog Default
            { typeof(HewnLogItem), (4, 8) },
        };

        private static bool IsSolidWallBlock(Block block)
        {
            return block.Is<Wall>() && block.Is<Solid>();
        }

        private static bool IsConstructedBlock(Block block)
        {
            return block.Is<Constructed>();
        }

        private static bool IsEmpty(Block block)
        {
            return block == Block.Empty;
        }

        private static Integrity GetPhysic(Block block)
        {
            Integrity? physic = PhysicConfiguration.TryGetValue(block.GetType(), out var value) ? value : null;

            if (physic is null && block is IRepresentsItem representsItem)
            {
                physic = PhysicConfiguration.TryGetValue(representsItem.RepresentedItemType, out var val) ? val : null;
            }

            physic ??= (DefaultMaxHorizontalSpan, DefaultMaxStackHeight);

            return (Integrity)physic;
        }

        public static void HandleBlockChange(WrappedWorldPosition3i position)
        {
            var block = World.GetBlock(position);

            if (IsEmpty(block))
            {
                Log.WriteLineLoc($"Empty bloc at position {position}");
                ReevaluateStructure(position);
            }
            else if (IsConstructedBlock(block))
            {
                Log.WriteLineLoc($"Constructed bloc at position {position} is now {block.GetType()}");
                ReevaluateStructure(position);
                CheckColumnOverload(position);
            }
            else
            {
                Log.WriteLineLoc($"Block at position {position} is now {block.GetType()}");
            }
        }

        private static void ReevaluateStructure(WrappedWorldPosition3i changedPosition)
        {
            Queue<WrappedWorldPosition3i> toCheck = new();
            HashSet<WrappedWorldPosition3i> alreadyChecked = new();
            toCheck.Enqueue(changedPosition);

            while (toCheck.Count > 0)
            {
                var pos = toCheck.Dequeue();
                if (alreadyChecked.Contains(pos)) continue;
                alreadyChecked.Add(pos);

                var block = World.GetBlock(pos);
                if (IsEmpty(block))
                    continue;

                if (!CheckStability(pos, block))
                {
                    DestroyBlock(pos, block);

                    foreach (var above in GetBlocksAbove(pos))
                        toCheck.Enqueue(above);

                    foreach (var adjacent in GetAdjacentHorizontal(pos))
                        toCheck.Enqueue(adjacent);
                }
            }
        }

        private static bool CheckStability(WrappedWorldPosition3i pos, Block block)
        {
            // Vérification verticale
            var physic = GetPhysic(block);

            int heightAbove = CountBlocksAbove(pos);
            if (heightAbove > physic.maxStackHeight)
                return false;

            // Vérification horizontale
            if (physic.maxHorizontalSpan > 0)
            {
                if (!HasSupportWithinSpan(pos, block))
                    return false;
            }

            return true;
        }

        private static void CheckColumnOverload(WrappedWorldPosition3i fromPosition)
        {
            var column = GetVerticalColumn(fromPosition);
            for (int i = 0; i < column.Count; i++)
            {
                var current = column[i];
                var block = World.GetBlock(current);
                if (IsEmpty(block)) continue;

                var physic = GetPhysic(block);
                int heightAbove = column.Count - i - 1;
                if (heightAbove > physic.maxStackHeight)
                {
                    for (int j = i; j < column.Count; j++)
                    {
                        var toDestroy = World.GetBlock(column[j]);
                        if (!IsEmpty(toDestroy))
                            DestroyBlock(column[j], toDestroy);
                    }
                    break;
                }
            }
        }

        private static List<WrappedWorldPosition3i> GetVerticalColumn(WrappedWorldPosition3i from)
        {
            List<WrappedWorldPosition3i> column = new();
            WrappedWorldPosition3i pos = from;

            while (!IsEmpty(World.GetBlock(pos)))
            {
                column.Insert(0, pos);
                if (!pos.TryDecreaseY(1, out pos))
                {
                    break;
                }
            }

            from.TryIncreaseY(1, out pos);
            while (!IsEmpty(World.GetBlock(pos)))
            {
                column.Add(pos);
                if (!pos.TryIncreaseY(1, out pos))
                {
                    break;
                }
            }

            return column;
        }

        private static int CountBlocksAbove(WrappedWorldPosition3i position)
        {
            var count = 0;

            while (position.TryIncreaseY(1, out position))
            {
                count++;
            }

            return count;
        }

        private static IEnumerable<WrappedWorldPosition3i> GetBlocksAbove(WrappedWorldPosition3i position)
        {
            while (position.TryIncreaseY(1, out position))
            {
                yield return position;
            }
        }

        private static IEnumerable<WrappedWorldPosition3i> GetAdjacentHorizontal(WrappedWorldPosition3i position)
        {
            yield return position.AddX(1);
            yield return position.AddX(-1);
            yield return position.AddZ(1);
            yield return position.AddZ(-1);
        }

        private static bool HasSupportWithinSpan(WrappedWorldPosition3i position, Block block)
        {
            var physic = GetPhysic(block);
            int maxSpan = physic.maxHorizontalSpan;
            WrappedWorldPosition3i basePos = position;

            for (int dx = -maxSpan; dx <= maxSpan; dx++)
            {
                for (int dz = -maxSpan; dz <= maxSpan; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    basePos.TryAdd(new Vector3i(dx, -1, dz), out WrappedWorldPosition3i supportPos);
                    var support = World.GetBlock(supportPos);

                    if (!IsEmpty(support))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void DestroyBlock(WrappedWorldPosition3i pos, Block block)
        {
            // Logique d'effondrement visuel ou suppression physique du bloc
            Log.WriteLineLoc($"[EFFONDREMENT] Bloc à {pos} de type {block.GetType()} s'effondre.");
            World.SetBlock(Block.Empty.GetType(), pos);
        }
    }
}
