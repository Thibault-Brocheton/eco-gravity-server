using Eco.Gameplay.Items;
using Eco.Mods.TechTree;
using Eco.Shared.Math;
using Eco.Shared.Utils;
using Eco.World;
using Eco.World.Blocks;
using Eco.World.Color;
using Integrity = (decimal overhang, decimal resistance);

namespace Gravity;

public static class GravityService
{
    public static Dictionary<WrappedWorldPosition3i, ByteColor> ChangedColorPositions = new Dictionary<WrappedWorldPosition3i, ByteColor>();
    public static Dictionary<WrappedWorldPosition3i, Integrity> WorldIntegrities = new Dictionary<WrappedWorldPosition3i, Integrity>();

    private static readonly ByteColor IntegrityOk = new ByteColor(0, 0, 255, 255);
    private static readonly ByteColor IntegrityFine = new ByteColor(0, 255, 0, 255);
    private static readonly ByteColor IntegrityWarning = new ByteColor(255, 255, 0, 255);
    private static readonly ByteColor IntegrityAlert = new ByteColor(128, 0, 128, 255);
    private static readonly ByteColor IntegrityFatal = new ByteColor(255, 0, 0, 255);


    private const decimal BreakValue = 10000;
    private const decimal UndefinedValue = -1;

    private const decimal DefaultMaxOverhang = 3;
    private const decimal DefaultMaxResistance = 6;

    private static readonly Dictionary<Type, Integrity> PhysicConfiguration = new Dictionary<Type, Integrity>()
    {
        // Adobe Default
        { typeof(AdobeItem), (3, 6) },
        // Adobe Specific
        { typeof(AdobeCubeBlock), (3, 6) },
        // HewnLog Default
        { typeof(HewnLogItem), (4, 8) },
    };

    public static void ClearColorIntegrities()
    {
        foreach (var changedColorPosition in ChangedColorPositions)
        {
            BlockColorManager.Obj.SetColor(changedColorPosition.Key, changedColorPosition.Value);
        }

        ChangedColorPositions.Clear();
    }

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

    public static void HandleBlockChange(WrappedWorldPosition3i blockPosition)
    {
        var block = World.GetBlock(blockPosition);

        if (IsEmpty(block))
        {
            Console.WriteLine($@"Handle empty block {block.GetType().Name} at {blockPosition.X},{blockPosition.Y},{blockPosition.Z}");

            WorldIntegrities.Remove(blockPosition);

            var neighbors = blockPosition.XYZNeighbors();

            foreach (var neighbor in neighbors)
            {
                var neighborBlock = World.GetBlock(neighbor);

                if (!IsSolidWallBlock(neighborBlock) || !IsConstructedBlock(neighborBlock)) continue;

                var neighborIntegrity = CalculateIntegrities(neighbor);
                var neighborPhysic = GetPhysic(neighborBlock);

                if (neighborIntegrity.overhang >= neighborPhysic.overhang || neighborIntegrity.resistance >= neighborPhysic.resistance )
                {
                    World.SetBlock(Block.Empty.GetType(), neighbor);
                    Console.WriteLine($@"Remove neighbor block {block.GetType().Name} at {neighbor.X},{neighbor.Y},{neighbor.Z}");
                }
                else
                {
                    MarkBlocks(new Dictionary<WrappedWorldPosition3i, Block> { [neighbor] = neighborBlock });
                }
            }
        }

        if (!IsSolidWallBlock(block) || !IsConstructedBlock(block))
        {
            return;
        }

        Console.WriteLine($@"Handle integrity block {block.GetType().Name} at {blockPosition.X},{blockPosition.Y},{blockPosition.Z}");

        var integrity = CalculateIntegrities(blockPosition);
        var physic = GetPhysic(block);

        if (integrity.overhang >= physic.overhang || integrity.resistance >= physic.resistance)
        {
            World.SetBlock(Block.Empty.GetType(), blockPosition);
            Console.WriteLine($@"Remove block {block.GetType().Name} at {blockPosition.X},{blockPosition.Y},{blockPosition.Z}");
        }
        else
        {
            MarkBlocks(new Dictionary<WrappedWorldPosition3i, Block> { [blockPosition] = block });
        }

        var neighbors2 = blockPosition.XYZNeighbors();
        foreach (var neighbor in neighbors2)
        {
            var neighborBlock = World.GetBlock(neighbor);

            if (!IsSolidWallBlock(neighborBlock) || !IsConstructedBlock(neighborBlock)) continue;

            var neighborIntegrity = CalculateIntegrities(neighbor);
            var neighborPhysic = GetPhysic(neighborBlock);

            if (neighborIntegrity.overhang >= neighborPhysic.overhang || neighborIntegrity.resistance >= neighborPhysic.resistance )
            {
                World.SetBlock(Block.Empty.GetType(), neighbor);
                Console.WriteLine($@"Remove neighbor block {block.GetType().Name} at {neighbor.X},{neighbor.Y},{neighbor.Z}");
            }
            else
            {
                MarkBlocks(new Dictionary<WrappedWorldPosition3i, Block> { [neighbor] = neighborBlock });
            }
        }
    }

