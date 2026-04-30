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

        if (config.SettlementCount < 1)
        {
            throw new ArgumentException("World must request at least one settlement.", nameof(config));
        }

        var maximumInteriorCells = Math.Max(0, (config.Width - 2) * (config.Height - 2));
        if (config.SettlementCount > maximumInteriorCells)
        {
            throw new ArgumentException(
                $"World cannot place {config.SettlementCount} settlements inside a {config.Width}x{config.Height} map with water borders.",
                nameof(config));
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
        var terrainSeed = rng.NextUInt64();

        for (var y = 0; y < config.Height; y++)
        {
            for (var x = 0; x < config.Width; x++)
            {
                var edgeDistance = Math.Min(
                    Math.Min(x, config.Width - 1 - x),
                    Math.Min(y, config.Height - 1 - y)) / (double)Math.Min(config.Width, config.Height);
                var inland = SmoothStep(0.02, 0.22, edgeDistance);
                var continental = FractalNoise(terrainSeed, x, y, 0.085, 4);
                var ridges = FractalNoise(terrainSeed ^ 0xA24BAED4963EE407UL, x + 41, y - 29, 0.19, 3);
                var height = Clamp01(0.18 + inland * 0.48 + (continental - 0.50) * 0.38 + (ridges - 0.50) * 0.18);
                var moistureField = FractalNoise(terrainSeed ^ 0x9FB21C651E98DF25UL, x - 17, y + 73, 0.12, 4);
                var fertilityField = FractalNoise(terrainSeed ^ 0xC2B2AE3D27D4EB4FUL, x + 101, y + 11, 0.16, 3);
                var isWater = height < 0.34 || x == 0 || y == 0 || x == config.Width - 1 || y == config.Height - 1;
                var moisture = Clamp01(0.24 + moistureField * 0.56 + (isWater ? 0.20 : 0.0) - Math.Max(0.0, height - 0.62) * 0.34);
                var lowland = 1.0 - Math.Abs(height - 0.46) * 1.55;
                var fertility = Clamp01(lowland * 0.45 + moisture * 0.34 + fertilityField * 0.21 - (isWater ? 0.42 : 0.0));
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
        var terrainByPoint = terrain.ToDictionary(cell => (cell.X, cell.Y));
        foreach (var cell in terrain.Where(cell => !cell.IsWater))
        {
            scoredLand.Add((cell, cell.Fertility + rng.NextDouble() * 0.2));
        }

        var land = scoredLand
            .OrderByDescending(candidate => candidate.Score)
            .Select(candidate => candidate.Cell)
            .ToList();
        if (land.Count < config.SettlementCount)
        {
            throw new InvalidOperationException($"World seed {config.Seed} generated only {land.Count} land cells for {config.SettlementCount} settlements.");
        }

        var selected = new List<TerrainCell>();
        for (var minimumDistance = 5; minimumDistance >= 3 && selected.Count < config.SettlementCount; minimumDistance--)
        {
            foreach (var candidate in land)
            {
                if (selected.Contains(candidate)
                    || selected.Any(existing => Distance(existing.X, existing.Y, candidate.X, candidate.Y) < minimumDistance))
                {
                    continue;
                }

                selected.Add(candidate);

                if (selected.Count == config.SettlementCount)
                {
                    break;
                }
            }
        }

        while (selected.Count < config.SettlementCount)
        {
            var fallback = land.FirstOrDefault(candidate => !selected.Contains(candidate));
            if (fallback is null)
            {
                throw new InvalidOperationException($"World seed {config.Seed} generated only {selected.Count} unique land cells for {config.SettlementCount} settlements.");
            }

            selected.Add(fallback);
        }

        var nodes = new List<WorldNode>(selected.Count);
        for (var i = 0; i < selected.Count; i++)
        {
            var cell = selected[i];
            var resources = PickResources(cell, i);
            var kind = i == 0 ? "charter_town" : IsCoastalCell(terrainByPoint, cell) ? "port" : "market_town";
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
        var mode = from.Kind == "port" && to.Kind == "port" ? "coastal" : "road";
        var movementCost = Math.Round(distance * (mode == "coastal" ? 0.65 : 1.0), 3);
        var capacity = mode == "coastal" ? 18 : 12;
        edges.Add(new WorldEdge($"edge_{index + 1:000}", from.Id, to.Id, mode, Math.Round(distance, 3), movementCost, capacity));
    }

    private static bool Connects(WorldEdge edge, WorldNode a, WorldNode b)
    {
        return (edge.FromNode == a.Id && edge.ToNode == b.Id) || (edge.FromNode == b.Id && edge.ToNode == a.Id);
    }

    private static bool IsCoastalCell(IReadOnlyDictionary<(int X, int Y), TerrainCell> terrainByPoint, TerrainCell cell)
    {
        return IsWater(terrainByPoint, cell.X - 1, cell.Y)
            || IsWater(terrainByPoint, cell.X + 1, cell.Y)
            || IsWater(terrainByPoint, cell.X, cell.Y - 1)
            || IsWater(terrainByPoint, cell.X, cell.Y + 1);
    }

    private static bool IsWater(IReadOnlyDictionary<(int X, int Y), TerrainCell> terrainByPoint, int x, int y)
    {
        return !terrainByPoint.TryGetValue((x, y), out var cell) || cell.IsWater;
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

    private static double FractalNoise(ulong seed, double x, double y, double frequency, int octaves)
    {
        var value = 0.0;
        var amplitude = 1.0;
        var amplitudeSum = 0.0;
        var currentFrequency = frequency;

        for (var octave = 0; octave < octaves; octave++)
        {
            value += ValueNoise(seed + (ulong)octave * 0x9E3779B97F4A7C15UL, x * currentFrequency, y * currentFrequency) * amplitude;
            amplitudeSum += amplitude;
            amplitude *= 0.52;
            currentFrequency *= 2.03;
        }

        return amplitudeSum <= 0.0 ? 0.0 : value / amplitudeSum;
    }

    private static double ValueNoise(ulong seed, double x, double y)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;
        var tx = Fade(x - x0);
        var ty = Fade(y - y0);
        var a = Lerp(HashUnit(seed, x0, y0), HashUnit(seed, x1, y0), tx);
        var b = Lerp(HashUnit(seed, x0, y1), HashUnit(seed, x1, y1), tx);
        return Lerp(a, b, ty);
    }

    private static double HashUnit(ulong seed, int x, int y)
    {
        var value = seed
            ^ ((ulong)(uint)x * 0xBF58476D1CE4E5B9UL)
            ^ ((ulong)(uint)y * 0x94D049BB133111EBUL);
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        value ^= value >> 31;
        return (value >> 11) * (1.0 / (1UL << 53));
    }

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        var t = Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3.0 - 2.0 * t);
    }

    private static double Fade(double value)
    {
        return value * value * value * (value * (value * 6.0 - 15.0) + 10.0);
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }
}
