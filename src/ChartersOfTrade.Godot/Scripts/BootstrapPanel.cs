using System.Globalization;
using ChartersOfTrade.GodotBridge;
using ChartersOfTrade.Logistics.Core;
using Godot;

public enum PrototypeMapMode
{
    Routes,
    Profit,
    Demand
}

[GlobalClass]
public partial class BootstrapPanel : Control
{
    private readonly Dictionary<string, Label> _metrics = [];
    private readonly Dictionary<PrototypeMapMode, Button> _mapModeButtons = [];

    private PrototypeSession? _session;
    private PrototypeSnapshot? _snapshot;
    private PrototypeMapView? _map;
    private RichTextLabel? _ledger;
    private RichTextLabel? _cities;
    private RichTextLabel? _inspector;
    private RichTextLabel? _warnings;
    private Label? _contractSummary;
    private OptionButton? _contractOptions;
    private Button? _contractActionButton;
    private IReadOnlyList<PrototypeRouteContractView> _visibleContracts = [];
    private PrototypeMapMode _mapMode = PrototypeMapMode.Routes;
    private string? _pendingContractId;
    private string? _contractScopeKey;
    private string? _invalidContractId;
    private string? _selectedCityId;
    private string? _selectedRouteId;
    private bool _refreshingContractControl;

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

        sidebar.AddChild(CreateSectionLabel("Map Mode"));
        var mapModes = new HBoxContainer();
        mapModes.AddThemeConstantOverride("separation", 6);
        sidebar.AddChild(mapModes);
        AddMapModeButton(mapModes, "Routes", PrototypeMapMode.Routes);
        AddMapModeButton(mapModes, "Profit", PrototypeMapMode.Profit);
        AddMapModeButton(mapModes, "Demand", PrototypeMapMode.Demand);

        sidebar.AddChild(CreateSectionLabel("Route Contract"));
        var contractStack = CreateStack();
        contractStack.AddThemeConstantOverride("separation", 6);
        _contractSummary = CreateInlineLabel("");
        contractStack.AddChild(_contractSummary);

