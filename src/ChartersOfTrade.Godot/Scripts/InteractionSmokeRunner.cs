using ChartersOfTrade.GodotBridge;
using ChartersOfTrade.Logistics.Core;
using Godot;

[GlobalClass]
public partial class InteractionSmokeRunner : Control
{
    private const int Seed = 424242;
    private const int AlternateSeed = 424243;
    private const int ExpectedFinalTick = 18;
    private static readonly Vector2I SmokeWindowSize = new(1920, 1080);

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

        var map = FindRequired<PrototypeMapView>(uiRoot);
        AssertSmoke(map.Size.X > 200 && map.Size.Y > 200, $"Map view did not lay out to an interactive size: {map.Size}.");
        AssertSmoke(map.Size.X >= 1000 && map.Size.Y >= 700, $"Full HD map area was too small: {map.Size}.");

        var sidebarScroll = FindRequired<ScrollContainer>(uiRoot);
        var seedInput = FindRequired<SpinBox>(uiRoot);
        var routesButton = FindButton(uiRoot, "Routes");
        var profitButton = FindButton(uiRoot, "Profit");
        var demandButton = FindButton(uiRoot, "Demand");
        var priorityPolicyButton = FindButton(uiRoot, "Priority");
        var safetyPolicyButton = FindButton(uiRoot, "Safety");
        var reorderPolicyButton = FindButton(uiRoot, "Reorder");
        var advanceButton = FindButton(uiRoot, "Advance Tick");
        var runFiveButton = FindButton(uiRoot, "Run 5");
        var runTwelveButton = FindButton(uiRoot, "Run 12");
        var resetSeedButton = FindButton(uiRoot, "Reset Seed");
        var selectContractButton = FindButton(uiRoot, "Select Contract");