    public static void ClearWorldIntegrities()
    {
        WorldIntegrities.Clear();
    }

    public static void DisplayWorldIntegrities()
    {
        WorldIntegrities.ForEach(kvp =>
        {
            var (pos, integrity) = kvp;
            Console.WriteLine($@"{pos.X},{pos.Y},{pos.Z} : ({integrity.overhang},{integrity.resistance})");
        });
    }

    private static Integrity CalculateIntegrities(WrappedWorldPosition3i pos, List<WrappedWorldPosition3i>? excluded = null)
    {
        // Je check le block en dessous
        pos.TryAdd(Vector3i.Down, out var underBlockPos);
        var underBlock = World.GetBlock(underBlockPos);
        Integrity underBlockIntegrities = (BreakValue, BreakValue);

        // Si c'est de la roche, je suis en 0, 1
        if (IsSolidWallBlock(underBlock) && !IsConstructedBlock(underBlock))
        {
            WorldIntegrities[pos] = (0, 1);
            Console.WriteLine($@"Under Rock -> {pos.X},{pos.Y},{pos.Z} : ({WorldIntegrities[pos].overhang};{WorldIntegrities[pos].resistance})");
            return WorldIntegrities[pos];
        }

        // Si c'est un bloc construit, je check ses intégrités, je les calcule au besoin (récursion)
        if (IsSolidWallBlock(underBlock) && IsConstructedBlock(underBlock))
        {
            underBlockIntegrities = WorldIntegrities.TryGetValue(underBlockPos, out var ubi) ? ubi : CalculateIntegrities(underBlockPos);

            // Mon bloc en dessous est un pilier, donc je suis un pilier, donc je n'ai pas d'overhang, et ma résistance est de +1
            if (underBlockIntegrities.overhang == 0)
            {
                WorldIntegrities[pos] = (0, underBlockIntegrities.resistance + 1);
                Console.WriteLine($@"Under Pilar -> {pos.X},{pos.Y},{pos.Z} : ({WorldIntegrities[pos].overhang};{WorldIntegrities[pos].resistance})");
                return WorldIntegrities[pos];
            }

            // Mon bloc en dessous n'est pas un pilier, je garde en tête que je repose sur un bloc tout de même grace à underBlockIntegrities
        }

        // Si c'est un bloc vide, rien ne se passe

        // Je check mes voisins (en retirant depuis la liste d'exclusion au besoin)
        excluded ??= [];
        var neighbors = pos.XYZNeighbors().Where(n => n.Y == pos.Y).Where(n => !excluded.Contains(n)).ToList();

        var rockNeighbors = neighbors.Select(World.GetBlock).Where(n => IsSolidWallBlock(n) && !IsConstructedBlock(n));

        if (rockNeighbors.Any())
        {
            WorldIntegrities[pos] = (0, 1);
            Console.WriteLine($@"Rock Neighbor -> {pos.X},{pos.Y},{pos.Z} : ({WorldIntegrities[pos].overhang};{WorldIntegrities[pos].resistance})");
            return WorldIntegrities[pos];
        }

        var integrityBlockNeighbors = neighbors.Where(n =>
        {
            var b = World.GetBlock(n);
            return IsSolidWallBlock(b) && IsConstructedBlock(b);
        }).ToList();

        if (integrityBlockNeighbors.Count != 0)
        {
            foreach (var integrityBlockNeighbor in integrityBlockNeighbors)
            {
                if (!WorldIntegrities.ContainsKey(integrityBlockNeighbor))
                {
                    CalculateIntegrities(integrityBlockNeighbor, excluded.Concat([pos]).ToList());
                }
            }

            var overhangResult = integrityBlockNeighbors.Min(n => WorldIntegrities[n].overhang);

            if (overhangResult == BreakValue)
            {
                WorldIntegrities[pos] = (BreakValue, BreakValue);
            }
            else
            {
                WorldIntegrities[pos] = (overhangResult + (underBlockIntegrities.overhang < BreakValue ? 0.5m : 1), underBlockIntegrities.resistance < BreakValue ? underBlockIntegrities.resistance + 1 : 1);
            }

            Console.WriteLine($@"Min Neighbor -> {pos.X},{pos.Y},{pos.Z} : ({WorldIntegrities[pos].overhang};{WorldIntegrities[pos].resistance})");
            return WorldIntegrities[pos];
        }

        if (underBlockIntegrities.overhang == BreakValue)
        {
            WorldIntegrities[pos] = (underBlockIntegrities.overhang, underBlockIntegrities.resistance);
            Console.WriteLine($@"Under Break -> {pos.X},{pos.Y},{pos.Z} : ({WorldIntegrities[pos].overhang};{WorldIntegrities[pos].resistance})");
            return WorldIntegrities[pos];
        }

        WorldIntegrities[pos] = (underBlockIntegrities.overhang + 0.5m, underBlockIntegrities.resistance + 1);
        Console.WriteLine($@"Under Block -> {pos.X},{pos.Y},{pos.Z} : ({WorldIntegrities[pos].overhang};{WorldIntegrities[pos].resistance})");
        return WorldIntegrities[pos];
    }