        _contractOptions = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 30)
        };
        _contractOptions.ItemSelected += OnContractOptionSelected;
        contractStack.AddChild(_contractOptions);

        _contractActionButton = CreateButton("Select Contract");
        _contractActionButton.Pressed += SelectVisibleRouteContract;
        contractStack.AddChild(_contractActionButton);
        sidebar.AddChild(contractStack);

        sidebar.AddChild(CreateSectionLabel("Inspector"));
        _inspector = CreateLog();
        _inspector.CustomMinimumSize = new Vector2(0, 165);
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
        _map?.SetMapMode(_mapMode);
        _map?.SetSelection(_selectedCityId, _selectedRouteId);
        UpdateMapModeButtons();
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
        UpdateContractControl();
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
        _mapModeButtons.Clear();
        _map = null;
        _ledger = null;
        _cities = null;
        _inspector = null;
        _warnings = null;
        _contractSummary = null;
        _contractOptions = null;
        _contractActionButton = null;
        _visibleContracts = [];
        _pendingContractId = null;
        _contractScopeKey = null;
        _invalidContractId = null;
        _refreshingContractControl = false;
    }

    private void SelectCity(string cityId)
    {
        _selectedCityId = cityId;
        _selectedRouteId = null;
        _pendingContractId = null;
        _invalidContractId = null;
        UpdatePrototypeView();
    }

    private void SelectRoute(string routeId)
    {
        _selectedRouteId = routeId;
        _selectedCityId = null;
        _pendingContractId = null;
        _invalidContractId = null;
        UpdatePrototypeView();
    }

    private void ClearSelection()
    {
        _selectedCityId = null;
        _selectedRouteId = null;
        _pendingContractId = null;
        _invalidContractId = null;
        UpdatePrototypeView();
    }

    private void SelectMapMode(PrototypeMapMode mode)
    {
        _mapMode = mode;
        _map?.SetMapMode(_mapMode);
        UpdateMapModeButtons();
        UpdatePrototypeView();
    }

    private void AddMapModeButton(HBoxContainer parent, string text, PrototypeMapMode mode)
    {
        var button = CreateButton(text);
        button.ToggleMode = true;
        button.FocusMode = FocusModeEnum.None;
        button.Pressed += () => SelectMapMode(mode);
        parent.AddChild(button);
        _mapModeButtons[mode] = button;
    }

    private void UpdateMapModeButtons()
    {
        foreach (var (mode, button) in _mapModeButtons)
        {
            button.ButtonPressed = mode == _mapMode;
        }
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
        _inspector.AppendText($"Map mode: {_mapMode}\n");
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
        var pricePressure = city.MarketSignals
            .OrderByDescending(price => price.Scarcity)
            .Take(3)
            .Select(signal => $"{ResourceLabel(signal.ResourceId)} {signal.Price:0.00}, stock {signal.MarketStock}/{signal.DesiredStock}, {signal.Reason}")
            .ToArray();
        var routeLines = connectedRoutes
            .Take(4)
            .Select(route => $"{route.Id} to {OtherEndpointName(route, city.Id)} ({route.Mode}, cap {route.CapacityPerDay}/day, cash {FormatSignedMoney(LastCashForRoute(_snapshot, route.Id))})")
            .ToArray();
        var cityContracts = _snapshot.AvailableContracts
            .Where(contract => contract.FromNode == city.Id || contract.ToNode == city.Id)
            .OrderByDescending(contract => contract.ExpectedNet)
            .ThenBy(contract => contract.Id, StringComparer.Ordinal)
            .ToArray();

        _inspector.AppendText($"{city.Name} ({CityKindLabel(CityKindFor(city.Id))})\n");
        _inspector.AppendText($"Population {city.Population} | level {city.Level}\n");
        _inspector.AppendText($"Supply satisfaction {city.SupplySatisfaction:0.00} | unmet demand {SupplyPressure(city):0.00}\n");
        _inspector.AppendText($"Market stock: {StockSummary(city.MarketStock)}\n");
        _inspector.AppendText($"Company warehouse: {StockSummary(city.CompanyWarehouse)}\n");
        _inspector.AppendText(connectedRoutes.Length > 0
            ? $"Routes serving city ({connectedRoutes.Length}): {string.Join(", ", routeLines)}\n"
            : "Routes serving city: none\n");
        _inspector.AppendText(cityContracts.Length > 0
            ? $"Contracts serving city: {cityContracts.Length}; best {ContractBrief(cityContracts[0])}\n"
            : "Contracts serving city: none currently available\n");
        _inspector.AppendText(pricePressure.Length > 0
            ? $"Local market pressure: {string.Join(", ", pricePressure)}\n"
            : "Local market pressure: no tracked needs\n");

        if (recentLedger.Length > 0)
        {
            _inspector.AppendText("Recent city effects:\n");
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
        var routeDemand = RouteDemandSignal(route);
        var routeContracts = _snapshot.AvailableContracts
            .Where(contract => contract.RouteId == route.Id)
            .OrderByDescending(contract => contract.ExpectedNet)
            .ThenBy(contract => contract.Id, StringComparer.Ordinal)
            .ToArray();
        var selectedContract = SelectedContract();
        var selectedRouteContract = selectedContract is not null && selectedContract.RouteId == route.Id
            ? selectedContract
            : null;

        _inspector.AppendText($"Route {route.Id}\n");
        _inspector.AppendText($"{from?.Name ?? route.FromNode} -> {to?.Name ?? route.ToNode} | {route.Mode}\n");
        _inspector.AppendText($"Capacity {route.CapacityPerDay}/day | lead time {route.LeadDays} {DayLabel(route.LeadDays)} | cost {route.CostPerUnit:0.00}/unit\n");
        _inspector.AppendText($"This tick route cashflow: {FormatSignedMoney(lastCash)}\n");
        _inspector.AppendText($"Endpoint demand: {routeDemand}\n");
        if (selectedRouteContract is not null)
        {
            _inspector.AppendText($"Selected contract: {ContractBrief(selectedRouteContract)}\n");
        }
        else if (routeContracts.Length > 0)
        {
            _inspector.AppendText($"Best contract: {ContractBrief(routeContracts[0])} ({routeContracts.Length} available)\n");
        }
        else
        {
            _inspector.AppendText("Contracts: none currently available for this route\n");
        }

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

        foreach (var city in _snapshot.Cities.Where(city => city.SupplySatisfaction < 0.82).OrderBy(city => city.SupplySatisfaction).Take(2))
        {
            var pressure = TopPressureSignal(city);
            signals.Add(pressure is null
                ? $"! {city.Name}: unmet demand, supply {city.SupplySatisfaction:0.00}"
                : $"! {city.Name}: {ResourceLabel(pressure.ResourceId)} {pressure.MarketStock}/{pressure.DesiredStock}, {pressure.Reason}");
        }

        foreach (var route in _snapshot.Routes
            .Select(route => new { Route = route, Cash = _snapshot.Ledger.Where(entry => entry.Tick == _snapshot.Tick && entry.RelatedId == route.Id).Sum(entry => entry.CashDelta) })
            .OrderBy(route => route.Cash)
            .Where(route => route.Cash < 0)
            .Take(2))
        {
            signals.Add($"! {route.Route.Id}: losing money {route.Cash:+0.00;-0.00}");
        }

        foreach (var route in _snapshot.Routes
            .Select(route => new { Route = route, Pressure = RoutePressure(_snapshot, route) })
            .Where(route => route.Pressure > 0.30 && route.Route.CapacityPerDay <= 12)
            .OrderByDescending(route => route.Pressure)
            .Take(2))
        {
            signals.Add($"! {route.Route.Id}: capacity pressure {route.Pressure:0.00}");
        }

        if (_snapshot.UnmetDemandRatio > 0.65)
        {
            signals.Add($"! Network: unmet demand {_snapshot.UnmetDemandRatio:0.0000}");
        }

        if (_snapshot.LastTickCashDelta < 0)
        {
            signals.Add($"! Company cashflow fell {_snapshot.LastTickCashDelta:+0.00;-0.00}");
        }

        foreach (var signal in signals.Distinct(StringComparer.Ordinal).Take(4))
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

    private static Label CreateInlineLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", new Color(0.82f, 0.83f, 0.76f, 1.0f));
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

    private string RouteDemandSignal(TradeRoute route)
    {
        if (_snapshot is null)
        {
            return "unknown";
        }

        var from = _snapshot.Cities.FirstOrDefault(city => city.Id == route.FromNode);
        var to = _snapshot.Cities.FirstOrDefault(city => city.Id == route.ToNode);
        if (from is null || to is null)
        {
            return "unknown";
        }

        return $"{from.Name} {CityPressureSummary(from)}; {to.Name} {CityPressureSummary(to)}";
    }

    private void UpdateContractControl()
    {
        if (_snapshot is null || _contractSummary is null || _contractOptions is null || _contractActionButton is null)
        {
            return;
        }

        var selectedContractId = _snapshot.SelectedContractId;
        var selectedContract = SelectedContract();
        var scopeKey = ContractScopeKey();
        var sameScope = string.Equals(_contractScopeKey, scopeKey, StringComparison.Ordinal);
        var pendingContractId = _pendingContractId;
        if (sameScope && _visibleContracts.Count > 0 && _contractOptions.Selected >= 0)
        {
            var previousIndex = Math.Clamp(_contractOptions.Selected, 0, _visibleContracts.Count - 1);
            pendingContractId = _visibleContracts[previousIndex].Id;
        }

        _visibleContracts = _snapshot.AvailableContracts
            .Where(ContractAppliesToCurrentSelection)
            .OrderByDescending(contract => contract.ExpectedNet)
            .ThenBy(contract => contract.Id, StringComparer.Ordinal)
            .ToArray();

        var invalidContractId = _invalidContractId;
        if (invalidContractId is null
            && sameScope
            && pendingContractId is not null
            && _snapshot.AvailableContracts.All(contract => contract.Id != pendingContractId))
        {
            invalidContractId = pendingContractId;
        }

        _refreshingContractControl = true;
        _contractOptions.Clear();
        _contractScopeKey = scopeKey;

        if (_visibleContracts.Count == 0)
        {
            _contractOptions.AddItem(EmptyContractOptionLabel());
            _contractOptions.Disabled = true;
            _contractActionButton.Disabled = true;
            _contractActionButton.Text = "No Contracts";
            _pendingContractId = null;
            _contractSummary.Text = EmptyContractSummary(invalidContractId, selectedContract);
            _invalidContractId = null;
            _refreshingContractControl = false;
            return;
        }

        var preferredContractId = _visibleContracts.Any(contract => string.Equals(contract.Id, selectedContractId, StringComparison.Ordinal))
            ? selectedContractId
            : _visibleContracts.Any(contract => string.Equals(contract.Id, pendingContractId, StringComparison.Ordinal))
                ? pendingContractId
                : null;
        var selectedIndex = 0;
        for (var i = 0; i < _visibleContracts.Count; i++)
        {
            var contract = _visibleContracts[i];
            _contractOptions.AddItem(ContractOptionLabel(contract, i), i);
            if (string.Equals(contract.Id, preferredContractId, StringComparison.Ordinal))
            {
                selectedIndex = i;
            }
        }

        _contractOptions.Select(selectedIndex);
        _contractOptions.Disabled = false;
        _refreshingContractControl = false;
        RefreshContractSummary(selectedContractId, invalidContractId, selectedContract);
        _invalidContractId = null;
    }

    private void OnContractOptionSelected(long _)
    {
        if (_refreshingContractControl)
        {
            return;
        }

        _invalidContractId = null;
        RefreshContractSummary(_snapshot?.SelectedContractId, selectedContract: SelectedContract());
    }

    private void RefreshContractSummary(
        string? selectedContractId,
        string? invalidContractId = null,
        PrototypeRouteContractView? selectedContract = null)
    {
        if (_contractSummary is null || _contractOptions is null || _contractActionButton is null || _visibleContracts.Count == 0)
        {
            return;
        }

        var candidate = _visibleContracts[Math.Clamp(_contractOptions.Selected, 0, _visibleContracts.Count - 1)];
        var candidateIsSelected = selectedContractId is not null && string.Equals(candidate.Id, selectedContractId, StringComparison.Ordinal);
        var candidateIsBest = string.Equals(candidate.Id, _visibleContracts[0].Id, StringComparison.Ordinal);
        var summaryLead = candidateIsSelected
            ? "Selected contract"
            : candidateIsBest
                ? "Best available"
                : "Preview contract";
        var lines = new List<string>();

        if (invalidContractId is not null)
        {
            lines.Add($"Previous contract {invalidContractId} is no longer available.");
        }

        lines.Add($"{summaryLead}: {ContractSummary(candidate)}");
        if (!candidateIsSelected && selectedContract is not null)
        {
            lines.Add($"Selected contract remains: {ContractBrief(selectedContract)}.");
        }

        _pendingContractId = candidate.Id;
        _contractSummary.Text = string.Join("\n", lines);
        _contractActionButton.Disabled = candidateIsSelected;
        _contractActionButton.Text = candidateIsSelected
            ? "Selected Contract"
            : selectedContract is null
                ? "Select Contract"
                : "Switch Contract";
    }

    private void SelectVisibleRouteContract()
    {
        if (_session is null || _contractOptions is null || _visibleContracts.Count == 0)
        {
            return;
        }

        var index = Math.Clamp(_contractOptions.Selected, 0, _visibleContracts.Count - 1);
        var contractId = _visibleContracts[index].Id;
        if (!_session.SelectRouteContract(contractId))
        {
            _invalidContractId = contractId;
        }
        else
        {
            _invalidContractId = null;
            _pendingContractId = contractId;
        }

        _snapshot = _session.Current;

        KeepValidSelection();
        UpdatePrototypeView();
    }

    private bool ContractAppliesToCurrentSelection(PrototypeRouteContractView contract)
    {
        if (_selectedRouteId is not null)
        {
            return string.Equals(contract.RouteId, _selectedRouteId, StringComparison.Ordinal);
        }

        if (_selectedCityId is not null)
        {
            return string.Equals(contract.FromNode, _selectedCityId, StringComparison.Ordinal)
                || string.Equals(contract.ToNode, _selectedCityId, StringComparison.Ordinal);
        }

        return true;
    }

    private string ContractScopeKey()
    {
        if (_selectedRouteId is not null)
        {
            return $"route:{_selectedRouteId}";
        }

        if (_selectedCityId is not null)
        {
            return $"city:{_selectedCityId}";
        }

        return "map";
    }

    private string EmptyContractOptionLabel()
    {
        if (_selectedRouteId is not null)
        {
            return "No contracts on this route";
        }

        if (_selectedCityId is not null)
        {
            return "No contracts for this city";
        }

        return "No route contracts available";
    }

    private string EmptyContractSummary(string? invalidContractId, PrototypeRouteContractView? selectedContract)
    {
        var lines = new List<string>();
        if (invalidContractId is not null)
        {
            lines.Add($"Previous contract {invalidContractId} is no longer available.");
        }

        lines.Add(_selectedRouteId is not null
            ? $"No route contracts are currently available on {RouteDisplayName(_selectedRouteId)}."
            : _selectedCityId is not null
                ? $"No route contracts currently serve {CityName(_selectedCityId)}."
                : "No route contracts are currently available.");

        if (selectedContract is not null)
        {
            lines.Add($"Selected contract remains: {ContractBrief(selectedContract)}.");
        }

        return string.Join("\n", lines);
    }

    private string ContractOptionLabel(PrototypeRouteContractView contract, int index)
    {
        var rank = index == 0 ? "Best" : $"#{index + 1}";
        return $"{rank}: {ResourceLabel(contract.ResourceId)} | {CityName(contract.FromNode)} -> {CityName(contract.ToNode)} | {FormatSignedMoney(contract.ExpectedNet)} net";
    }

    private string ContractSummary(PrototypeRouteContractView contract)
    {
        return $"{ResourceLabel(contract.ResourceId)} from {CityName(contract.FromNode)} to {CityName(contract.ToNode)} on {RouteDisplayName(contract.RouteId)}; revenue {contract.ExpectedRevenue.ToString("0.00", CultureInfo.InvariantCulture)}, cost {contract.TransportCost.ToString("0.00", CultureInfo.InvariantCulture)}, net {FormatSignedMoney(contract.ExpectedNet)}, capacity {contract.CapacityPerDay}/day";
    }

    private string ContractBrief(PrototypeRouteContractView contract)
    {
        return $"{ResourceLabel(contract.ResourceId)} {CityName(contract.FromNode)} -> {CityName(contract.ToNode)} on {contract.RouteId} ({FormatSignedMoney(contract.ExpectedNet)} net)";
    }

    private PrototypeRouteContractView? SelectedContract()
    {
        if (_snapshot?.SelectedContractId is null)
        {
            return null;
        }

        return _snapshot.AvailableContracts.FirstOrDefault(contract => contract.Id == _snapshot.SelectedContractId);
    }

    private string CityName(string cityId)
    {
        return _snapshot?.Cities.FirstOrDefault(city => city.Id == cityId)?.Name ?? cityId;
    }

    private string OtherEndpointName(TradeRoute route, string cityId)
    {
        var otherId = route.FromNode == cityId ? route.ToNode : route.FromNode;
        return CityName(otherId);
    }

    private string RouteDisplayName(string routeId)
    {
        var route = _snapshot?.Routes.FirstOrDefault(route => route.Id == routeId);
        return route is null
            ? routeId
            : $"{route.Id} ({CityName(route.FromNode)} -> {CityName(route.ToNode)})";
    }

    private static string ResourceLabel(string resourceId)
    {
        var words = resourceId.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0
            ? resourceId
            : string.Join(" ", words.Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant())));
    }

    private static string FormatSignedMoney(decimal value)
    {
        return value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
    }

    private static int StockUnits(IReadOnlyDictionary<string, int> stock)
    {
        return stock.Values.DefaultIfEmpty(0).Sum();
    }

    private static string DayLabel(int days)
    {
        return days == 1 ? "day" : "days";
    }

    private static string CityKindLabel(string kind)
    {
        return kind switch
        {
            "charter_town" => "charter town",
            "port" => "port",
            _ => "market town"
        };
    }

    private string CityKindFor(string cityId)
    {
        return _snapshot?.World.Nodes.FirstOrDefault(node => node.Id == cityId)?.Kind ?? "market_town";
    }

    private PrototypeMarketSignal? TopPressureSignal(PrototypeCityView city)
    {
        return city.MarketSignals
            .Where(signal => signal.DesiredStock > 0 && signal.Scarcity > 0.10)
            .OrderByDescending(signal => signal.Scarcity)
            .ThenBy(signal => signal.ResourceId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private string CityPressureSummary(PrototypeCityView city)
    {
        var pressure = TopPressureSignal(city);
        return pressure is null
            ? $"stable demand, warehouse {StockUnits(city.CompanyWarehouse)}"
            : $"{ResourceLabel(pressure.ResourceId)} {pressure.MarketStock}/{pressure.DesiredStock}, unmet {SupplyPressure(city):0.00}";
    }

    private static double SupplyPressure(PrototypeCityView city)
    {
        return Math.Clamp(1.0 - city.SupplySatisfaction, 0.0, 1.0);
    }

    private static double RoutePressure(PrototypeSnapshot snapshot, TradeRoute route)
    {
        var from = snapshot.Cities.FirstOrDefault(city => city.Id == route.FromNode);
        var to = snapshot.Cities.FirstOrDefault(city => city.Id == route.ToNode);
        return Math.Max(from is null ? 0 : SupplyPressure(from), to is null ? 0 : SupplyPressure(to));
    }

    private static decimal LastCashForRoute(PrototypeSnapshot snapshot, string routeId)
    {
        return snapshot.Ledger
            .Where(entry => entry.Tick == snapshot.Tick && entry.RelatedId == routeId)
            .Sum(entry => entry.CashDelta);
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
    private PrototypeMapMode _mapMode = PrototypeMapMode.Routes;
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

    public void SetMapMode(PrototypeMapMode mode)
    {
        _mapMode = mode;
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
            var terrainFade = _mapMode == PrototypeMapMode.Demand ? 0.78f : 1.0f;
            var color = terrain.IsWater
                ? new Color(0.19f, 0.34f, 0.40f, 1.0f)
                : new Color(
                    (0.50f + (float)(terrain.Fertility * 0.14)) * terrainFade,
                    (0.48f + (float)(terrain.Fertility * 0.16)) * terrainFade,
                    (0.36f + (float)(terrain.Moisture * 0.08)) * terrainFade,
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
            var pressure = RoutePressure(snapshot, route);
            var color = RouteColor(route.Mode, cash, pressure, _mapMode);
            var alpha = _selectedCityId is not null || _selectedRouteId is not null
                ? selected || related ? 1.0f : 0.25f
                : _mapMode == PrototypeMapMode.Demand && pressure < 0.20 ? 0.42f : 0.92f;
            color.A = alpha;

            var width = RouteWidth(route, cash, pressure, _mapMode);
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

            if (cash < 0)
            {
                DrawWarningMark((start + end) / 2.0f + new Vector2(-10, 10));
            }
            else if (_mapMode == PrototypeMapMode.Demand && pressure > 0.35)
            {
                DrawWarningMark((start + end) / 2.0f + new Vector2(-10, 10));
            }

            if ((_mapMode == PrototypeMapMode.Profit && cash != 0) || selected || hovered)
            {
                DrawRouteCashLabel(start, end, cash);
            }
            else if (_mapMode == PrototypeMapMode.Routes && related)
            {
                DrawMapLabel((start + end) / 2.0f + new Vector2(8, -8), $"{route.Mode} cap {route.CapacityPerDay}");
            }
            else if (_mapMode == PrototypeMapMode.Demand && pressure > 0.30)
            {
                DrawMapLabel((start + end) / 2.0f + new Vector2(8, -8), $"pressure {pressure:0.00}");
            }
        }
    }

    private void DrawCities(PrototypeSnapshot snapshot)
    {
        foreach (var city in snapshot.Cities)
        {
            var point = PointFor(snapshot, city.X, city.Y);
            var kind = CityKindFor(snapshot, city.Id);
            var pressure = SupplyPressure(city);
            var radius = kind == "charter_town" ? 8.5f : kind == "port" ? 7.0f : 6.0f;
            var color = _mapMode == PrototypeMapMode.Demand
                ? DemandCityColor(pressure)
                : CityKindColor(kind, city.SupplySatisfaction);
            var selected = city.Id == _selectedCityId;
            var hovered = city.Id == _hoveredCityId;
            var related = _selectedRouteId is not null && snapshot.Routes.Any(route => route.Id == _selectedRouteId && (route.FromNode == city.Id || route.ToNode == city.Id));

            if (selected || hovered || related)
            {
                DrawCircle(point, radius + 6.0f, new Color(0.97f, 0.88f, 0.55f, selected ? 0.55f : 0.35f));
            }

            DrawCircle(point, radius + 2.0f, new Color(0.11f, 0.075f, 0.035f, 1.0f));
            DrawCityStamp(point, kind, radius, color);
            DrawArc(point, radius + 10.0f, 0.0f, Mathf.Tau * (float)Math.Clamp(city.SupplySatisfaction, 0.0, 1.0), 32, new Color(0.17f, 0.48f, 0.36f, 0.9f), 2.0f, true);

            if (pressure > 0.22)
            {
                DrawArc(point, radius + 14.0f, 0.0f, Mathf.Tau * (float)Math.Clamp(pressure, 0.0, 1.0), 32, new Color(0.62f, 0.17f, 0.14f, 0.78f), 2.0f, true);
            }

            if (city.SupplySatisfaction < 0.80)
            {
                DrawWarningMark(point + new Vector2(radius + 8.0f, -radius - 5.0f));
            }

            if (selected || hovered || (_mapMode == PrototypeMapMode.Demand && pressure > 0.25))
            {
                DrawMapLabel(point + new Vector2(12, -10), $"{city.Name} | supply {city.SupplySatisfaction:0.00}");
            }
        }
    }

    private void DrawLegend(PrototypeSnapshot snapshot)
    {
        var origin = new Vector2(18, Size.Y - 122);
        DrawRect(new Rect2(origin - new Vector2(10, 16), new Vector2(288, 116)), new Color(0.12f, 0.09f, 0.055f, 0.74f));
        DrawString(_font, origin, $"{_mapMode} mode", HorizontalAlignment.Left, 180, 13);

        if (_mapMode == PrototypeMapMode.Routes)
        {
            DrawLine(origin + new Vector2(0, 22), origin + new Vector2(44, 22), new Color(0.22f, 0.45f, 0.63f, 1.0f), 5.0f, true);
            DrawString(_font, origin + new Vector2(54, 27), "coastal capacity", HorizontalAlignment.Left, 190, 12);
            DrawLine(origin + new Vector2(0, 46), origin + new Vector2(44, 46), new Color(0.56f, 0.38f, 0.18f, 1.0f), 3.0f, true);
            DrawString(_font, origin + new Vector2(54, 51), "road capacity", HorizontalAlignment.Left, 190, 12);
        }
        else if (_mapMode == PrototypeMapMode.Profit)
        {
            DrawLine(origin + new Vector2(0, 22), origin + new Vector2(44, 22), new Color(0.21f, 0.55f, 0.42f, 1.0f), 4.0f, true);
            DrawString(_font, origin + new Vector2(54, 27), "profitable movement", HorizontalAlignment.Left, 190, 12);
            DrawLine(origin + new Vector2(0, 46), origin + new Vector2(44, 46), new Color(0.62f, 0.17f, 0.14f, 1.0f), 4.0f, true);
            DrawString(_font, origin + new Vector2(54, 51), "route losing money", HorizontalAlignment.Left, 190, 12);
        }
        else
        {
            DrawCircle(origin + new Vector2(16, 24), 7.0f, new Color(0.62f, 0.17f, 0.14f, 1.0f));
            DrawArc(origin + new Vector2(16, 24), 14.0f, 0.0f, Mathf.Tau * 0.58f, 24, new Color(0.62f, 0.17f, 0.14f, 0.78f), 2.0f, true);
            DrawString(_font, origin + new Vector2(54, 29), "unmet demand pressure", HorizontalAlignment.Left, 190, 12);
            DrawLine(origin + new Vector2(0, 48), origin + new Vector2(44, 48), new Color(0.71f, 0.48f, 0.14f, 1.0f), 4.0f, true);
            DrawString(_font, origin + new Vector2(54, 53), "pressure on route", HorizontalAlignment.Left, 190, 12);
        }

        DrawCityStamp(origin + new Vector2(12, 74), "charter_town", 7.0f, CityKindColor("charter_town", 1.0));
        DrawString(_font, origin + new Vector2(32, 79), "C charter", HorizontalAlignment.Left, 82, 12);
        DrawCityStamp(origin + new Vector2(118, 74), "port", 6.0f, CityKindColor("port", 1.0));
        DrawString(_font, origin + new Vector2(136, 79), "P port", HorizontalAlignment.Left, 70, 12);
        DrawCityStamp(origin + new Vector2(206, 74), "market_town", 6.0f, CityKindColor("market_town", 1.0));
        DrawString(_font, origin + new Vector2(224, 79), "M market", HorizontalAlignment.Left, 70, 12);

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

    private static string CityKindFor(PrototypeSnapshot snapshot, string cityId)
    {
        return snapshot.World.Nodes.FirstOrDefault(node => node.Id == cityId)?.Kind ?? "market_town";
    }

    private static Color CityKindColor(string kind, double supplySatisfaction)
    {
        if (supplySatisfaction < 0.75)
        {
            return new Color(0.62f, 0.17f, 0.14f, 1.0f);
        }

        return kind switch
        {
            "charter_town" => new Color(0.81f, 0.62f, 0.20f, 1.0f),
            "port" => new Color(0.22f, 0.50f, 0.61f, 1.0f),
            _ => new Color(0.40f, 0.58f, 0.39f, 1.0f)
        };
    }

    private static Color DemandCityColor(double pressure)
    {
        return pressure switch
        {
            > 0.45 => new Color(0.62f, 0.17f, 0.14f, 1.0f),
            > 0.25 => new Color(0.71f, 0.48f, 0.14f, 1.0f),
            _ => new Color(0.29f, 0.52f, 0.40f, 1.0f)
        };
    }

    private void DrawCityStamp(Vector2 point, string kind, float radius, Color color)
    {
        var ink = new Color(0.09f, 0.065f, 0.04f, 1.0f);
        if (kind == "charter_town")
        {
            var size = new Vector2(radius * 2.1f, radius * 2.1f);
            DrawRect(new Rect2(point - size / 2.0f, size), ink);
            DrawRect(new Rect2(point - size / 2.0f + new Vector2(2, 2), size - new Vector2(4, 4)), color);
            DrawString(_font, point + new Vector2(-4, 5), "C", HorizontalAlignment.Left, 16, 11);
            return;
        }

        if (kind == "port")
        {
            var diamond =
                new[]
                {
                    point + new Vector2(0, -radius - 2),
                    point + new Vector2(radius + 2, 0),
                    point + new Vector2(0, radius + 2),
                    point + new Vector2(-radius - 2, 0),
                    point + new Vector2(0, -radius - 2)
                };
            DrawPolyline(diamond, ink, 3.0f, true);
            DrawPolyline(diamond, color, 2.0f, true);
            DrawString(_font, point + new Vector2(-4, 5), "P", HorizontalAlignment.Left, 16, 11);
            return;
        }

        DrawCircle(point, radius + 1.8f, ink);
        DrawCircle(point, radius, color);
        DrawString(_font, point + new Vector2(-5, 5), "M", HorizontalAlignment.Left, 16, 11);
    }

    private void DrawWarningMark(Vector2 point)
    {
        DrawCircle(point, 6.5f, new Color(0.10f, 0.065f, 0.035f, 0.95f));
        DrawCircle(point, 5.0f, new Color(0.72f, 0.18f, 0.14f, 0.95f));
        DrawString(_font, point + new Vector2(-2.8f, 4.0f), "!", HorizontalAlignment.Left, 12, 11);
    }

    private static float RouteWidth(TradeRoute route, decimal cash, double pressure, PrototypeMapMode mode)
    {
        return mode switch
        {
            PrototypeMapMode.Profit => Math.Clamp(2.0f + (float)Math.Min(4.0m, Math.Abs(cash) / 3.0m), 2.0f, 7.0f),
            PrototypeMapMode.Demand => Math.Clamp(2.0f + (float)pressure * 5.0f, 2.0f, 7.0f),
            _ => Math.Clamp(route.CapacityPerDay / 3.8f, 2.4f, 7.0f)
        };
    }

    private static Color RouteColor(string mode, decimal cash, double pressure, PrototypeMapMode mapMode)
    {
        if (mapMode == PrototypeMapMode.Demand)
        {
            return pressure switch
            {
                > 0.45 => new Color(0.62f, 0.17f, 0.14f, 1.0f),
                > 0.25 => new Color(0.71f, 0.48f, 0.14f, 1.0f),
                _ => new Color(0.30f, 0.42f, 0.39f, 1.0f)
            };
        }

        if (mapMode == PrototypeMapMode.Profit && cash > 0)
        {
            return new Color(0.21f, 0.55f, 0.42f, 1.0f);
        }

        if (mapMode == PrototypeMapMode.Profit && cash < 0)
        {
            return new Color(0.62f, 0.17f, 0.14f, 1.0f);
        }

        return mode == "coastal"
            ? new Color(0.22f, 0.45f, 0.63f, 1.0f)
            : new Color(0.56f, 0.38f, 0.18f, 1.0f);
    }

    private static double SupplyPressure(PrototypeCityView city)
    {
        return Math.Clamp(1.0 - city.SupplySatisfaction, 0.0, 1.0);
    }

    private static double RoutePressure(PrototypeSnapshot snapshot, TradeRoute route)
    {
        var from = snapshot.Cities.FirstOrDefault(city => city.Id == route.FromNode);
        var to = snapshot.Cities.FirstOrDefault(city => city.Id == route.ToNode);
        return Math.Max(from is null ? 0 : SupplyPressure(from), to is null ? 0 : SupplyPressure(to));
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
