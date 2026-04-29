using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ChartersOfTrade.WorldGen.Core;

public sealed record WorldGenConfig(
    int Seed,
    int Width = 32,
    int Height = 24,
    int SettlementCount = 8,
    string WorldGenVersion = "0.1.0");

public sealed record TerrainCell(int X, int Y, double Height, double Fertility, double Moisture, bool IsWater);

public sealed record WorldNode(
    string Id,
    string Kind,
    int X,
    int Y,
    string Region,
    IReadOnlyList<string> Resources,
    double SettlementScore);

public sealed record WorldEdge(
    string Id,
    string FromNode,
    string ToNode,
    string Mode,
    double Distance,
    double MovementCost,
    int CapacityPerDay);

public sealed record GeneratedWorld(
    int Seed,
    string WorldGenVersion,
    int Width,
    int Height,
    IReadOnlyList<TerrainCell> Terrain,
    IReadOnlyList<WorldNode> Nodes,
    IReadOnlyList<WorldEdge> Edges,
    string Hash,
    bool HasSolvencyKernel);

public static class WorldHasher
{
    public static string Compute(
        WorldGenConfig config,
        IEnumerable<TerrainCell> terrain,
        IEnumerable<WorldNode> nodes,
        IEnumerable<WorldEdge> edges)
    {
        var builder = new StringBuilder();
        builder.Append(config.WorldGenVersion).Append('|').Append(config.Seed).Append('|');

        foreach (var cell in terrain.OrderBy(cell => cell.Y).ThenBy(cell => cell.X))
        {
            builder
                .Append(cell.X).Append(',').Append(cell.Y).Append(':')
                .Append(cell.Height.ToString("0.0000", CultureInfo.InvariantCulture)).Append(':')
                .Append(cell.Fertility.ToString("0.0000", CultureInfo.InvariantCulture)).Append(':')
                .Append(cell.Moisture.ToString("0.0000", CultureInfo.InvariantCulture)).Append(':')
                .Append(cell.IsWater ? '1' : '0')
                .Append(';');
        }

        builder.Append('|');

        foreach (var node in nodes.OrderBy(node => node.Id, StringComparer.Ordinal))
        {
            builder
                .Append(node.Id).Append(':')
                .Append(node.Kind).Append(':')
                .Append(node.X).Append(',').Append(node.Y).Append(':')
                .Append(node.Region).Append(':')
                .Append(string.Join(',', node.Resources.Order(StringComparer.Ordinal)))
                .Append(';');
        }

        builder.Append('|');

        foreach (var edge in edges.OrderBy(edge => edge.Id, StringComparer.Ordinal))
        {
            builder
                .Append(edge.Id).Append(':')
                .Append(edge.FromNode).Append("->").Append(edge.ToNode).Append(':')
                .Append(edge.Mode).Append(':')
                .Append(edge.Distance.ToString("0.000", CultureInfo.InvariantCulture)).Append(':')
                .Append(edge.MovementCost.ToString("0.000", CultureInfo.InvariantCulture)).Append(':')
                .Append(edge.CapacityPerDay)
                .Append(';');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
