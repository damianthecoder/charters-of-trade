using System.Globalization;
using ChartersOfTrade.GodotBridge;
using ChartersOfTrade.Logistics.Core;
using Godot;

[GlobalClass]
public partial class BootstrapPanel : Control
{
    private readonly Dictionary<string, Label> _metrics = [];

    private PrototypeSession? _session;
    private PrototypeSnapshot? _snapshot;
    private PrototypeMapView? _map;
    private RichTextLabel? _ledger;
    private RichTextLabel? _cities;
    private RichTextLabel? _inspector;
    private RichTextLabel? _warnings;
    private string? _selectedCityId;
    private string? _selectedRouteId;

    [Export]
    public int Seed { get; set; } = 424242;

    public override void _Ready()
    {
        Name = "BootstrapPanel";
        SetAnchorsPreset(LayoutPreset.FullRect);

        try
        {
            _session = new SimulationBridge().CreatePrototypeSession(Seed);
            _snapshot = _session.Current;
            BuildPrototypeView();
            UpdatePrototypeView();
        }
        catch (Exception ex)
        {
            BuildFailureView(ex);
        }
    }

    private void BuildPrototypeView()
    {
        ClearChildren();

        var background = new ColorRect { Color = new Color(0.10f, 0.095f, 0.082f, 1.0f) };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        AddChild(margin);

        var root = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 14);
        margin.AddChild(root);