    public static Dictionary<WrappedWorldPosition3i, Block> GetConstruction(List<WrappedWorldPosition3i> startPositions)
    {
        var queue = new Queue<WrappedWorldPosition3i>();
        var visited = new HashSet<WrappedWorldPosition3i>();
        var construction = new Dictionary<WrappedWorldPosition3i, Block>();

        foreach (var startPosition in startPositions)
        {
            queue.Enqueue(startPosition);
            visited.Add(startPosition);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var element = World.GetBlock(current);

            if (element == null) continue;
            if (!element.Is<Wall>() || !element.Is<Solid>() || !element.Is<Constructed>()) continue;

            construction[current] = element;

            foreach (var neighbor in current.XYZNeighbors())
            {
                if (!visited.Add(neighbor)) continue;
                queue.Enqueue(neighbor);
            }
        }

        return construction;
    }

    private static Integrity GetPhysic(Block block)
    {
        Integrity? physic = PhysicConfiguration.TryGetValue(block.GetType(), out var value) ? value : null;

        if (physic is null && block is IRepresentsItem representsItem)
        {
            physic = PhysicConfiguration.TryGetValue(representsItem.RepresentedItemType, out var val) ? val : null;
        }

        physic ??= (DefaultMaxOverhang, DefaultMaxResistance);

        return (Integrity)physic;
    }

    public static void MarkBlocks(Dictionary<WrappedWorldPosition3i, Block> construction)
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
            Integrity physic = GetPhysic(block);

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
    }
}
