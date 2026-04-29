namespace ChartersOfTrade.WorldGen.Core;

public sealed class WorldGenerator
{
    private static readonly string[] BasicResources = ["grain", "wood", "fish", "wool"];
    private static readonly string[] RegionalResources = ["clay", "iron"];

    public GeneratedWorld Generate(WorldGenConfig config)
    {
        if (config.Width < 8 || config.Height < 8)
        {
            throw new ArgumentException("World must be at least 8x8 tiles.", nameof(config));
        }

        var rng = new DeterministicRng((ulong)config.Seed);
        var terrain = GenerateTerrain(config, ref rng);
        var nodes = GenerateNodes(config, terrain, ref rng);
        var edges = GenerateEdges(nodes);
        var hash = WorldHasher.Compute(config, terrain, nodes, edges);
        var hasSolvencyKernel = nodes.Any(n => n.Resources.Contains("grain") || n.Resources.Contains("fish"))
            && nodes.Any(n => n.Resources.Contains("wood"))
            && edges.Count >= Math.Max(1, nodes.Count - 1);

        return new GeneratedWorld(
            config.Seed,
            config.WorldGenVersion,
            config.Width,
            config.Height,
            terrain,
            nodes,
            edges,
            hash,
            hasSolvencyKernel);
    }

    private static IReadOnlyList<TerrainCell> GenerateTerrain(WorldGenConfig config, ref DeterministicRng rng)
    {
        var cells = new List<TerrainCell>(config.Width * config.Height);

        for (var y = 0; y < config.Height; y++)
        {
            for (var x = 0; x < config.Width; x++)
            {
                var coastFactor = Math.Min(Math.Min(x, config.Width - 1 - x), Math.Min(y, config.Height - 1 - y)) / 6.0;
                var height = Clamp01((rng.NextDouble() * 0.65) + (coastFactor * 0.12));
                var moisture = Clamp01(rng.NextDouble());
                var fertility = Clamp01((1.0 - height) * 0.45 + moisture * 0.45 + rng.NextDouble() * 0.1);
                var isWater = height < 0.18 || x == 0 || y == 0 || x == config.Width - 1 || y == config.Height - 1;
                cells.Add(new TerrainCell(x, y, height, fertility, moisture, isWater));
            }
        }

        return cells;
    }

    private static IReadOnlyList<WorldNode> GenerateNodes(
        WorldGenConfig config,
        IReadOnlyList<TerrainCell> terrain,
        ref DeterministicRng rng)
    {
        var scoredLand = new List<(TerrainCell Cell, double Score)>();
        foreach (var cell in terrain.Where(cell => !cell.IsWater))
        {
            scoredLand.Add((cell, cell.Fertility + rng.NextDouble() * 0.2));
        }

        var land = scoredLand
            .OrderByDescending(candidate => candidate.Score)
            .Take(config.SettlementCount * 4)
            .Select(candidate => candidate.Cell)
            .ToList();

        var selected = new List<TerrainCell>();
        foreach (var candidate in land)
        {
            if (selected.All(existing => Distance(existing.X, existing.Y, candidate.X, candidate.Y) >= 5))
            {
                selected.Add(candidate);
            }

            if (selected.Count == config.SettlementCount)
            {
                break;
            }
        }

        while (selected.Count < config.SettlementCount)
        {
            selected.Add(land[selected.Count % land.Count]);
        }

        var nodes = new List<WorldNode>(selected.Count);
        for (var i = 0; i < selected.Count; i++)
        {
            var cell = selected[i];
            var resources = PickResources(cell, i);
            var kind = i == 0 ? "charter_town" : i % 3 == 0 ? "port" : "market_town";
            nodes.Add(new WorldNode(
                $"node_{i + 1:000}",
                kind,
                cell.X,
                cell.Y,
                RegionFor(cell, config),
                resources,
                Math.Round(cell.Fertility * 100 + resources.Count * 8, 3)));
        }

        return nodes;
    }

    private static IReadOnlyList<string> PickResources(TerrainCell cell, int index)
    {
        var resources = new List<string>();
        resources.Add(BasicResources[index % BasicResources.Length]);

        if (cell.Fertility > 0.55 && !resources.Contains("grain"))
        {
            resources.Add("grain");
        }

        if (cell.Height > 0.62)
        {
            resources.Add("iron");
        }
        else if (cell.Moisture > 0.60)
        {
            resources.Add("clay");
        }
        else
        {
            resources.Add(RegionalResources[index % RegionalResources.Length]);
        }

        return resources.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<WorldEdge> GenerateEdges(IReadOnlyList<WorldNode> nodes)
    {
        var edges = new List<WorldEdge>();
        var ordered = nodes.OrderBy(node => node.X).ThenBy(node => node.Y).ToArray();

        for (var i = 0; i < ordered.Length - 1; i++)
        {
            AddEdge(edges, ordered[i], ordered[i + 1], i);
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var nearest = nodes
                .Where(other => other.Id != nodes[i].Id)
                .OrderBy(other => Distance(nodes[i].X, nodes[i].Y, other.X, other.Y))
                .First();
            if (edges.All(edge => !Connects(edge, nodes[i], nearest)))
            {
                AddEdge(edges, nodes[i], nearest, edges.Count);
            }
        }

        return edges;
    }

    private static void AddEdge(List<WorldEdge> edges, WorldNode from, WorldNode to, int index)
    {
        var distance = Distance(from.X, from.Y, to.X, to.Y);
        var mode = from.Kind == "port" || to.Kind == "port" ? "coastal" : "road";
        var movementCost = Math.Round(distance * (mode == "coastal" ? 0.65 : 1.0), 3);
        var capacity = mode == "coastal" ? 18 : 12;
        edges.Add(new WorldEdge($"edge_{index + 1:000}", from.Id, to.Id, mode, Math.Round(distance, 3), movementCost, capacity));
    }

    private static bool Connects(WorldEdge edge, WorldNode a, WorldNode b)
    {
        return (edge.FromNode == a.Id && edge.ToNode == b.Id) || (edge.FromNode == b.Id && edge.ToNode == a.Id);
    }

    private static string RegionFor(TerrainCell cell, WorldGenConfig config)
    {
        var horizontal = cell.X < config.Width / 2 ? "west" : "east";
        var vertical = cell.Y < config.Height / 2 ? "north" : "south";
        return $"{vertical}_{horizontal}";
    }

    private static double Distance(int ax, int ay, int bx, int by)
    {
        var dx = ax - bx;
        var dy = ay - by;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double Clamp01(double value)
    {
        return Math.Max(0, Math.Min(1, value));
    }
}