        AssertSmoke(AnyVisibleTextContains(uiRoot, "System Test Bench"), "System Test Bench was not visible.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "Production Chains"), "Production Chains panel was not visible.");
        AssertSmoke(!runTwelveButton.Disabled, "Run 12 was disabled.");
        AssertSmoke(!resetSeedButton.Disabled, "Reset Seed was disabled.");
        AssertSmoke(GetMetricValue(uiRoot, "Tick") == "0", "Initial tick metric was not zero.");
        AssertControlIntersectsViewport(runTwelveButton, "Run 12");
        await ExerciseSidebarScrollAsync(sidebarScroll);

        var initialHash = GetMetricValue(uiRoot, "Save Hash");
        seedInput.Value = AlternateSeed;
        await PressButtonAsync(resetSeedButton);
        AssertSmoke(GetMetricValue(uiRoot, "Tick") == "0", "Reset Seed did not return the session to tick 0.");
        AssertSmoke(GetMetricValue(uiRoot, "Save Hash") != initialHash, "Reset Seed did not change the session hash after changing the seed.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, $"Seed {AlternateSeed}"), "System Test Bench did not report the changed seed.");

        seedInput.Value = Seed;
        await PressButtonAsync(resetSeedButton);
        AssertSmoke(GetMetricValue(uiRoot, "Tick") == "0", "Reset Seed did not restore the starter seed to tick 0.");
        AssertSmoke(GetMetricValue(uiRoot, "Save Hash") == initialHash, "Reset Seed did not restore the starter seed hash.");

        var reference = new SimulationBridge().CreatePrototypeSession(Seed).Current;
        var targetContract = reference.AvailableContracts.FirstOrDefault()
            ?? throw new InvalidOperationException("Starter session did not expose any route contracts.");
        var targetCity = reference.Cities.First(city => city.Id == targetContract.FromNode);
        var targetRoute = reference.Routes.First(route => route.Id == targetContract.RouteId);

        await PressButtonAsync(profitButton);
        AssertSmoke(profitButton.ButtonPressed, "Profit map mode did not become selected.");
        await PressButtonAsync(demandButton);
        AssertSmoke(demandButton.ButtonPressed, "Demand map mode did not become selected.");
        await PressButtonAsync(routesButton);
        AssertSmoke(routesButton.ButtonPressed, "Routes map mode did not become selected.");

        await ClickMapAsync(map, MapPoint(map, reference, targetCity.X, targetCity.Y));
        AssertRichTextContains(uiRoot, targetCity.Name, "Company warehouse");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "Top chain"), "City inspector did not expose the top production chain.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, $"Focus city: {targetCity.Name}"), "Warehouse policy did not focus the selected city.");

        var policyFocusOptions = FindRequired<OptionButton>(uiRoot, control => string.Equals(control.Name, "PolicyFocusOptions", StringComparison.Ordinal));
        AssertSmoke(!policyFocusOptions.Disabled, "Policy focus dropdown was disabled.");
        AssertSmoke(policyFocusOptions.ItemCount > 1, "Policy focus dropdown did not contain city choices.");
        var targetCityIndex = FindItemIndex(policyFocusOptions, targetCity.Name);
        policyFocusOptions.Select(targetCityIndex);
        policyFocusOptions.EmitSignal(OptionButton.SignalName.ItemSelected, (long)targetCityIndex);
        await WaitFrames(2);
        AssertSmoke(AnyVisibleTextContains(uiRoot, $"Focus city: {targetCity.Name}"), "Policy focus dropdown did not select the requested city.");

        await PressButtonAsync(safetyPolicyButton);
        AssertSmoke(safetyPolicyButton.ButtonPressed, "Safety policy view did not become selected.");
        AssertSmoke(policyFocusOptions.GetItemText(0) == "Auto safety", "Safety policy view did not relabel auto focus.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "View: Safety stock guard"), "Warehouse policy did not render the safety stock view.");

        await PressButtonAsync(reorderPolicyButton);
        AssertSmoke(reorderPolicyButton.ButtonPressed, "Reorder policy view did not become selected.");
        AssertSmoke(policyFocusOptions.GetItemText(0) == "Auto reorder", "Reorder policy view did not relabel auto focus.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "View: Reorder queue"), "Warehouse policy did not render the reorder queue view.");

        await PressButtonAsync(priorityPolicyButton);
        AssertSmoke(priorityPolicyButton.ButtonPressed, "Priority policy view did not become selected.");
        AssertSmoke(policyFocusOptions.GetItemText(0) == "Auto priority", "Priority policy view did not relabel auto focus.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "View: Priority dispatch"), "Warehouse policy did not render the priority dispatch view.");

        var warehouseResourceOptions = FindRequired<OptionButton>(uiRoot, control => string.Equals(control.Name, "WarehouseResourceOptions", StringComparison.Ordinal));
        var warehouseModeOptions = FindRequired<OptionButton>(uiRoot, control => string.Equals(control.Name, "WarehouseModeOptions", StringComparison.Ordinal));
        var warehouseSafetyInput = FindRequired<SpinBox>(uiRoot, control => string.Equals(control.Name, "WarehouseSafetyInput", StringComparison.Ordinal));
        var warehouseReorderInput = FindRequired<SpinBox>(uiRoot, control => string.Equals(control.Name, "WarehouseReorderInput", StringComparison.Ordinal));
        var applyWarehousePolicyButton = FindButton(uiRoot, "Apply Warehouse Policy");
        await ScrollControlIntoViewAsync(sidebarScroll, warehouseModeOptions, "Warehouse automation mode options");
        await ScrollControlIntoViewAsync(sidebarScroll, warehouseResourceOptions, "Warehouse policy resource options");
        AssertSmoke(!warehouseModeOptions.Disabled, "Warehouse automation mode options stayed disabled after selecting a city.");
        AssertSmoke(!warehouseResourceOptions.Disabled, "Warehouse policy resource options stayed disabled after selecting a city.");
        AssertSmoke(!applyWarehousePolicyButton.Disabled, "Apply Warehouse Policy stayed disabled after selecting a city.");
        AssertControlIntersectsViewport(warehouseModeOptions, "Warehouse automation mode options");
        AssertControlIntersectsViewport(warehouseResourceOptions, "Warehouse policy resource options");
        AssertControlIntersectsViewport(applyWarehousePolicyButton, "Apply Warehouse Policy");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "reserved"), "Warehouse policy panel did not explain reserved stock.");

        var preModeHash = GetMetricValue(uiRoot, "Save Hash");
        var conservativeModeIndex = FindItemIndex(warehouseModeOptions, "Conservative");
        warehouseModeOptions.Select(conservativeModeIndex);
        warehouseModeOptions.EmitSignal(OptionButton.SignalName.ItemSelected, (long)conservativeModeIndex);
        await WaitFrames(2);
        AssertSmoke(GetMetricValue(uiRoot, "Save Hash") != preModeHash, "Conservative warehouse mode did not change the save hash.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "Conservative"), "Warehouse policy panel did not expose Conservative mode.");

        var postConservativeHash = GetMetricValue(uiRoot, "Save Hash");
        var balancedModeIndex = FindItemIndex(warehouseModeOptions, "Balanced");
        warehouseModeOptions.Select(balancedModeIndex);
        warehouseModeOptions.EmitSignal(OptionButton.SignalName.ItemSelected, (long)balancedModeIndex);
        await WaitFrames(2);
        AssertSmoke(GetMetricValue(uiRoot, "Save Hash") != postConservativeHash, "Balanced warehouse mode did not reset the save hash.");
        AssertSmoke(GetMetricValue(uiRoot, "Save Hash") == preModeHash, "Balanced warehouse mode did not return to the default policy hash.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "Balanced"), "Warehouse policy panel did not expose Balanced mode.");

        var prePolicyHash = GetMetricValue(uiRoot, "Save Hash");
        warehouseSafetyInput.Value = Math.Min(64, warehouseSafetyInput.Value + 1);
        warehouseReorderInput.Value = Math.Min(64, Math.Max(warehouseReorderInput.Value + 1, warehouseSafetyInput.Value));
        await PressButtonAsync(applyWarehousePolicyButton);
        AssertSmoke(GetMetricValue(uiRoot, "Save Hash") != prePolicyHash, "Warehouse policy apply did not change the save hash.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "Applied warehouse policy"), "Warehouse policy apply did not produce confirmation text.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "manual"), "Warehouse policy apply did not mark the policy as manual.");

        await ClickMapAsync(map, RouteHitPoint(map, reference, targetRoute));
        AssertRichTextContains(uiRoot, targetRoute.Id, "cashflow");

        var contractOptions = FindRequired<OptionButton>(uiRoot, control => string.Equals(control.Name, "ContractOptions", StringComparison.Ordinal));
        AssertSmoke(!contractOptions.Disabled, "Contract dropdown stayed disabled after selecting a route with contracts.");
        AssertSmoke(contractOptions.ItemCount > 0, "Contract dropdown did not contain any choices.");

        var routePolicyOptions = FindRequired<OptionButton>(uiRoot, control => string.Equals(control.Name, "RoutePolicyResourceOptions", StringComparison.Ordinal));
        var routePriorityButton = FindButton(uiRoot, "Set Priority");
        await ScrollControlIntoViewAsync(sidebarScroll, routePolicyOptions, "Route policy resource options");
        AssertSmoke(!routePolicyOptions.Disabled, "Route policy resource options stayed disabled after selecting a route.");
        AssertControlIntersectsViewport(routePriorityButton, "Set Priority");

        var preRoutePolicyHash = GetMetricValue(uiRoot, "Save Hash");
        await PressButtonAsync(routePriorityButton);
        AssertSmoke(GetMetricValue(uiRoot, "Save Hash") != preRoutePolicyHash, "Route priority policy did not change the save hash.");
        AssertSmoke(AnyVisibleTextContains(uiRoot, "route priority"), "Route policy controls did not report route priority.");

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

        await PressButtonAsync(runTwelveButton);
        AssertSmoke(GetMetricValue(uiRoot, "Tick") == ExpectedFinalTick.ToString(), "Run 12 did not advance the tick metric to 18.");

        await WaitFrames(6);
        AssertViewportHasVisualContent();

        GD.Print($"INTERACTION_SMOKE route={targetRoute.Id} city={targetCity.Id} contractChoice={contractIndex} tick={ExpectedFinalTick}");
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

    private async Task ExerciseSidebarScrollAsync(ScrollContainer scroll)
    {
        var maxScroll = scroll.GetVScrollBar().MaxValue;
        AssertSmoke(maxScroll > 0, "Sidebar did not expose a vertical scroll range.");

        scroll.ScrollVertical = (int)maxScroll;
        await WaitFrames(2);
        AssertSmoke(scroll.ScrollVertical > 0, "Sidebar did not accept vertical scrolling.");

        scroll.ScrollVertical = 0;
        await WaitFrames(2);
    }

    private async Task ScrollControlIntoViewAsync(ScrollContainer scroll, Control control, string name)
    {
        var scrollRect = scroll.GetGlobalRect();
        var controlRect = control.GetGlobalRect();
        var contentY = controlRect.Position.Y - scrollRect.Position.Y + scroll.ScrollVertical;
        var maxScroll = scroll.GetVScrollBar().MaxValue;
        scroll.ScrollVertical = (int)Math.Clamp(contentY - 48.0, 0.0, maxScroll);
        await WaitFrames(2);
        AssertControlIntersectsViewport(control, name);
    }

    private void AssertControlIntersectsViewport(Control control, string name)
    {
        var viewportRect = new Rect2(Vector2.Zero, GetViewportRect().Size);
        AssertSmoke(viewportRect.Intersects(control.GetGlobalRect()), $"{name} did not intersect the visible viewport.");
    }

    private void AssertViewportHasVisualContent()
    {
        if (string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
        {
            AssertSmoke(GetMetricValue(this, "Tick") == ExpectedFinalTick.ToString(), "Headless smoke reached visual check before post-interaction UI updated.");
            AssertSmoke(AnyVisibleTextContains(this, "Cashflow"), "Headless smoke did not retain visible KPI content.");
            return;
        }

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

    private static int FindItemIndex(OptionButton options, string text)
    {
        for (var i = 0; i < options.ItemCount; i++)
        {
            if (string.Equals(options.GetItemText(i), text, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Could not find option '{text}'.");
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
            || SelfAndDescendants(root).OfType<RichTextLabel>().Any(label => RichTextContent(label).Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertRichTextContains(Node root, string first, string second)
    {
        var found = SelfAndDescendants(root).OfType<RichTextLabel>().Any(label =>
            RichTextContent(label).Contains(first, StringComparison.OrdinalIgnoreCase)
            && RichTextContent(label).Contains(second, StringComparison.OrdinalIgnoreCase));
        AssertSmoke(found, $"No rich text panel contained both '{first}' and '{second}'. Visible rich text: {RichTextSnapshot(root)}");
    }

    private static string RichTextContent(RichTextLabel label)
    {
        return label.GetParsedText();
    }

    private static string RichTextSnapshot(Node root)
    {
        var text = string.Join(" | ", SelfAndDescendants(root)
            .OfType<RichTextLabel>()
            .Select(label => RichTextContent(label).Replace('\n', ' ').Trim())
            .Where(text => text.Length > 0));
        return text.Length <= 900 ? text : text[..900] + "...";
    }

    private static void AssertSmoke(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
