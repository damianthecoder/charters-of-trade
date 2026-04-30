using ChartersOfTrade.GodotBridge;
using ChartersOfTrade.Logistics.Core;
using Godot;

[GlobalClass]
public partial class InteractionSmokeRunner : Control
{
    private const int Seed = 424242;
    private static readonly Vector2I SmokeWindowSize = new(1366, 768);

    public override async void _Ready()
    {
        try
        {
            await RunSmokeAsync();
            GD.Print("INTERACTION_SMOKE PASS");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"INTERACTION_SMOKE FAIL: {ex}");
            GD.PushError($"INTERACTION_SMOKE FAIL: {ex.Message}");
            GetTree().Quit(1);
        }
    }

    private async Task RunSmokeAsync()
    {
        GetWindow().Size = SmokeWindowSize;
        SetAnchorsPreset(LayoutPreset.FullRect);

        var uiRoot = LoadMainScene();
        await WaitFrames(8);

        AssertSmoke(!AnyVisibleTextContains(uiRoot, "Startup failed"), "Main scene rendered the startup failure view.");

        var reference = new SimulationBridge().CreatePrototypeSession(Seed).Current;
        var targetContract = reference.AvailableContracts.FirstOrDefault()
            ?? throw new InvalidOperationException("Starter session did not expose any route contracts.");
        var targetCity = reference.Cities.First(city => city.Id == targetContract.FromNode);
        var targetRoute = reference.Routes.First(route => route.Id == targetContract.RouteId);

        var map = FindRequired<PrototypeMapView>(uiRoot);
        AssertSmoke(map.Size.X > 200 && map.Size.Y > 200, $"Map view did not lay out to an interactive size: {map.Size}.");

        var routesButton = FindButton(uiRoot, "Routes");
        var profitButton = FindButton(uiRoot, "Profit");
        var demandButton = FindButton(uiRoot, "Demand");
        var advanceButton = FindButton(uiRoot, "Advance Tick");
        var runFiveButton = FindButton(uiRoot, "Run 5");
        var selectContractButton = FindButton(uiRoot, "Select Contract");

        AssertSmoke(GetMetricValue(uiRoot, "Tick") == "0", "Initial tick metric was not zero.");

        await PressButtonAsync(profitButton);
        AssertSmoke(profitButton.ButtonPressed, "Profit map mode did not become selected.");
        await PressButtonAsync(demandButton);
        AssertSmoke(demandButton.ButtonPressed, "Demand map mode did not become selected.");
        await PressButtonAsync(routesButton);
        AssertSmoke(routesButton.ButtonPressed, "Routes map mode did not become selected.");

        await ClickMapAsync(map, MapPoint(map, reference, targetCity.X, targetCity.Y));
        AssertRichTextContains(uiRoot, targetCity.Name, "Company warehouse");

        await ClickMapAsync(map, RouteHitPoint(map, reference, targetRoute));
        AssertRichTextContains(uiRoot, targetRoute.Id, "cashflow");

        var contractOptions = FindRequired<OptionButton>(uiRoot);
        AssertSmoke(!contractOptions.Disabled, "Contract dropdown stayed disabled after selecting a route with contracts.");
        AssertSmoke(contractOptions.ItemCount > 0, "Contract dropdown did not contain any choices.");

        var contractIndex = contractOptions.ItemCount > 1 ? contractOptions.ItemCount - 1 : 0;
        contractOptions.Select(contractIndex);
        contractOptions.EmitSignal(OptionButton.SignalName.ItemSelected, (long)contractIndex);
        await WaitFrames(2);
        AssertSmoke(contractOptions.Selected == contractIndex, "Contract dropdown did not keep the requested selection.");

        await PressButtonAsync(selectContractButton);
        AssertSmoke(AnyVisibleTextContains(uiRoot, "Selected contract:"), "Selected contract summary did not appear.");

        await PressButtonAsync(advanceButton);
        AssertSmoke(GetMetricValue(uiRoot, "Tick") == "1", "Advance Tick did not advance the tick metric to 1.");

        await PressButtonAsync(runFiveButton);
        AssertSmoke(GetMetricValue(uiRoot, "Tick") == "6", "Run 5 did not advance the tick metric to 6.");

        await WaitFrames(6);
        AssertViewportHasVisualContent();

        GD.Print($"INTERACTION_SMOKE route={targetRoute.Id} city={targetCity.Id} contractChoice={contractIndex} tick=6");
    }

    private Control LoadMainScene()
    {
        var packed = GD.Load<PackedScene>("res://scenes/Main.tscn")
            ?? throw new InvalidOperationException("Could not load res://scenes/Main.tscn.");
        var instance = packed.Instantiate<Control>();

        if (instance is BootstrapPanel panel)
        {
            panel.Seed = Seed;
        }

        instance.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(instance);
        return instance;
    }

    private async Task PressButtonAsync(Button button)
    {
        AssertSmoke(!button.Disabled, $"Button '{button.Text}' was disabled.");
        button.EmitSignal(BaseButton.SignalName.Pressed);
        await WaitFrames(2);
    }

    private async Task ClickMapAsync(PrototypeMapView map, Vector2 localPosition)
    {
        map._GuiInput(new InputEventMouseMotion { Position = localPosition });
        map._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = localPosition
        });
        map._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            Position = localPosition
        });
        await WaitFrames(3);
    }

    private async Task WaitFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void AssertViewportHasVisualContent()
    {
        var texture = GetViewport().GetTexture();
        var image = texture.GetImage();
        var width = image.GetWidth();
        var height = image.GetHeight();

        AssertSmoke(width >= 400 && height >= 300, $"Viewport image was unexpectedly small: {width}x{height}.");

        var colors = new HashSet<int>();
        var visibleSamples = 0;
        var litSamples = 0;
        var stepX = Math.Max(1, width / 64);
        var stepY = Math.Max(1, height / 36);

        for (var y = 0; y < height; y += stepY)
        {
            for (var x = 0; x < width; x += stepX)
            {
                var color = image.GetPixel(x, y);
                if (color.A <= 0.05f)
                {
                    continue;
                }

                visibleSamples++;
                colors.Add(Quantize(color));

                if (color.R > 0.18f || color.G > 0.18f || color.B > 0.18f)
                {
                    litSamples++;
                }
            }
        }

        AssertSmoke(colors.Count >= 8, $"Viewport looked blank or flat; sampled only {colors.Count} colors.");
        AssertSmoke(litSamples >= Math.Max(12, visibleSamples / 45), $"Viewport had too few lit samples: {litSamples}/{visibleSamples}.");
    }

    private static int Quantize(Color color)
    {
        var r = (int)Math.Clamp(color.R * 15.0f, 0.0f, 15.0f);
        var g = (int)Math.Clamp(color.G * 15.0f, 0.0f, 15.0f);
        var b = (int)Math.Clamp(color.B * 15.0f, 0.0f, 15.0f);
        var a = (int)Math.Clamp(color.A * 15.0f, 0.0f, 15.0f);
        return r << 12 | g << 8 | b << 4 | a;
    }

    private static Vector2 RouteHitPoint(PrototypeMapView map, PrototypeSnapshot snapshot, TradeRoute route)
    {
        var from = snapshot.Cities.First(city => city.Id == route.FromNode);
        var to = snapshot.Cities.First(city => city.Id == route.ToNode);
        var start = MapPoint(map, snapshot, from.X, from.Y);
        var end = MapPoint(map, snapshot, to.X, to.Y);

        foreach (var weight in new[] { 0.50f, 0.35f, 0.65f, 0.25f, 0.75f })
        {
            var candidate = start.Lerp(end, weight);
            if (snapshot.Cities.All(city => MapPoint(map, snapshot, city.X, city.Y).DistanceTo(candidate) > 20.0f))
            {
                return candidate;
            }
        }

        return start.Lerp(end, 0.5f);
    }

    private static Vector2 MapPoint(PrototypeMapView map, PrototypeSnapshot snapshot, int x, int y)
    {
        var xScale = Math.Max(1.0, (map.Size.X - 26.0) / Math.Max(1, snapshot.World.Width));
        var yScale = Math.Max(1.0, (map.Size.Y - 26.0) / Math.Max(1, snapshot.World.Height));
        var cell = (float)Math.Min(xScale, yScale);
        var width = snapshot.World.Width * cell;
        var height = snapshot.World.Height * cell;
        var offset = new Vector2((map.Size.X - width) / 2.0f, (map.Size.Y - height) / 2.0f);
        return offset + new Vector2(x * cell + cell / 2.0f, y * cell + cell / 2.0f);
    }

    private static Button FindButton(Node root, string text)
    {
        return FindRequired<Button>(root, button => string.Equals(button.Text, text, StringComparison.Ordinal));
    }

    private static T FindRequired<T>(Node root, Func<T, bool>? predicate = null)
        where T : Node
    {
        foreach (var node in SelfAndDescendants(root))
        {
            if (node is T typed && (predicate is null || predicate(typed)))
            {
                return typed;
            }
        }

        throw new InvalidOperationException($"Could not find required node of type {typeof(T).Name}.");
    }

    private static IEnumerable<Node> SelfAndDescendants(Node root)
    {
        yield return root;

        foreach (var child in root.GetChildren())
        {
            foreach (var descendant in SelfAndDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static string GetMetricValue(Node root, string metricName)
    {
        foreach (var grid in SelfAndDescendants(root).OfType<GridContainer>())
        {
            var children = grid.GetChildren();
            for (var i = 0; i < children.Count - 1; i++)
            {
                if (children[i] is Label key
                    && children[i + 1] is Label value
                    && string.Equals(key.Text, metricName, StringComparison.Ordinal))
                {
                    return value.Text;
                }
            }
        }

        throw new InvalidOperationException($"Could not find metric '{metricName}'.");
    }

    private static bool AnyVisibleTextContains(Node root, string text)
    {
        return SelfAndDescendants(root).OfType<Label>().Any(label => label.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
            || SelfAndDescendants(root).OfType<RichTextLabel>().Any(label => label.Text.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertRichTextContains(Node root, string first, string second)
    {
        var found = SelfAndDescendants(root).OfType<RichTextLabel>().Any(label =>
            label.Text.Contains(first, StringComparison.OrdinalIgnoreCase)
            && label.Text.Contains(second, StringComparison.OrdinalIgnoreCase));
        AssertSmoke(found, $"No rich text panel contained both '{first}' and '{second}'.");
    }

    private static void AssertSmoke(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
