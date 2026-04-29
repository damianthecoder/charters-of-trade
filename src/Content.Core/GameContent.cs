using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChartersOfTrade.Economy.Core;

namespace ChartersOfTrade.Content.Core;

public sealed record GameContent(
    IReadOnlyList<ResourceDef> Resources,
    IReadOnlyList<RecipeDef> Recipes,
    string ContentHash)
{
    public ResourceDef Resource(string id)
    {
        return Resources.First(resource => resource.Id == id);
    }
}

public sealed class ContentValidationException : Exception
{
    public ContentValidationException(IReadOnlyList<string> errors)
        : base("Content validation failed: " + string.Join("; ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}

public static class GameContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static GameContent LoadFromDirectory(string contentDirectory)
    {
        var root = Path.GetFullPath(contentDirectory);
        var resourcesPath = Path.Combine(root, "resources.p0.json");
        var recipesPath = Path.Combine(root, "recipes.p0.json");

        var resourcesJson = File.ReadAllText(resourcesPath, Encoding.UTF8);
        var recipesJson = File.ReadAllText(recipesPath, Encoding.UTF8);

        return Load(resourcesJson, recipesJson);
    }

    public static GameContent Load(string resourcesJson, string recipesJson)
    {
        var resources = Deserialize<IReadOnlyList<ResourceDef>>(resourcesJson, "resources").ToArray();
        var recipes = Deserialize<IReadOnlyList<RecipeDef>>(recipesJson, "recipes").ToArray();
        ContentValidator.Validate(resources, recipes);
        return new GameContent(resources, recipes, ComputeHash(resourcesJson, recipesJson));
    }

    private static T Deserialize<T>(string json, string label)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new InvalidOperationException($"{label} payload was empty.");
        }
        catch (JsonException ex)
        {
            throw new ContentValidationException([$"{label} JSON could not be parsed: {ex.Message}"]);
        }
    }

    private static string ComputeHash(string resourcesJson, string recipesJson)
    {
        var canonical = new StringBuilder()
            .Append(NormalizeJson(resourcesJson))
            .Append('\n')
            .Append(NormalizeJson(recipesJson))
            .ToString();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeJson(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });
        return JsonSerializer.Serialize(document.RootElement);
    }
}

public static class ContentValidator
{
    public static void Validate(IReadOnlyList<ResourceDef> resources, IReadOnlyList<RecipeDef> recipes)
    {
        var errors = new List<string>();
        ValidateResources(resources, errors);
        ValidateRecipes(resources, recipes, errors);

        if (errors.Count > 0)
        {
            throw new ContentValidationException(errors);
        }
    }

    private static void ValidateResources(IReadOnlyList<ResourceDef> resources, List<string> errors)
    {
        if (resources.Count == 0)
        {
            errors.Add("resources must contain at least one resource");
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var resource in resources)
        {
            if (string.IsNullOrWhiteSpace(resource.Id))
            {
                errors.Add("resource id must not be empty");
                continue;
            }

            if (!ids.Add(resource.Id))
            {
                errors.Add($"duplicate resource id '{resource.Id}'");
            }

            if (string.IsNullOrWhiteSpace(resource.Tier))
            {
                errors.Add($"resource '{resource.Id}' must define tier");
            }

            if (resource.BasePrice <= 0)
            {
                errors.Add($"resource '{resource.Id}' must have positive basePrice");
            }

            if (resource.Weight <= 0)
            {
                errors.Add($"resource '{resource.Id}' must have positive weight");
            }

            if (resource.SpoilDays < 0)
            {
                errors.Add($"resource '{resource.Id}' must not have negative spoilDays");
            }

            if (resource.Tags.Count != resource.Tags.Distinct(StringComparer.Ordinal).Count())
            {
                errors.Add($"resource '{resource.Id}' has duplicate tags");
            }
        }

        foreach (var resource in resources)
        {
            foreach (var substitute in resource.Substitutes)
            {
                if (!ids.Contains(substitute))
                {
                    errors.Add($"resource '{resource.Id}' references unknown substitute '{substitute}'");
                }
            }
        }
    }

    private static void ValidateRecipes(IReadOnlyList<ResourceDef> resources, IReadOnlyList<RecipeDef> recipes, List<string> errors)
    {
        if (recipes.Count == 0)
        {
            errors.Add("recipes must contain at least one recipe");
            return;
        }

        var resourceIds = resources.Select(resource => resource.Id).ToHashSet(StringComparer.Ordinal);
        var recipeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var recipe in recipes)
        {
            if (string.IsNullOrWhiteSpace(recipe.Id))
            {
                errors.Add("recipe id must not be empty");
                continue;
            }

            if (!recipeIds.Add(recipe.Id))
            {
                errors.Add($"duplicate recipe id '{recipe.Id}'");
            }

            if (string.IsNullOrWhiteSpace(recipe.BuildingType))
            {
                errors.Add($"recipe '{recipe.Id}' must define buildingType");
            }

            if (recipe.BaseDays <= 0)
            {
                errors.Add($"recipe '{recipe.Id}' must have positive baseDays");
            }

            if (recipe.Outputs.Count == 0)
            {
                errors.Add($"recipe '{recipe.Id}' must define at least one output");
            }

            ValidateAmounts(recipe.Id, "input", recipe.Inputs, resourceIds, errors);
            ValidateAmounts(recipe.Id, "output", recipe.Outputs, resourceIds, errors);

            if (recipe.Workforce.Peasants < 0 || recipe.Workforce.Artisans < 0)
            {
                errors.Add($"recipe '{recipe.Id}' must not require negative workforce");
            }
        }
    }

    private static void ValidateAmounts(
        string recipeId,
        string side,
        IReadOnlyList<ResourceAmount> amounts,
        HashSet<string> resourceIds,
        List<string> errors)
    {
        foreach (var amount in amounts)
        {
            if (!resourceIds.Contains(amount.ResourceId))
            {
                errors.Add($"recipe '{recipeId}' references unknown {side} resource '{amount.ResourceId}'");
            }

            if (amount.Amount <= 0)
            {
                errors.Add($"recipe '{recipeId}' has non-positive {side} amount for '{amount.ResourceId}'");
            }
        }
    }
}