        _map = new PrototypeMapView
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(760, 560)
        };
        _map.CitySelected += SelectCity;
        _map.RouteSelected += SelectRoute;
        _map.SelectionCleared += ClearSelection;
        root.AddChild(WrapPanel(_map));

        var sidebar = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(390, 560)
        };
        sidebar.AddThemeConstantOverride("separation", 10);
        root.AddChild(WrapPanel(sidebar));

        sidebar.AddChild(CreateTitle("Charters of Trade"));
        sidebar.AddChild(CreateMutedLabel("Prototype systems loop"));
        sidebar.AddChild(CreateDivider());

        var metricGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        sidebar.AddChild(metricGrid);

        AddMetric(metricGrid, "Tick");
        AddMetric(metricGrid, "Day");
        AddMetric(metricGrid, "Cash");
        AddMetric(metricGrid, "Cashflow");
        AddMetric(metricGrid, "Save Hash");
        AddMetric(metricGrid, "AI Move");
        AddMetric(metricGrid, "Unmet Demand");

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        sidebar.AddChild(actions);

        var tickButton = CreateButton("Advance Tick");
        tickButton.Pressed += AdvanceOneTick;
        actions.AddChild(tickButton);

        var runButton = CreateButton("Run 5");
        runButton.Pressed += AdvanceFiveTicks;
        actions.AddChild(runButton);

        sidebar.AddChild(CreateSectionLabel("Inspector"));
        _inspector = CreateLog();
        _inspector.CustomMinimumSize = new Vector2(0, 150);
        sidebar.AddChild(_inspector);

        sidebar.AddChild(CreateSectionLabel("Priority Signals"));
        _warnings = CreateLog();
        _warnings.CustomMinimumSize = new Vector2(0, 92);
        sidebar.AddChild(_warnings);

        sidebar.AddChild(CreateSectionLabel("Cities"));
        _cities = CreateLog();
        _cities.CustomMinimumSize = new Vector2(0, 102);
        sidebar.AddChild(_cities);

        sidebar.AddChild(CreateSectionLabel("Ledger"));
        _ledger = CreateLog();
        _ledger.SizeFlagsVertical = SizeFlags.ExpandFill;
        sidebar.AddChild(_ledger);
    }

    private void BuildFailureView(Exception ex)
    {
        ClearChildren();

        var panel = WrapPanel(CreateStack());
        panel.AnchorLeft = 0.5f;
        panel.AnchorTop = 0.5f;
        panel.AnchorRight = 0.5f;
        panel.AnchorBottom = 0.5f;
        panel.OffsetLeft = -320;
        panel.OffsetTop = -120;
        panel.OffsetRight = 320;
        panel.OffsetBottom = 120;
        AddChild(panel);

        if (panel.GetChild(0) is VBoxContainer stack)
        {
            stack.AddChild(CreateTitle("Startup failed"));
            stack.AddChild(CreateMutedLabel(ex.Message));
        }
    }

    private void AdvanceOneTick()
    {
        if (_session is null)
        {
            return;
        }

        _snapshot = _session.AdvanceTick();
        KeepValidSelection();
        UpdatePrototypeView();
    }

    private void AdvanceFiveTicks()
    {
        if (_session is null)
        {
            return;
        }

        for (var i = 0; i < 5; i++)
        {
            _snapshot = _session.AdvanceTick();
        }

        KeepValidSelection();
        UpdatePrototypeView();
    }

    private void UpdatePrototypeView()
    {
        if (_snapshot is null)
        {
            return;
        }

        KeepValidSelection();
        _map?.SetSnapshot(_snapshot);
        _map?.SetSelection(_selectedCityId, _selectedRouteId);
        SetMetric("Tick", _snapshot.Tick.ToString(CultureInfo.InvariantCulture));
        SetMetric("Day", _snapshot.Calendar.DayOfYear.ToString(CultureInfo.InvariantCulture));
        SetMetric("Cash", _snapshot.Company.Cash.ToString("0.00", CultureInfo.InvariantCulture));
        SetMetric("Cashflow", _snapshot.LastTickCashDelta.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture));
        SetMetric("Save Hash", ShortHash(_snapshot.SaveHash));
        SetMetric("AI Move", _snapshot.AiChoice.OpportunityId);
        SetMetric("Unmet Demand", _snapshot.UnmetDemandRatio.ToString("0.0000", CultureInfo.InvariantCulture));

        if (_cities is not null)
        {
            _cities.Clear();
            foreach (var city in _snapshot.Cities.OrderBy(city => city.Id, StringComparer.Ordinal).Take(8))
            {
                _cities.AppendText($"{city.Name} | pop {city.Population} | {city.Level} | supply {city.SupplySatisfaction:0.00}\n");
            }
        }

        UpdateInspector();
        UpdateWarnings();

        if (_ledger is not null)
        {
            _ledger.Clear();
            foreach (var entry in _snapshot.Ledger.OrderByDescending(entry => entry.Tick).ThenBy(entry => entry.Category, StringComparer.Ordinal).Take(18))
            {
                var cash = entry.CashDelta == 0 ? "" : $" | cash {entry.CashDelta:+0.00;-0.00}";
                _ledger.AppendText($"T{entry.Tick} {entry.Category}: {entry.Message}{cash}\n");
            }
        }
    }

    private void ClearChildren()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        _metrics.Clear();
        _map = null;
        _ledger = null;
        _cities = null;
        _inspector = null;
        _warnings = null;
    }

    private void SelectCity(string cityId)
    {
        _selectedCityId = cityId;
        _selectedRouteId = null;
        UpdatePrototypeView();
    }

    private void SelectRoute(string routeId)
    {
        _selectedRouteId = routeId;
        _selectedCityId = null;
        UpdatePrototypeView();
    }

    private void ClearSelection()
    {
        _selectedCityId = null;
        _selectedRouteId = null;
        UpdatePrototypeView();
    }

    private void KeepValidSelection()
    {
        if (_snapshot is null)
        {
            return;
        }

        if (_selectedCityId is not null && _snapshot.Cities.All(city => city.Id != _selectedCityId))
        {
            _selectedCityId = null;
        }

        if (_selectedRouteId is not null && _snapshot.Routes.All(route => route.Id != _selectedRouteId))
        {
            _selectedRouteId = null;
        }
    }

    private void UpdateInspector()
    {
        if (_snapshot is null || _inspector is null)
        {
            return;
        }

        _inspector.Clear();

        if (_selectedCityId is not null)
        {
            var city = _snapshot.Cities.FirstOrDefault(city => city.Id == _selectedCityId);
            if (city is not null)
            {
                AppendCityInspector(city);
                return;
            }
        }

        if (_selectedRouteId is not null)
        {
            var route = _snapshot.Routes.FirstOrDefault(route => route.Id == _selectedRouteId);
            if (route is not null)
            {
                AppendRouteInspector(route);
                return;
            }
        }

        _inspector.AppendText("Select a city or route on the map.\n");
        _inspector.AppendText($"This tick cashflow: {_snapshot.LastTickCashDelta:+0.00;-0.00;0.00}\n");
        _inspector.AppendText("Route color shows current cashflow signal; city rings show supply pressure.\n");
    }

    private void AppendCityInspector(PrototypeCityView city)
    {
        if (_snapshot is null || _inspector is null)
        {
            return;
        }

        var connectedRoutes = _snapshot.Routes
            .Where(route => route.FromNode == city.Id || route.ToNode == city.Id)
            .OrderBy(route => route.Id, StringComparer.Ordinal)
            .ToArray();
        var recentLedger = _snapshot.Ledger
            .Where(entry => entry.RelatedId == city.Id)
            .OrderByDescending(entry => entry.Tick)
            .Take(4)
            .ToArray();
        var pricePressure = _snapshot.Prices
            .OrderByDescending(price => price.Scarcity)
            .Take(3)
            .Select(price => $"{price.ResourceId} {price.Price:0.00}")
            .ToArray();

        _inspector.AppendText($"{city.Name}\n");
        _inspector.AppendText($"Population {city.Population} | {city.Level} | supply {city.SupplySatisfaction:0.00}\n");
        _inspector.AppendText($"Market: {StockSummary(city.MarketStock)}\n");
        _inspector.AppendText($"Warehouse: {StockSummary(city.CompanyWarehouse)}\n");
        _inspector.AppendText($"Connected routes: {connectedRoutes.Length}\n");
        _inspector.AppendText($"Price pressure: {string.Join(", ", pricePressure)}\n");

        if (recentLedger.Length > 0)
        {
            _inspector.AppendText("Recent effects:\n");
            foreach (var entry in recentLedger)
            {
                _inspector.AppendText($"T{entry.Tick} {entry.Category}: {entry.Message}\n");
            }
        }
    }

    private void AppendRouteInspector(TradeRoute route)
    {
        if (_snapshot is null || _inspector is null)
        {
            return;
        }

        var from = _snapshot.Cities.FirstOrDefault(city => city.Id == route.FromNode);
        var to = _snapshot.Cities.FirstOrDefault(city => city.Id == route.ToNode);
        var recentLedger = _snapshot.Ledger
            .Where(entry => entry.RelatedId == route.Id)
            .OrderByDescending(entry => entry.Tick)
            .Take(5)
            .ToArray();
        var lastCash = recentLedger.Where(entry => entry.Tick == _snapshot.Tick).Sum(entry => entry.CashDelta);
        var routeDemand = RouteDemandSignal(route.FromNode, route.ToNode);

        _inspector.AppendText($"{route.Id}\n");
        _inspector.AppendText($"{from?.Name ?? route.FromNode} -> {to?.Name ?? route.ToNode} | {route.Mode}\n");
        _inspector.AppendText($"Capacity/day {route.CapacityPerDay} | lead {route.LeadDays}d | cost/unit {route.CostPerUnit:0.00}\n");
        _inspector.AppendText($"Cashflow this tick: {lastCash:+0.00;-0.00;0.00}\n");
        _inspector.AppendText($"Demand signal: {routeDemand}\n");

        if (recentLedger.Length == 0)
        {
            _inspector.AppendText("No deliveries recorded for this route yet.\n");
            return;
        }

        _inspector.AppendText("Recent route ledger:\n");
        foreach (var entry in recentLedger)
        {
            var cash = entry.CashDelta == 0 ? "" : $" ({entry.CashDelta:+0.00;-0.00})";
            _inspector.AppendText($"T{entry.Tick}: {entry.Message}{cash}\n");
        }
    }

    private void UpdateWarnings()
    {
        if (_snapshot is null || _warnings is null)
        {
            return;
        }

        _warnings.Clear();

        var signals = new List<string>();
        var weakestCity = _snapshot.Cities.OrderBy(city => city.SupplySatisfaction).FirstOrDefault();
        if (weakestCity is not null && weakestCity.SupplySatisfaction < 0.80)
        {
            signals.Add($"{weakestCity.Name}: demand unmet, supply {weakestCity.SupplySatisfaction:0.00}");
        }

        var worstRoute = _snapshot.Routes
            .Select(route => new { Route = route, Cash = _snapshot.Ledger.Where(entry => entry.Tick == _snapshot.Tick && entry.RelatedId == route.Id).Sum(entry => entry.CashDelta) })
            .OrderBy(route => route.Cash)
            .FirstOrDefault(route => route.Cash < 0);
        if (worstRoute is not null)
        {
            signals.Add($"{worstRoute.Route.Id}: losing money {worstRoute.Cash:+0.00;-0.00}");
        }

        if (_snapshot.UnmetDemandRatio > 0.65)
        {
            signals.Add($"Network: unmet demand {_snapshot.UnmetDemandRatio:0.0000}");
        }

        if (_snapshot.LastTickCashDelta < 0)
        {
            signals.Add($"Company cashflow fell {_snapshot.LastTickCashDelta:+0.00;-0.00}");
        }

        foreach (var signal in signals.Take(3))
        {
            _warnings.AppendText($"{signal}\n");
        }

        if (signals.Count == 0)
        {
            _warnings.AppendText("No high-priority bottlenecks this tick.\n");
        }
    }

    private static PanelContainer WrapPanel(Control child)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.115f, 0.105f, 0.090f, 0.97f),
            BorderColor = new Color(0.48f, 0.39f, 0.24f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 16,
            ContentMarginTop = 14,
            ContentMarginRight = 16,
            ContentMarginBottom = 14
        };

        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(child);
        return panel;
    }

    private static VBoxContainer CreateStack()
    {
        return new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
    }

    private static Button CreateButton(string text)
    {
        return new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 34)
        };
    }

    private static Label CreateTitle(string text)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 26);
        label.AddThemeColorOverride("font_color", new Color(0.93f, 0.84f, 0.58f, 1.0f));
        return label;
    }

    private static Label CreateSectionLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 15);
        label.AddThemeColorOverride("font_color", new Color(0.89f, 0.83f, 0.67f, 1.0f));
        return label;
    }

    private static Label CreateMutedLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", new Color(0.70f, 0.70f, 0.62f, 1.0f));
        return label;
    }

    private static HSeparator CreateDivider()
    {
        return new HSeparator { CustomMinimumSize = new Vector2(0, 12) };
    }

    private void AddMetric(GridContainer grid, string label)
    {
        var key = CreateMetricLabel(label, new Color(0.62f, 0.69f, 0.69f, 1.0f), HorizontalAlignment.Left);
        var value = CreateMetricLabel("", new Color(0.93f, 0.95f, 0.91f, 1.0f), HorizontalAlignment.Right);
        grid.AddChild(key);
        grid.AddChild(value);
        _metrics[label] = value;
    }

    private void SetMetric(string label, string value)
    {
        if (_metrics.TryGetValue(label, out var control))
        {
            control.Text = value;
        }
    }

    private static Label CreateMetricLabel(string text, Color color, HorizontalAlignment alignment)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
            ClipText = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static RichTextLabel CreateLog()
    {
        var log = new RichTextLabel
        {
            FitContent = false,
            ScrollActive = true,
            SelectionEnabled = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        log.AddThemeFontSizeOverride("normal_font_size", 12);
        log.AddThemeColorOverride("default_color", new Color(0.90f, 0.88f, 0.79f, 1.0f));
        return log;
    }

    private static string ShortHash(string hash)
    {
        return hash.Length <= 18 ? hash : hash[..18];
    }

    private static string StockSummary(IReadOnlyDictionary<string, int> stock)
    {
        var parts = stock
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Take(4)
            .Select(kvp => $"{kvp.Key} {kvp.Value}")
            .ToArray();
        return parts.Length == 0 ? "empty" : string.Join(", ", parts);
    }

    private string RouteDemandSignal(string fromNode, string toNode)
    {
        if (_snapshot is null)
        {
            return "unknown";
        }

        var from = _snapshot.Cities.FirstOrDefault(city => city.Id == fromNode);
        var to = _snapshot.Cities.FirstOrDefault(city => city.Id == toNode);
        if (from is null || to is null)
        {
            return "unknown";
        }

        var fromStock = from.CompanyWarehouse.Values.DefaultIfEmpty(0).Sum();
        var toPressure = 1.0 - to.SupplySatisfaction;
        return $"{from.Name} stock {fromStock}, {to.Name} pressure {toPressure:0.00}";
    }
}

