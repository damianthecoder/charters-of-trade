using Godot;

[GlobalClass]
public partial class VisualQaRunner : Control
{
    private static readonly int[] Seeds = [424242, 424243, 20260429];
    private static readonly string[] MapModes = ["Routes", "Profit", "Demand"];
    private static readonly Vector2I QaWindowSize = new(1920, 1080);

    public override async void _Ready()
    {
        try
        {
            await RunVisualQaAsync();
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"VISUAL_QA FAIL: {ex}");
            GD.PushError($"VISUAL_QA FAIL: {ex.Message}");
            GetTree().Quit(1);
        }
    }

    private async Task RunVisualQaAsync()
    {
        if (string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Visual QA requires a non-headless renderer.");
        }

        GetWindow().Size = QaWindowSize;
        SetAnchorsPreset(LayoutPreset.FullRect);

        var outputDir = OutputDirectory();
        var captures = new List<string>();

        foreach (var seed in Seeds)
        {
            var uiRoot = LoadMainScene(seed);
            await WaitFrames(8);

            AssertQa(!AnyVisibleTextContains(uiRoot, "Startup failed"), $"Seed {seed} rendered the startup failure view.");
            AssertQa(AnyVisibleTextContains(uiRoot, $"Seed {seed}"), $"Seed {seed} was not visible in the System Test Bench.");
            AssertQa(AnyVisibleTextContains(uiRoot, "Warehouse Policy"), "Warehouse Policy panel title was not present.");
            AssertQa(AnyVisibleTextContains(uiRoot, "Production Chains"), "Production Chains panel title was not present.");
            AssertQa(AnyVisibleTextContains(uiRoot, "Route Operation"), "Route operation summary was not present.");
            AssertQa(AnyVisibleTextContains(uiRoot, "First Charter Season"), "Scenario objective panel title was not present.");
            AssertQa(AnyVisibleTextContains(uiRoot, "Stage 3 Production"), "Stage 3 status card was not present.");
            AssertQa(AnyVisibleTextContains(uiRoot, "Stage 4 Routes"), "Stage 4 status card was not present.");
            AssertQa(AnyVisibleTextContains(uiRoot, "Stage 5 NPC"), "Stage 5 status card was not present.");
            AssertQa(AnyVisibleTextContains(uiRoot, "Warehouse Guard"), "Warehouse status card was not present.");

            var sidebar = FindRequired<ScrollContainer>(uiRoot);
            var scenarioObjectiveLog = FindRequired<RichTextLabel>(uiRoot, control => string.Equals(control.Name, "ScenarioObjectiveLog", StringComparison.Ordinal));
            var npcPressureLog = FindRequired<RichTextLabel>(uiRoot, control => string.Equals(control.Name, "NpcPressureLog", StringComparison.Ordinal));
            var tickFeedback = FindRequired<Label>(uiRoot, control => string.Equals(control.Name, "TickChangeFeedback", StringComparison.Ordinal));
            var stage3CardBody = FindRequired<Label>(uiRoot, control => string.Equals(control.Name, "StatusCardProduction", StringComparison.Ordinal));
            var stage4CardBody = FindRequired<Label>(uiRoot, control => string.Equals(control.Name, "StatusCardRoutes", StringComparison.Ordinal));
            var cashProgress = FindRequired<ProgressBar>(uiRoot, control => string.Equals(control.Name, "SeasonCashProgress", StringComparison.Ordinal));
            var deliveryProgress = FindRequired<ProgressBar>(uiRoot, control => string.Equals(control.Name, "SeasonDeliveryProgress", StringComparison.Ordinal));
            var routePolicyOptions = FindRequired<OptionButton>(uiRoot, control => string.Equals(control.Name, "RoutePolicyResourceOptions", StringComparison.Ordinal));
            var policyFocusOptions = FindRequired<OptionButton>(uiRoot, control => string.Equals(control.Name, "PolicyFocusOptions", StringComparison.Ordinal));
            var warehouseModeOptions = FindRequired<OptionButton>(uiRoot, control => string.Equals(control.Name, "WarehouseModeOptions", StringComparison.Ordinal));
            AssertQa(tickFeedback.Text.Contains("Stage 3-6", StringComparison.OrdinalIgnoreCase), "Tick change feedback did not identify the visible systems.");
            AssertQa(cashProgress.Value > 0, "Season cash progress bar did not show progress.");
            AssertQa(deliveryProgress.Value >= 0, "Season delivery progress bar was not initialized.");
            AssertControlIntersectsViewport(cashProgress, "Season cash progress bar");
            AssertControlIntersectsViewport(deliveryProgress, "Season delivery progress bar");
            AssertControlIntersectsViewport(stage3CardBody, "Stage 3 Production status card");
            AssertControlIntersectsViewport(stage4CardBody, "Stage 4 Routes status card");
            var scenarioText = scenarioObjectiveLog.GetParsedText();
            AssertQa(scenarioText.Contains("Deliveries", StringComparison.OrdinalIgnoreCase), "Scenario objective panel did not render delivery progress.");
            AssertQa(scenarioText.Contains("Stable needs", StringComparison.OrdinalIgnoreCase), "Scenario objective panel did not render stable-needs progress.");
            AssertQa(scenarioText.Contains("Next:", StringComparison.OrdinalIgnoreCase), "Scenario objective panel did not render the next-step line.");
            AssertQa(npcPressureLog.GetParsedText().Contains("Top rival pressure:", StringComparison.OrdinalIgnoreCase), "NPC Pressure panel did not render the rival pressure heading.");
            AssertQa(npcPressureLog.GetParsedText().Contains("North Sea Company", StringComparison.OrdinalIgnoreCase), "NPC Pressure panel did not render a rival company line.");
            await ScrollControlIntoViewAsync(sidebar, scenarioObjectiveLog, "First Charter Season objective");
            captures.Add(SaveCapture(outputDir, $"seed-{seed}-scenario-objective.png"));
            await ScrollControlIntoViewAsync(sidebar, npcPressureLog, "NPC pressure log");
            captures.Add(SaveCapture(outputDir, $"seed-{seed}-npc-pressure.png"));
            await ScrollControlIntoViewAsync(sidebar, routePolicyOptions, "Route policy resource options");
            await ScrollControlIntoViewAsync(sidebar, policyFocusOptions, "Warehouse policy focus options");
            await ScrollControlIntoViewAsync(sidebar, warehouseModeOptions, "Warehouse automation mode options");
            captures.Add(SaveCapture(outputDir, $"seed-{seed}-warehouse-mode.png"));
            sidebar.ScrollVertical = 0;
            await WaitFrames(2);

            foreach (var mode in MapModes)
            {
                await PressButtonAsync(uiRoot, mode);
                AssertQa(AnyVisibleTextContains(uiRoot, mode), $"Map mode {mode} was not visible after selection.");
                captures.Add(SaveCapture(outputDir, $"seed-{seed}-{mode.ToLowerInvariant()}.png"));
            }

            sidebar.ScrollVertical = (int)sidebar.GetVScrollBar().MaxValue;
            await WaitFrames(3);
            AssertControlIntersectsViewport(policyFocusOptions, "Warehouse policy focus options");
            captures.Add(SaveCapture(outputDir, $"seed-{seed}-sidebar-bottom.png"));

            uiRoot.QueueFree();
            await WaitFrames(2);
        }

        GD.Print($"VISUAL_QA PASS {captures.Count} captures {outputDir}");
    }

    private Control LoadMainScene(int seed)
    {
        var packed = GD.Load<PackedScene>("res://scenes/Main.tscn")
            ?? throw new InvalidOperationException("Could not load res://scenes/Main.tscn.");
        var instance = packed.Instantiate<Control>();

        if (instance is BootstrapPanel panel)
        {
            panel.Seed = seed;
        }

        instance.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(instance);
        return instance;
    }

    private async Task PressButtonAsync(Node root, string text)
    {
        var button = FindRequired<Button>(root, button => string.Equals(button.Text, text, StringComparison.Ordinal));
        AssertQa(!button.Disabled, $"Button '{text}' was disabled.");
        button.EmitSignal(BaseButton.SignalName.Pressed);
        await WaitFrames(3);
        AssertQa(button.ButtonPressed, $"Button '{text}' did not remain selected.");
    }

    private async Task ScrollControlIntoViewAsync(ScrollContainer scroll, Control control, string name)
    {
        var scrollRect = scroll.GetGlobalRect();
        var controlRect = control.GetGlobalRect();
        var contentY = controlRect.Position.Y - scrollRect.Position.Y + scroll.ScrollVertical;
        var maxScroll = scroll.GetVScrollBar().MaxValue;
        scroll.ScrollVertical = (int)Math.Clamp(contentY - 48.0, 0.0, maxScroll);
        await WaitFrames(3);
        AssertControlIntersectsViewport(control, name);
    }

    private void AssertControlIntersectsViewport(Control control, string name)
    {
        var viewportRect = new Rect2(Vector2.Zero, GetViewportRect().Size);
        var controlRect = control.GetGlobalRect();
        AssertQa(viewportRect.Intersects(controlRect), $"{name} did not intersect the visible viewport: {controlRect}.");
        AssertQa(controlRect.Size.X >= 120.0f && controlRect.Size.Y >= 24.0f, $"{name} was too small to inspect: {controlRect.Size}.");
    }

    private string SaveCapture(string outputDir, string fileName)
    {
        var image = GetViewport().GetTexture().GetImage();
        AssertQa(image.GetWidth() == QaWindowSize.X && image.GetHeight() == QaWindowSize.Y, $"Expected {QaWindowSize.X}x{QaWindowSize.Y}, got {image.GetWidth()}x{image.GetHeight()}.");
        AssertQa(HasVisualContent(image), $"{fileName} looked blank or too flat.");

        var path = Path.Combine(outputDir, fileName);
        var result = image.SavePng(path);
        if (result != Error.Ok)
        {
            throw new InvalidOperationException($"Could not save visual QA capture {path}: {result}.");
        }

        GD.Print($"VISUAL_QA_CAPTURE {path}");
        return path;
    }

    private static bool HasVisualContent(Image image)
    {
        var colors = new HashSet<int>();
        var lit = 0;
        var stepX = Math.Max(1, image.GetWidth() / 64);
        var stepY = Math.Max(1, image.GetHeight() / 36);

        for (var y = 0; y < image.GetHeight(); y += stepY)
        {
            for (var x = 0; x < image.GetWidth(); x += stepX)
            {
                var color = image.GetPixel(x, y);
                colors.Add(Quantize(color));
                if (color.R > 0.14f || color.G > 0.14f || color.B > 0.14f)
                {
                    lit++;
                }
            }
        }

        return colors.Count >= 8 && lit >= 12;
    }

    private static int Quantize(Color color)
    {
        var r = (int)Math.Clamp(color.R * 15.0f, 0.0f, 15.0f);
        var g = (int)Math.Clamp(color.G * 15.0f, 0.0f, 15.0f);
        var b = (int)Math.Clamp(color.B * 15.0f, 0.0f, 15.0f);
        var a = (int)Math.Clamp(color.A * 15.0f, 0.0f, 15.0f);
        return r << 12 | g << 8 | b << 4 | a;
    }

    private static string OutputDirectory()
    {
        var requested = System.Environment.GetEnvironmentVariable("COT_VISUAL_QA_DIR");
        if (!string.IsNullOrWhiteSpace(requested))
        {
            Directory.CreateDirectory(requested);
            return requested;
        }

        var outputDir = ProjectSettings.GlobalizePath($"res://../../artifacts/godot-visual-qa/visual-qa-{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(outputDir);
        return outputDir;
    }

    private async Task WaitFrames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private static bool AnyVisibleTextContains(Node root, string text)
    {
        return SelfAndDescendants(root).OfType<Label>().Any(label => label.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
            || SelfAndDescendants(root).OfType<RichTextLabel>().Any(label => label.GetParsedText().Contains(text, StringComparison.OrdinalIgnoreCase));
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

    private static void AssertQa(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