public partial class PrototypeMapView : Control
{
    private readonly Font _font = ThemeDB.FallbackFont;
    private PrototypeSnapshot? _snapshot;
    private string? _selectedCityId;
    private string? _selectedRouteId;
    private string? _hoveredCityId;
    private string? _hoveredRouteId;
    private float _flowPhase;

    public event Action<string>? CitySelected;
    public event Action<string>? RouteSelected;
    public event Action? SelectionCleared;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Process(double delta)
    {
        if (_snapshot is null)
        {
            return;
        }

        _flowPhase = (_flowPhase + (float)delta * 42.0f) % 1000.0f;
        QueueRedraw();
    }

    public void SetSnapshot(PrototypeSnapshot snapshot)
    {
        _snapshot = snapshot;
        QueueRedraw();
    }

    public void SetSelection(string? cityId, string? routeId)
    {
        _selectedCityId = cityId;
        _selectedRouteId = routeId;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_snapshot is null)
        {
            return;
        }

        if (@event is InputEventMouseMotion motion)
        {
            var city = FindCityAt(motion.Position);
            var route = city is null ? FindRouteAt(motion.Position) : null;
            if (_hoveredCityId != city?.Id || _hoveredRouteId != route?.Id)
            {
                _hoveredCityId = city?.Id;
                _hoveredRouteId = route?.Id;
                QueueRedraw();
            }

            return;
        }

        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } button)
        {
            var city = FindCityAt(button.Position);
            if (city is not null)
            {
                CitySelected?.Invoke(city.Id);
                AcceptEvent();
                return;
            }

            var route = FindRouteAt(button.Position);
            if (route is not null)
            {
                RouteSelected?.Invoke(route.Id);
                AcceptEvent();
                return;
            }

            SelectionCleared?.Invoke();
            AcceptEvent();
        }
    }

    public override void _Draw()
    {
        if (_snapshot is null)
        {
            return;
        }

        DrawTerrain(_snapshot);
        DrawRoutes(_snapshot);
        DrawCities(_snapshot);
        DrawLegend(_snapshot);
        DrawHoverLabel(_snapshot);
    }

    private void DrawTerrain(PrototypeSnapshot snapshot)
    {
        var cell = MapScale(snapshot);
        foreach (var terrain in snapshot.World.Terrain)
        {
            var color = terrain.IsWater
                ? new Color(0.26f, 0.43f, 0.48f, 1.0f)
                : new Color(
                    0.55f + (float)(terrain.Fertility * 0.16),
                    0.49f + (float)(terrain.Fertility * 0.18),
                    0.36f + (float)(terrain.Moisture * 0.10),
                    1.0f);
            var point = PointFor(snapshot, terrain.X, terrain.Y);
            DrawRect(new Rect2(point, new Vector2(cell + 1, cell + 1)), color);
        }
    }

    private void DrawRoutes(PrototypeSnapshot snapshot)
    {
        var cities = snapshot.Cities.ToDictionary(city => city.Id, StringComparer.Ordinal);
        foreach (var route in snapshot.Routes)
        {
            if (!cities.TryGetValue(route.FromNode, out var from) || !cities.TryGetValue(route.ToNode, out var to))
            {
                continue;
            }

            var start = PointFor(snapshot, from.X, from.Y);
            var end = PointFor(snapshot, to.X, to.Y);
            var selected = route.Id == _selectedRouteId;
            var related = _selectedCityId is not null && (route.FromNode == _selectedCityId || route.ToNode == _selectedCityId);
            var hovered = route.Id == _hoveredRouteId;
            var cash = LastCashForRoute(snapshot, route.Id);
            var color = RouteColor(route.Mode, cash);
            var alpha = _selectedCityId is not null || _selectedRouteId is not null
                ? selected || related ? 1.0f : 0.25f
                : 0.92f;
            color.A = alpha;

            var width = Math.Clamp(route.CapacityPerDay / 4.5f, 2.0f, 6.5f);
            if (selected)
            {
                DrawLine(start, end, new Color(0.07f, 0.055f, 0.035f, 0.85f), width + 5.0f, true);
                width += 2.5f;
            }
            else if (hovered || related)
            {
                width += 1.2f;
            }

            DrawLine(start, end, color, width, true);
            DrawRoutePulse(start, end, cash >= 0 ? color.Lightened(0.25f) : color, selected || hovered || related);

            if (selected || hovered || cash != 0)
            {
                DrawRouteCashLabel(start, end, cash);
            }
        }
    }

    private void DrawCities(PrototypeSnapshot snapshot)
    {
        foreach (var city in snapshot.Cities)
        {
            var point = PointFor(snapshot, city.X, city.Y);
            var radius = city.Id == "node_001" ? 8.0f : 5.5f;
            var color = city.SupplySatisfaction >= 0.75
                ? new Color(0.86f, 0.66f, 0.22f, 1.0f)
                : new Color(0.72f, 0.18f, 0.14f, 1.0f);
            var selected = city.Id == _selectedCityId;
            var hovered = city.Id == _hoveredCityId;
            var related = _selectedRouteId is not null && snapshot.Routes.Any(route => route.Id == _selectedRouteId && (route.FromNode == city.Id || route.ToNode == city.Id));

            if (selected || hovered || related)
            {
                DrawCircle(point, radius + 6.0f, new Color(0.97f, 0.88f, 0.55f, selected ? 0.55f : 0.35f));
            }

            DrawCircle(point, radius + 2.0f, new Color(0.11f, 0.075f, 0.035f, 1.0f));
            DrawCircle(point, radius, color);
            DrawArc(point, radius + 10.0f, 0.0f, Mathf.Tau * (float)Math.Clamp(city.SupplySatisfaction, 0.0, 1.0), 32, new Color(0.17f, 0.48f, 0.36f, 0.9f), 2.0f, true);
        }
    }

    private void DrawLegend(PrototypeSnapshot snapshot)
    {
        var origin = new Vector2(18, Size.Y - 102);
        DrawRect(new Rect2(origin - new Vector2(10, 16), new Vector2(260, 96)), new Color(0.12f, 0.09f, 0.055f, 0.72f));
        DrawString(_font, origin, "Flow map", HorizontalAlignment.Left, 180, 13);
        DrawLine(origin + new Vector2(0, 22), origin + new Vector2(42, 22), new Color(0.21f, 0.55f, 0.42f, 1.0f), 4.0f, true);
        DrawString(_font, origin + new Vector2(52, 27), "profitable movement", HorizontalAlignment.Left, 170, 12);
        DrawLine(origin + new Vector2(0, 44), origin + new Vector2(42, 44), new Color(0.54f, 0.17f, 0.12f, 1.0f), 4.0f, true);
        DrawString(_font, origin + new Vector2(52, 49), "loss / pressure", HorizontalAlignment.Left, 170, 12);
        DrawCircle(origin + new Vector2(16, 66), 6.0f, new Color(0.86f, 0.66f, 0.22f, 1.0f));
        DrawArc(origin + new Vector2(16, 66), 12.0f, 0.0f, Mathf.Tau * 0.72f, 24, new Color(0.17f, 0.48f, 0.36f, 0.9f), 2.0f, true);
        DrawString(_font, origin + new Vector2(52, 71), "city supply ring", HorizontalAlignment.Left, 170, 12);

        if (_selectedCityId is null && _selectedRouteId is null)
        {
            DrawString(_font, new Vector2(18, 30), "Click a city or route", HorizontalAlignment.Left, Math.Max(120.0f, snapshot.World.Width * MapScale(snapshot)), 15);
        }
    }

    private void DrawHoverLabel(PrototypeSnapshot snapshot)
    {
        if (_hoveredCityId is not null)
        {
            var city = snapshot.Cities.FirstOrDefault(city => city.Id == _hoveredCityId);
            if (city is not null)
            {
                DrawMapLabel(PointFor(snapshot, city.X, city.Y) + new Vector2(12, -10), $"{city.Name} | supply {city.SupplySatisfaction:0.00}");
            }
        }
        else if (_hoveredRouteId is not null)
        {
            var route = snapshot.Routes.FirstOrDefault(route => route.Id == _hoveredRouteId);
            if (route is not null)
            {
                DrawMapLabel(GetRouteMidpoint(snapshot, route.FromNode, route.ToNode) + new Vector2(10, -8), $"{route.Id} | {LastCashForRoute(snapshot, route.Id):+0.00;-0.00;0.00}");
            }
        }
    }

    private void DrawRoutePulse(Vector2 start, Vector2 end, Color color, bool emphasized)
    {
        var direction = end - start;
        var length = direction.Length();
        if (length <= 1.0f)
        {
            return;
        }

        var normal = direction / length;
        var count = emphasized ? 4 : 2;
        for (var i = 0; i < count; i++)
        {
            var offset = (_flowPhase + i * 54.0f) % Math.Max(1.0f, length);
            var point = start + normal * offset;
            DrawCircle(point, emphasized ? 3.4f : 2.4f, color);
        }
    }

    private void DrawRouteCashLabel(Vector2 start, Vector2 end, decimal cash)
    {
        if (cash == 0)
        {
            return;
        }

        var text = cash.ToString("+0.00;-0.00", CultureInfo.InvariantCulture);
        DrawMapLabel((start + end) / 2.0f + new Vector2(8, -8), text);
    }

    private void DrawMapLabel(Vector2 position, string text)
    {
        var size = new Vector2(Math.Max(72, text.Length * 7), 22);
        DrawRect(new Rect2(position + new Vector2(-6, -18), size), new Color(0.09f, 0.065f, 0.04f, 0.82f));
        DrawString(_font, position, text, HorizontalAlignment.Left, size.X - 8, 12);
    }

    private PrototypeCityView? FindCityAt(Vector2 position)
    {
        if (_snapshot is null)
        {
            return null;
        }

        return _snapshot.Cities
            .Select(city => new { City = city, Distance = PointFor(_snapshot, city.X, city.Y).DistanceTo(position) })
            .Where(candidate => candidate.Distance <= 13.0f)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.City)
            .FirstOrDefault();
    }

    private TradeRoute? FindRouteAt(Vector2 position)
    {
        if (_snapshot is null)
        {
            return null;
        }

        var cities = _snapshot.Cities.ToDictionary(city => city.Id, StringComparer.Ordinal);
        return _snapshot.Routes
            .Where(route => cities.ContainsKey(route.FromNode) && cities.ContainsKey(route.ToNode))
            .Select(route => new
            {
                Route = route,
                Distance = DistanceToSegment(
                    position,
                    PointFor(_snapshot, cities[route.FromNode].X, cities[route.FromNode].Y),
                    PointFor(_snapshot, cities[route.ToNode].X, cities[route.ToNode].Y))
            })
            .Where(candidate => candidate.Distance <= 8.0f)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Route)
            .FirstOrDefault();
    }

    private Vector2 GetRouteMidpoint(PrototypeSnapshot snapshot, string fromNode, string toNode)
    {
        var from = snapshot.Cities.FirstOrDefault(city => city.Id == fromNode);
        var to = snapshot.Cities.FirstOrDefault(city => city.Id == toNode);
        if (from is null || to is null)
        {
            return Size / 2.0f;
        }

        return (PointFor(snapshot, from.X, from.Y) + PointFor(snapshot, to.X, to.Y)) / 2.0f;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared <= 0.0001f)
        {
            return point.DistanceTo(start);
        }

        var t = Math.Clamp((point - start).Dot(segment) / lengthSquared, 0.0f, 1.0f);
        var projection = start + segment * t;
        return point.DistanceTo(projection);
    }

    private static Color RouteColor(string mode, decimal cash)
    {
        if (cash > 0)
        {
            return new Color(0.21f, 0.55f, 0.42f, 1.0f);
        }

        if (cash < 0)
        {
            return new Color(0.54f, 0.17f, 0.12f, 1.0f);
        }

        return mode == "coastal"
            ? new Color(0.20f, 0.43f, 0.58f, 1.0f)
            : new Color(0.47f, 0.32f, 0.18f, 1.0f);
    }

    private static decimal LastCashForRoute(PrototypeSnapshot snapshot, string routeId)
    {
        return snapshot.Ledger
            .Where(entry => entry.Tick == snapshot.Tick && entry.RelatedId == routeId)
            .Sum(entry => entry.CashDelta);
    }

    private float MapScale(PrototypeSnapshot snapshot)
    {
        var xScale = Math.Max(1, (Size.X - 26) / Math.Max(1, snapshot.World.Width));
        var yScale = Math.Max(1, (Size.Y - 26) / Math.Max(1, snapshot.World.Height));
        return Math.Min(xScale, yScale);
    }

    private Vector2 PointFor(PrototypeSnapshot snapshot, int x, int y)
    {
        var cell = MapScale(snapshot);
        var width = snapshot.World.Width * cell;
        var height = snapshot.World.Height * cell;
        var offset = new Vector2((Size.X - width) / 2.0f, (Size.Y - height) / 2.0f);
        return offset + new Vector2(x * cell + cell / 2.0f, y * cell + cell / 2.0f);
    }
}
