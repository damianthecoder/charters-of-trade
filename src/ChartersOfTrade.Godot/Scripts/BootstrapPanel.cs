using System.Globalization;
using ChartersOfTrade.GodotBridge;
using ChartersOfTrade.Logistics.Core;
using ChartersOfTrade.WorldGen.Core;
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
    private RichTextLabel? _policy;
    private RichTextLabel? _testProbe;
    private SpinBox? _seedInput;
    private Label? _contractSummary;
    private OptionButton? _contractOptions;
    private Button? _contractActionButton;
    private Label? _warehousePolicySummary;
    private OptionButton? _warehouseResourceOptions;
    private SpinBox? _warehouseSafetyInput;
    private SpinBox? _warehouseReorderInput;
    private Button? _warehouseApplyButton;
    private IReadOnlyList<PrototypeRouteContractView> _visibleContracts = [];
    private IReadOnlyList<PrototypeMarketSignal> _visibleWarehousePolicies = [];
    private PrototypeMapMode _mapMode = PrototypeMapMode.Routes;
    private string? _pendingContractId;
    private string? _contractScopeKey;
    private string? _warehousePolicyMessage;
    private string? _invalidContractId;
    private string? _selectedCityId;
    private string? _selectedRouteId;
    private bool _refreshingContractControl;
    private bool _refreshingWarehousePolicyControl;

    private sealed record LayoutProfile(
        int OuterMargin,
        int RootSeparation,
        Vector2 MapMinimum,
        float SidebarWidth,
        float InspectorHeight,
        float WarningHeight,
        float PolicyHeight,
        float CitiesHeight,
        float ProbeHeight);

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
        var layout = LayoutFor(GetViewportRect().Size);

        var background = new ColorRect { Color = new Color(0.065f, 0.070f, 0.070f, 1.0f) };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", layout.OuterMargin);
        margin.AddThemeConstantOverride("margin_top", layout.OuterMargin);
        margin.AddThemeConstantOverride("margin_right", layout.OuterMargin);
        margin.AddThemeConstantOverride("margin_bottom", layout.OuterMargin);
        AddChild(margin);

        var root = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", layout.RootSeparation);
        margin.AddChild(root);

        _map = new PrototypeMapView
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = layout.MapMinimum
        };
        _map.CitySelected += SelectCity;
        _map.RouteSelected += SelectRoute;
        _map.SelectionCleared += ClearSelection;
        root.AddChild(WrapPanel(_map));

        var sidebarScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(layout.SidebarWidth + 22, layout.MapMinimum.Y),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        var sidebarInset = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(layout.SidebarWidth, 0)
        };
        sidebarInset.AddThemeConstantOverride("margin_right", 24);
        sidebarScroll.AddChild(sidebarInset);

        var sidebar = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(layout.SidebarWidth, 0)
        };
        sidebar.AddThemeConstantOverride("separation", 12);
        sidebarInset.AddChild(sidebar);
        root.AddChild(WrapPanel(sidebarScroll, horizontalExpand: false, minimumWidth: layout.SidebarWidth + 58));

        sidebar.AddChild(CreateTitle("Charters of Trade"));
        sidebar.AddChild(CreateMutedLabel("Prototype systems loop"));
        sidebar.AddChild(CreateDivider());

        var metricGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        AddMetric(metricGrid, "Tick");
        AddMetric(metricGrid, "Day");
        AddMetric(metricGrid, "Cash");
        AddMetric(metricGrid, "Cashflow");
        AddMetric(metricGrid, "Save Hash");
        AddMetric(metricGrid, "AI Move");
        AddMetric(metricGrid, "Unmet Demand");
        sidebar.AddChild(CreateSectionPanel("Company Ledger", metricGrid));

        var testStack = CreateSectionStack();
        testStack.AddChild(CreateSectionHint("Seed resets the same world, economy, routes, and save hash. Tick buttons advance the daily simulation so changes can be traced."));
        var seedRow = new HBoxContainer();
        seedRow.AddThemeConstantOverride("separation", 8);
        testStack.AddChild(seedRow);
        seedRow.AddChild(CreateMetricLabel("Seed", new Color(0.62f, 0.69f, 0.69f, 1.0f), HorizontalAlignment.Left));
        _seedInput = new SpinBox
        {
            MinValue = 1,
            MaxValue = int.MaxValue,
            Step = 1,
            Rounded = true,
            Value = Seed,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 32)
        };
        seedRow.AddChild(_seedInput);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        testStack.AddChild(actions);

        var tickButton = CreateButton("Advance Tick");
        tickButton.Pressed += AdvanceOneTick;
        actions.AddChild(tickButton);

        var runButton = CreateButton("Run 5");
        runButton.Pressed += AdvanceFiveTicks;
        actions.AddChild(runButton);

        var runTwelveButton = CreateButton("Run 12");
        runTwelveButton.Pressed += AdvanceTwelveTicks;
        actions.AddChild(runTwelveButton);

        var resetButton = CreateButton("Reset Seed");
        resetButton.Pressed += ResetSeed;
        testStack.AddChild(resetButton);

        _testProbe = CreateLog();
        _testProbe.CustomMinimumSize = new Vector2(0, layout.ProbeHeight);
        testStack.AddChild(_testProbe);
        sidebar.AddChild(CreateSectionPanel("System Test Bench", testStack));

        var mapModeStack = CreateSectionStack();
        mapModeStack.AddChild(CreateSectionHint("Routes shows capacity, Profit shows this-tick cash, Demand shows city shortage pressure from local stock and reorder policy."));
        var mapModes = new HBoxContainer();
        mapModes.AddThemeConstantOverride("separation", 6);
        mapModeStack.AddChild(mapModes);
        AddMapModeButton(mapModes, "Routes", PrototypeMapMode.Routes);
        AddMapModeButton(mapModes, "Profit", PrototypeMapMode.Profit);
        AddMapModeButton(mapModes, "Demand", PrototypeMapMode.Demand);
        sidebar.AddChild(CreateSectionPanel("Map Mode", mapModeStack));

        var contractStack = CreateSectionStack();
        contractStack.AddChild(CreateSectionHint("Contracts are concrete logistics orders: source warehouse stock, destination demand, capacity, transport cost, and expected net."));
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
        sidebar.AddChild(CreateSectionPanel("Route Contract", contractStack));

        _inspector = CreateLog();
        _inspector.CustomMinimumSize = new Vector2(0, layout.InspectorHeight);
        sidebar.AddChild(CreateSectionPanel("Inspector", _inspector));

        _warnings = CreateLog();
        _warnings.CustomMinimumSize = new Vector2(0, layout.WarningHeight);
        sidebar.AddChild(CreateSectionPanel("Market Pressure", _warnings));

        var warehousePolicyStack = CreateSectionStack();
        warehousePolicyStack.AddChild(CreateSectionHint("Controls how much company stock this city protects before routes or contracts can export it."));
        _warehousePolicySummary = CreateInlineLabel("Select a city to control warehouse policy.");
        warehousePolicyStack.AddChild(_warehousePolicySummary);

        _warehouseResourceOptions = new OptionButton
        {
            Name = "WarehouseResourceOptions",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 30)
        };
        _warehouseResourceOptions.ItemSelected += OnWarehouseResourceSelected;
        warehousePolicyStack.AddChild(_warehouseResourceOptions);

        var warehousePolicyGrid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        warehousePolicyStack.AddChild(warehousePolicyGrid);
        warehousePolicyGrid.AddChild(CreateMetricLabel("Safety", new Color(0.62f, 0.69f, 0.69f, 1.0f), HorizontalAlignment.Left));
        _warehouseSafetyInput = CreatePolicySpinBox("WarehouseSafetyInput");
        warehousePolicyGrid.AddChild(_warehouseSafetyInput);
        warehousePolicyGrid.AddChild(CreateMetricLabel("Reorder", new Color(0.62f, 0.69f, 0.69f, 1.0f), HorizontalAlignment.Left));
        _warehouseReorderInput = CreatePolicySpinBox("WarehouseReorderInput");
        warehousePolicyGrid.AddChild(_warehouseReorderInput);

        _warehouseApplyButton = CreateButton("Apply Warehouse Policy");
        _warehouseApplyButton.Pressed += ApplyWarehousePolicy;
        warehousePolicyStack.AddChild(_warehouseApplyButton);

        _policy = CreateLog();
        _policy.CustomMinimumSize = new Vector2(0, layout.PolicyHeight);
        warehousePolicyStack.AddChild(_policy);
        sidebar.AddChild(CreateSectionPanel("Warehouse Policy", warehousePolicyStack));

        _cities = CreateLog();
        _cities.CustomMinimumSize = new Vector2(0, layout.CitiesHeight);
        sidebar.AddChild(CreateSectionPanel("City Network", _cities));

        _ledger = CreateLog();
        _ledger.SizeFlagsVertical = SizeFlags.ExpandFill;
        sidebar.AddChild(CreateSectionPanel("Event Ledger", _ledger, verticalExpand: true, minimumHeight: 160));
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
        AdvanceTicks(1);
    }

    private void AdvanceFiveTicks()
    {
        AdvanceTicks(5);
    }

    private void AdvanceTwelveTicks()
    {
        AdvanceTicks(12);
    }

    private void AdvanceTicks(int count)
    {
        if (_session is null)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            _snapshot = _session.AdvanceTick();
        }

        KeepValidSelection();
        UpdatePrototypeView();
    }

    private void ResetSeed()
    {
        if (_seedInput is not null)
        {
            Seed = Math.Clamp((int)_seedInput.Value, 1, int.MaxValue);
        }

        try
        {
            _session = new SimulationBridge().CreatePrototypeSession(Seed);
            _snapshot = _session.Current;
            _selectedCityId = null;
            _selectedRouteId = null;
            _pendingContractId = null;
            _invalidContractId = null;
            _warehousePolicyMessage = null;
            UpdatePrototypeView();
        }
        catch (Exception ex)
        {
            BuildFailureView(ex);
        }
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
        UpdateWarehousePolicyControl();
        UpdatePolicyPanel();
        UpdateTestProbe();

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
        _policy = null;
        _testProbe = null;
        _seedInput = null;
        _contractSummary = null;
        _contractOptions = null;
        _contractActionButton = null;
        _warehousePolicySummary = null;
        _warehouseResourceOptions = null;
        _warehouseSafetyInput = null;
        _warehouseReorderInput = null;
        _warehouseApplyButton = null;
        _visibleContracts = [];
        _visibleWarehousePolicies = [];
        _pendingContractId = null;
        _contractScopeKey = null;
        _warehousePolicyMessage = null;
        _invalidContractId = null;
        _refreshingContractControl = false;
        _refreshingWarehousePolicyControl = false;
    }

    private void SelectCity(string cityId)
    {
        _selectedCityId = cityId;
        _selectedRouteId = null;
        _pendingContractId = null;
        _invalidContractId = null;
        _warehousePolicyMessage = null;
        UpdatePrototypeView();
    }

    private void SelectRoute(string routeId)
    {
        _selectedRouteId = routeId;
        _selectedCityId = null;
        _pendingContractId = null;
        _invalidContractId = null;
        _warehousePolicyMessage = null;
        UpdatePrototypeView();
    }

    private void ClearSelection()
    {
        _selectedCityId = null;
        _selectedRouteId = null;
        _pendingContractId = null;
        _invalidContractId = null;
        _warehousePolicyMessage = null;
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
            .OrderByDescending(signal => signal.ShipmentPriority)
            .ThenByDescending(signal => signal.Scarcity)
            .Take(3)
            .Select(signal => $"{ResourceLabel(signal.ResourceId)} {signal.MarketStock}/{signal.DesiredStock}, safety {signal.SafetyStock}, reorder {signal.ReorderPoint}, {signal.PolicyAction}")
            .ToArray();
        var routeLines = connectedRoutes
            .Take(4)
            .Select(route => $"{route.Id} to {OtherEndpointName(route, city.Id)} ({route.Mode}, cap {route.CapacityPerDay}/day, cash {FormatSignedMoney(LastCashForRoute(_snapshot, route.Id))})")
            .ToArray();
        var cityContracts = _snapshot.AvailableContracts
            .Where(contract => contract.FromNode == city.Id || contract.ToNode == city.Id)
            .ToArray();

        _inspector.AppendText($"{city.Name} ({CityKindLabel(CityKindFor(city.Id))})\n");
        _inspector.AppendText($"Population {city.Population} | level {city.Level}\n");
        _inspector.AppendText($"Market Pressure: supply {city.SupplySatisfaction:0.00}, unmet demand {SupplyPressure(city):0.00}\n");
        _inspector.AppendText($"Local prices read market stock: {StockSummary(city.MarketStock)}\n");
        _inspector.AppendText($"Company warehouse: {StockSummary(city.CompanyWarehouse)}\n");
        _inspector.AppendText(connectedRoutes.Length > 0
            ? $"Routes serving city ({connectedRoutes.Length}): {string.Join(", ", routeLines)}\n"
            : "Routes serving city: none\n");
        _inspector.AppendText(cityContracts.Length > 0
            ? $"Route Contract options: {cityContracts.Length}; best {ContractBrief(cityContracts[0])}\n"
            : "Route Contract options: none currently available\n");
        _inspector.AppendText(pricePressure.Length > 0
            ? $"Warehouse Policy: {string.Join(", ", pricePressure)}\n"
            : "Local market pressure: no tracked needs\n");
        _inspector.AppendText("Policy controls reserve warehouse stock before route contracts and exports use it.\n");

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
            .ToArray();
        var selectedContract = SelectedContract();
        var selectedRouteContract = selectedContract is not null && selectedContract.RouteId == route.Id
            ? selectedContract
            : null;

        _inspector.AppendText($"Route {route.Id}\n");
        _inspector.AppendText($"{from?.Name ?? route.FromNode} -> {to?.Name ?? route.ToNode} | {route.Mode}\n");
        _inspector.AppendText($"Capacity {route.CapacityPerDay}/day | lead time {route.LeadDays} {DayLabel(route.LeadDays)} | cost {route.CostPerUnit:0.00}/unit\n");
        _inspector.AppendText($"Company Ledger cashflow on this route: {FormatSignedMoney(lastCash)}\n");
        _inspector.AppendText($"Market Pressure at endpoints: {routeDemand}\n");
        if (selectedRouteContract is not null)
        {
            _inspector.AppendText($"Route Contract selected: {ContractBrief(selectedRouteContract)}\n");
        }
        else if (routeContracts.Length > 0)
        {
            _inspector.AppendText($"Route Contract best option: {ContractBrief(routeContracts[0])} ({routeContracts.Length} available)\n");
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
                : $"! {city.Name}: {ResourceLabel(pressure.ResourceId)} {pressure.MarketStock}/{pressure.ReorderPoint}, {pressure.PolicyAction}");
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

    private void UpdateWarehousePolicyControl()
    {
        if (_snapshot is null
            || _warehousePolicySummary is null
            || _warehouseResourceOptions is null
            || _warehouseSafetyInput is null
            || _warehouseReorderInput is null
            || _warehouseApplyButton is null)
        {
            return;
        }

        var previousResourceId = SelectedWarehousePolicySignal()?.ResourceId;
        var selectedCity = SelectedWarehousePolicyCity();
        _visibleWarehousePolicies = selectedCity?.MarketSignals
            .Where(signal => signal.DesiredStock > 0)
            .OrderByDescending(signal => signal.ShipmentPriority)
            .ThenByDescending(signal => signal.Scarcity)
            .ThenBy(signal => signal.ResourceId, StringComparer.Ordinal)
            .ToArray() ?? [];

        _refreshingWarehousePolicyControl = true;
        _warehouseResourceOptions.Clear();

        if (selectedCity is null)
        {
            _warehousePolicySummary.Text = "Select a city to control warehouse policy.";
            _warehouseResourceOptions.AddItem("No city selected");
            SetWarehousePolicyControlsEnabled(false);
            _refreshingWarehousePolicyControl = false;
            return;
        }

        if (_visibleWarehousePolicies.Count == 0)
        {
            _warehousePolicySummary.Text = $"{selectedCity.Name}: no tracked market needs.";
            _warehouseResourceOptions.AddItem("No tracked resources");
            SetWarehousePolicyControlsEnabled(false);
            _refreshingWarehousePolicyControl = false;
            return;
        }

        var selectedIndex = 0;
        for (var i = 0; i < _visibleWarehousePolicies.Count; i++)
        {
            var signal = _visibleWarehousePolicies[i];
            var source = signal.IsPolicyOverridden ? "manual" : "default";
            _warehouseResourceOptions.AddItem($"{ResourceLabel(signal.ResourceId)} | safety {signal.SafetyStock}, reorder {signal.ReorderPoint} ({source})", i);
            if (string.Equals(signal.ResourceId, previousResourceId, StringComparison.Ordinal))
            {
                selectedIndex = i;
            }
        }

        _warehouseResourceOptions.Select(selectedIndex);
        SetWarehousePolicyControlsEnabled(true);
        _refreshingWarehousePolicyControl = false;
        RefreshWarehousePolicyInputs();
    }

    private void OnWarehouseResourceSelected(long _)
    {
        if (_refreshingWarehousePolicyControl)
        {
            return;
        }

        RefreshWarehousePolicyInputs();
        UpdatePolicyPanel();
    }

    private void RefreshWarehousePolicyInputs()
    {
        if (_warehousePolicySummary is null || _warehouseSafetyInput is null || _warehouseReorderInput is null)
        {
            return;
        }

        var city = SelectedWarehousePolicyCity();
        var signal = SelectedWarehousePolicySignal();
        if (city is null || signal is null)
        {
            return;
        }

        _warehouseSafetyInput.Value = signal.SafetyStock;
        _warehouseReorderInput.Value = signal.ReorderPoint;
        var source = signal.IsPolicyOverridden ? "manual policy" : "default policy";
        var exportable = Math.Max(0, signal.WarehouseStock - signal.SafetyStock);
        _warehousePolicySummary.Text = $"{city.Name}: {ResourceLabel(signal.ResourceId)} uses {source}; reserved {signal.SafetyStock}, exportable {exportable}.";
    }

    private void ApplyWarehousePolicy()
    {
        if (_session is null
            || _snapshot is null
            || _warehouseSafetyInput is null
            || _warehouseReorderInput is null)
        {
            return;
        }

        var city = SelectedWarehousePolicyCity();
        var signal = SelectedWarehousePolicySignal();
        if (city is null || signal is null)
        {
            _warehousePolicyMessage = "Select a city and resource before applying warehouse policy.";
            UpdatePolicyPanel();
            return;
        }

        var previousHash = _snapshot.SaveHash;
        var safetyStock = (int)_warehouseSafetyInput.Value;
        var reorderPoint = (int)_warehouseReorderInput.Value;
        if (!_session.SetWarehousePolicy(city.Id, signal.ResourceId, safetyStock, reorderPoint))
        {
            _warehousePolicyMessage = $"Warehouse policy rejected for {city.Name} {ResourceLabel(signal.ResourceId)}.";
            UpdatePolicyPanel();
            return;
        }

        _snapshot = _session.Current;
        var updatedCity = _snapshot.Cities.FirstOrDefault(candidate => candidate.Id == city.Id);
        var updatedSignal = updatedCity?.MarketSignals.FirstOrDefault(candidate => candidate.ResourceId == signal.ResourceId);
        _warehousePolicyMessage = updatedSignal is null
            ? $"Applied warehouse policy to {city.Name}; save {ShortHash(previousHash)} -> {ShortHash(_snapshot.SaveHash)}."
            : $"Applied warehouse policy: {city.Name} {ResourceLabel(updatedSignal.ResourceId)} reserved {updatedSignal.SafetyStock}, reorder {updatedSignal.ReorderPoint}; save {ShortHash(previousHash)} -> {ShortHash(_snapshot.SaveHash)}.";

        KeepValidSelection();
        UpdatePrototypeView();
    }

    private void SetWarehousePolicyControlsEnabled(bool enabled)
    {
        if (_warehouseResourceOptions is not null)
        {
            _warehouseResourceOptions.Disabled = !enabled;
        }

        if (_warehouseSafetyInput is not null)
        {
            _warehouseSafetyInput.Editable = enabled;
        }

        if (_warehouseReorderInput is not null)
        {
            _warehouseReorderInput.Editable = enabled;
        }

        if (_warehouseApplyButton is not null)
        {
            _warehouseApplyButton.Disabled = !enabled;
        }
    }

    private PrototypeCityView? SelectedWarehousePolicyCity()
    {
        if (_snapshot is null || _selectedCityId is null)
        {
            return null;
        }

        return _snapshot.Cities.FirstOrDefault(city => city.Id == _selectedCityId);
    }

    private PrototypeMarketSignal? SelectedWarehousePolicySignal()
    {
        if (_warehouseResourceOptions is null || _visibleWarehousePolicies.Count == 0)
        {
            return null;
        }

        var index = Math.Clamp(_warehouseResourceOptions.Selected, 0, _visibleWarehousePolicies.Count - 1);
        return _visibleWarehousePolicies[index];
    }

    private void UpdatePolicyPanel()
    {
        if (_snapshot is null || _policy is null)
        {
            return;
        }

        _policy.Clear();

        var focusCity = _selectedCityId is not null
            ? _snapshot.Cities.FirstOrDefault(city => city.Id == _selectedCityId)
            : _snapshot.Cities
                .OrderByDescending(city => TopPressureSignal(city)?.ShipmentPriority ?? 0)
                .ThenBy(city => city.SupplySatisfaction)
                .FirstOrDefault();

        if (focusCity is null)
        {
            _policy.AppendText("Warehouse policy has no city state to inspect.\n");
            return;
        }

        var signals = focusCity.MarketSignals
            .Where(signal => signal.DesiredStock > 0)
            .OrderByDescending(signal => signal.ShipmentPriority)
            .ThenByDescending(signal => signal.Scarcity)
            .Take(3)
            .ToArray();

        _policy.AppendText($"Focus city: {focusCity.Name}\n");
        if (_warehousePolicyMessage is not null)
        {
            _policy.AppendText($"{_warehousePolicyMessage}\n");
        }

        if (signals.Length == 0)
        {
            _policy.AppendText("No tracked reorder needs in this city.\n");
            return;
        }

        foreach (var signal in signals)
        {
            var source = signal.IsPolicyOverridden ? "manual" : "default";
            var exportable = Math.Max(0, signal.WarehouseStock - signal.SafetyStock);
            _policy.AppendText($"{ResourceLabel(signal.ResourceId)}: market {signal.MarketStock}/{signal.DesiredStock}, warehouse {signal.WarehouseStock}, reserved {signal.SafetyStock}, exportable {exportable}, reorder {signal.ReorderPoint}, P{signal.ShipmentPriority}, {source}\n");
            _policy.AppendText($"Action: {signal.PolicyAction}\n");
        }
    }

    private void UpdateTestProbe()
    {
        if (_snapshot is null || _testProbe is null)
        {
            return;
        }

        _testProbe.Clear();
        var topCity = _snapshot.Cities
            .Select(city => new { City = city, Signal = TopPressureSignal(city) })
            .Where(item => item.Signal is not null)
            .OrderByDescending(item => item.Signal!.ShipmentPriority)
            .ThenByDescending(item => item.Signal!.Scarcity)
            .FirstOrDefault();
        var bestContract = _snapshot.AvailableContracts
            .OrderByDescending(contract => contract.ShipmentPriority)
            .ThenByDescending(contract => contract.ExpectedNet)
            .FirstOrDefault();
        var currentLedger = _snapshot.Ledger.Where(entry => entry.Tick == _snapshot.Tick).ToArray();

        _testProbe.AppendText($"Determinism: seed {Seed} | tick {_snapshot.Tick} | save {ShortHash(_snapshot.SaveHash)}\n");
        _testProbe.AppendText($"Company Ledger: cashflow {_snapshot.LastTickCashDelta:+0.00;-0.00;0.00} | events {currentLedger.Length} | AI move {_snapshot.AiChoice.OpportunityId}\n");
        _testProbe.AppendText(topCity?.Signal is null
            ? "Market Pressure: none\n"
            : $"Market Pressure: {topCity.City.Name} {ResourceLabel(topCity.Signal.ResourceId)} {topCity.Signal.MarketStock}/{topCity.Signal.ReorderPoint}, P{topCity.Signal.ShipmentPriority}, {topCity.Signal.PolicyAction}\n");
        _testProbe.AppendText(bestContract is null
            ? "Route Contract: none\n"
            : $"Route Contract: P{bestContract.ShipmentPriority} {ResourceLabel(bestContract.ResourceId)} x{bestContract.Units}, net {FormatSignedMoney(bestContract.ExpectedNet)}\n");
    }

    private static PanelContainer WrapPanel(Control child, bool horizontalExpand = true, float minimumWidth = 0.0f)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = horizontalExpand ? SizeFlags.ExpandFill : SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        if (minimumWidth > 0.0f)
        {
            panel.CustomMinimumSize = new Vector2(minimumWidth, 0);
        }

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.095f, 0.096f, 0.089f, 0.97f),
            BorderColor = new Color(0.55f, 0.43f, 0.22f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 16,
            ContentMarginTop = 14,
            ContentMarginRight = 16,
            ContentMarginBottom = 14
        };

        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(child);
        return panel;
    }

    private static LayoutProfile LayoutFor(Vector2 viewportSize)
    {
        if (viewportSize.X >= 1800 && viewportSize.Y >= 1000)
        {
            return new LayoutProfile(
                24,
                18,
                new Vector2(1050, 760),
                640,
                214,
                118,
                190,
                126,
                116);
        }

        if (viewportSize.X >= 1500)
        {
            return new LayoutProfile(
                20,
                16,
                new Vector2(860, 620),
                540,
                190,
                108,
                168,
                112,
                104);
        }

        return new LayoutProfile(
            16,
            12,
            new Vector2(720, 520),
            430,
            156,
            84,
            148,
            92,
            92);
    }

    private static VBoxContainer CreateStack()
    {
        return new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
    }

    private static VBoxContainer CreateSectionStack()
    {
        var stack = CreateStack();
        stack.AddThemeConstantOverride("separation", 8);
        return stack;
    }

    private static Button CreateButton(string text)
    {
        var button = new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 34)
        };
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.105f, 0.115f, 0.112f, 1.0f), new Color(0.22f, 0.24f, 0.23f, 1.0f)));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.145f, 0.165f, 0.155f, 1.0f), new Color(0.48f, 0.39f, 0.20f, 1.0f)));
        button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.04f, 0.045f, 0.043f, 1.0f), new Color(0.73f, 0.56f, 0.22f, 1.0f)));
        button.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.86f, 1.0f));
        button.AddThemeColorOverride("font_pressed_color", new Color(0.98f, 0.83f, 0.38f, 1.0f));
        return button;
    }

    private static SpinBox CreatePolicySpinBox(string name)
    {
        return new SpinBox
        {
            Name = name,
            MinValue = 0,
            MaxValue = 64,
            Step = 1,
            Rounded = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 30)
        };
    }

    private static StyleBoxFlat CreateButtonStyle(Color background, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 8,
            ContentMarginTop = 5,
            ContentMarginRight = 8,
            ContentMarginBottom = 5
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

    private static PanelContainer CreateSectionPanel(string title, Control content, bool verticalExpand = false, float minimumHeight = 0.0f)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        if (verticalExpand)
        {
            panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        }

        if (minimumHeight > 0.0f)
        {
            panel.CustomMinimumSize = new Vector2(0, minimumHeight);
        }

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.070f, 0.078f, 0.075f, 0.96f),
            BorderColor = new Color(0.36f, 0.28f, 0.14f, 0.95f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            ContentMarginLeft = 12,
            ContentMarginTop = 10,
            ContentMarginRight = 12,
            ContentMarginBottom = 10
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var stack = CreateSectionStack();
        if (verticalExpand)
        {
            stack.SizeFlagsVertical = SizeFlags.ExpandFill;
            content.SizeFlagsVertical = SizeFlags.ExpandFill;
        }

        var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var titleLabel = CreateSectionLabel(title);
        titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(titleLabel);
        var accent = new ColorRect
        {
            Color = new Color(0.76f, 0.58f, 0.24f, 0.78f),
            CustomMinimumSize = new Vector2(42, 2),
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        header.AddChild(accent);
        stack.AddChild(header);
        stack.AddChild(content);
        panel.AddChild(stack);
        return panel;
    }

    private static Label CreateSectionHint(string text)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", new Color(0.64f, 0.69f, 0.66f, 1.0f));
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
        label.AddThemeColorOverride("font_color", new Color(0.68f, 0.71f, 0.67f, 1.0f));
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
        var key = CreateMetricLabel(label, new Color(0.60f, 0.70f, 0.70f, 1.0f), HorizontalAlignment.Left);
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
        log.AddThemeFontSizeOverride("normal_font_size", 13);
        log.AddThemeColorOverride("default_color", new Color(0.88f, 0.89f, 0.82f, 1.0f));
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
        return $"{rank}: P{contract.ShipmentPriority} {ResourceLabel(contract.ResourceId)} x{contract.Units} | {CityName(contract.FromNode)} -> {CityName(contract.ToNode)} | {FormatSignedMoney(contract.ExpectedNet)} net";
    }

    private string ContractSummary(PrototypeRouteContractView contract)
    {
        return $"{ResourceLabel(contract.ResourceId)} x{contract.Units} from {CityName(contract.FromNode)} to {CityName(contract.ToNode)} on {RouteDisplayName(contract.RouteId)}; {contract.PolicyAction}, priority {contract.ShipmentPriority}, revenue {contract.ExpectedRevenue.ToString("0.00", CultureInfo.InvariantCulture)}, cost {contract.TransportCost.ToString("0.00", CultureInfo.InvariantCulture)}, net {FormatSignedMoney(contract.ExpectedNet)}, capacity {contract.CapacityPerDay}/day";
    }

    private string ContractBrief(PrototypeRouteContractView contract)
    {
        return $"P{contract.ShipmentPriority} {ResourceLabel(contract.ResourceId)} x{contract.Units} {CityName(contract.FromNode)} -> {CityName(contract.ToNode)} on {contract.RouteId} ({FormatSignedMoney(contract.ExpectedNet)} net)";
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
            .OrderByDescending(signal => signal.ShipmentPriority)
            .ThenByDescending(signal => signal.Scarcity)
            .ThenBy(signal => signal.ResourceId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private string CityPressureSummary(PrototypeCityView city)
    {
        var pressure = TopPressureSignal(city);
        return pressure is null
            ? $"stable demand, warehouse {StockUnits(city.CompanyWarehouse)}"
            : $"{ResourceLabel(pressure.ResourceId)} {pressure.MarketStock}/{pressure.ReorderPoint}, {pressure.PolicyAction}, unmet {SupplyPressure(city):0.00}";
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
    private IReadOnlyDictionary<(int X, int Y), TerrainCell> _terrainByPoint = new Dictionary<(int X, int Y), TerrainCell>();
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
        if (_snapshot?.World.Hash != snapshot.World.Hash)
        {
            _terrainByPoint = snapshot.World.Terrain.ToDictionary(terrain => (terrain.X, terrain.Y));
        }

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
        DrawModeBanner(_snapshot);
        DrawRoutes(_snapshot);
        DrawCities(_snapshot);
        DrawLegend(_snapshot);
        DrawHoverLabel(_snapshot);
    }

    private void DrawTerrain(PrototypeSnapshot snapshot)
    {
        var cell = MapScale(snapshot);
        var origin = MapOrigin(snapshot);
        var bounds = new Rect2(origin, new Vector2(snapshot.World.Width * cell, snapshot.World.Height * cell));
        DrawRect(new Rect2(bounds.Position - new Vector2(10, 10), bounds.Size + new Vector2(20, 20)), new Color(0.055f, 0.063f, 0.066f, 1.0f));
        DrawRect(bounds, new Color(0.13f, 0.26f, 0.31f, 1.0f));

        foreach (var terrain in snapshot.World.Terrain)
        {
            var point = CellTopLeftFor(snapshot, terrain.X, terrain.Y);
            var color = TerrainColor(terrain, _mapMode);
            DrawRect(new Rect2(point, new Vector2(cell + 0.8f, cell + 0.8f)), color);
            DrawTerrainDetail(terrain, point, cell);
        }

        DrawCoastlines(snapshot, cell);
        DrawRegionGrid(snapshot, cell);
    }

    private void DrawCoastlines(PrototypeSnapshot snapshot, float cell)
    {
        var shore = new Color(0.76f, 0.70f, 0.48f, _mapMode == PrototypeMapMode.Demand ? 0.40f : 0.62f);
        foreach (var terrain in snapshot.World.Terrain.Where(terrain => !terrain.IsWater))
        {
            var point = CellTopLeftFor(snapshot, terrain.X, terrain.Y);
            if (IsWater(_terrainByPoint, terrain.X, terrain.Y - 1))
            {
                DrawLine(point, point + new Vector2(cell, 0), shore, 1.6f, true);
            }

            if (IsWater(_terrainByPoint, terrain.X, terrain.Y + 1))
            {
                DrawLine(point + new Vector2(0, cell), point + new Vector2(cell, cell), shore, 1.6f, true);
            }

            if (IsWater(_terrainByPoint, terrain.X - 1, terrain.Y))
            {
                DrawLine(point, point + new Vector2(0, cell), shore, 1.6f, true);
            }

            if (IsWater(_terrainByPoint, terrain.X + 1, terrain.Y))
            {
                DrawLine(point + new Vector2(cell, 0), point + new Vector2(cell, cell), shore, 1.6f, true);
            }
        }
    }

    private void DrawRegionGrid(PrototypeSnapshot snapshot, float cell)
    {
        if (cell < 17.0f)
        {
            return;
        }

        var origin = MapOrigin(snapshot);
        var width = snapshot.World.Width * cell;
        var height = snapshot.World.Height * cell;
        var grid = new Color(0.04f, 0.050f, 0.050f, 0.08f);

        for (var x = 4; x < snapshot.World.Width; x += 4)
        {
            var px = origin.X + x * cell;
            DrawLine(new Vector2(px, origin.Y), new Vector2(px, origin.Y + height), grid, 1.0f, true);
        }

        for (var y = 4; y < snapshot.World.Height; y += 4)
        {
            var py = origin.Y + y * cell;
            DrawLine(new Vector2(origin.X, py), new Vector2(origin.X + width, py), grid, 1.0f, true);
        }
    }

    private static bool IsWater(IReadOnlyDictionary<(int X, int Y), TerrainCell> terrainByPoint, int x, int y)
    {
        return !terrainByPoint.TryGetValue((x, y), out var terrain) || terrain.IsWater;
    }

    private static Color TerrainColor(TerrainCell terrain, PrototypeMapMode mapMode)
    {
        var fade = mapMode == PrototypeMapMode.Demand ? 0.72f : 1.0f;
        if (terrain.IsWater)
        {
            var depth = (float)Math.Clamp((0.36 - terrain.Height) * 1.8, 0.0, 0.35);
            return new Color(0.075f - depth * 0.04f, 0.210f - depth * 0.06f, 0.270f - depth * 0.05f, 1.0f);
        }

        if (terrain.Height > 0.68)
        {
            return new Color(0.49f * fade, 0.47f * fade, 0.35f * fade, 1.0f);
        }

        if (terrain.Moisture < 0.32)
        {
            return new Color(0.61f * fade, 0.56f * fade, 0.35f * fade, 1.0f);
        }

        if (terrain.Fertility > 0.58)
        {
            return new Color(0.39f * fade, 0.57f * fade, 0.34f * fade, 1.0f);
        }

        return new Color(
            (0.45f + (float)terrain.Fertility * 0.12f) * fade,
            (0.50f + (float)terrain.Moisture * 0.11f) * fade,
            (0.34f + (float)terrain.Fertility * 0.06f) * fade,
            1.0f);
    }

    private void DrawTerrainDetail(TerrainCell terrain, Vector2 point, float cell)
    {
        if (cell < 18.0f)
        {
            return;
        }

        var hash = terrain.X * 73 + terrain.Y * 151;
        if (terrain.IsWater)
        {
            if ((hash & 3) == 0)
            {
                DrawLine(point + new Vector2(cell * 0.12f, cell * 0.68f), point + new Vector2(cell * 0.82f, cell * 0.58f), new Color(0.40f, 0.65f, 0.70f, 0.10f), 1.0f, true);
            }

            return;
        }

        var alpha = 0.06f + (hash & 3) * 0.018f;
        if (terrain.Height > 0.68)
        {
            var ridge = new Color(0.18f, 0.17f, 0.13f, alpha + 0.06f);
            DrawLine(point + new Vector2(cell * 0.18f, cell * 0.70f), point + new Vector2(cell * 0.48f, cell * 0.24f), ridge, 1.2f, true);
            DrawLine(point + new Vector2(cell * 0.48f, cell * 0.24f), point + new Vector2(cell * 0.78f, cell * 0.68f), ridge, 1.2f, true);
            return;
        }

        if (terrain.Fertility > 0.58 && (hash & 1) == 0)
        {
            var grove = new Color(0.12f, 0.23f, 0.12f, alpha + 0.05f);
            DrawCircle(point + new Vector2(cell * 0.35f, cell * 0.42f), Math.Max(1.8f, cell * 0.055f), grove);
            DrawCircle(point + new Vector2(cell * 0.52f, cell * 0.34f), Math.Max(1.4f, cell * 0.045f), grove);
            DrawCircle(point + new Vector2(cell * 0.60f, cell * 0.52f), Math.Max(1.4f, cell * 0.045f), grove);
            return;
        }

        DrawCircle(point + new Vector2(cell * 0.72f, cell * 0.30f), Math.Max(1.3f, cell * 0.040f), new Color(0.78f, 0.72f, 0.48f, alpha));
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

            DrawLine(start, end, new Color(0.025f, 0.028f, 0.024f, alpha * 0.72f), width + 3.4f, true);
            DrawLine(start, end, color, width, true);
            DrawRouteArrow(start, end, color, width, selected || hovered || related);
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
        var labels = new List<(PrototypeCityView City, Vector2 Point, string Kind, double Pressure, bool Selected, bool Hovered, bool Related)>();
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

            labels.Add((city, point, kind, pressure, selected, hovered, related));
        }

        var occupiedLabels = new List<Rect2>
        {
            new(new Vector2(12, 12), new Vector2(Size.X - 24, 58)),
            new(new Vector2(14, 78), new Vector2(150, 330)),
            new(new Vector2(14, Size.Y - 190), new Vector2(210, 174))
        };

        foreach (var cityLabel in labels.OrderByDescending(label => label.Selected || label.Hovered || label.Related).ThenBy(label => label.City.Id, StringComparer.Ordinal))
        {
            var city = cityLabel.City;
            var pressure = cityLabel.Pressure;
            var selected = cityLabel.Selected;
            var hovered = cityLabel.Hovered;
            var related = cityLabel.Related;
            if (selected || hovered || related || _mapMode == PrototypeMapMode.Routes || (_mapMode == PrototypeMapMode.Demand && pressure > 0.25))
            {
                var text = selected || hovered || related || _mapMode == PrototypeMapMode.Demand
                    ? CityMapLabel(city, pressure)
                    : city.Name;
                DrawPlacedMapLabel(cityLabel.Point, text, occupiedLabels, city.X < snapshot.World.Width / 2, CityKindColor(cityLabel.Kind, city.SupplySatisfaction));
            }
        }
    }

    private void DrawModeBanner(PrototypeSnapshot snapshot)
    {
        var modeText = _mapMode switch
        {
            PrototypeMapMode.Routes => "Capacity routes",
            PrototypeMapMode.Profit => "Cashflow routes",
            _ => "Demand pressure"
        };
        var panel = new Rect2(new Vector2(14, 14), new Vector2(Size.X - 28, 58));
        DrawRect(panel, new Color(0.038f, 0.043f, 0.041f, 0.93f));
        DrawRect(new Rect2(panel.Position, new Vector2(panel.Size.X, 1)), new Color(0.74f, 0.58f, 0.25f, 0.68f));
        DrawRect(new Rect2(panel.Position + new Vector2(0, panel.Size.Y - 1), new Vector2(panel.Size.X, 1)), new Color(0.18f, 0.15f, 0.09f, 0.72f));

        var compassCenter = panel.Position + new Vector2(28, 29);
        DrawCircle(compassCenter, 14.0f, new Color(0.10f, 0.085f, 0.050f, 0.88f));
        DrawLine(compassCenter + new Vector2(0, -10), compassCenter + new Vector2(0, 10), new Color(0.86f, 0.70f, 0.34f, 0.90f), 1.4f, true);
        DrawLine(compassCenter + new Vector2(-10, 0), compassCenter + new Vector2(10, 0), new Color(0.86f, 0.70f, 0.34f, 0.90f), 1.4f, true);
        DrawString(_font, compassCenter + new Vector2(-4, 5), "N", HorizontalAlignment.Left, 16, 10, new Color(0.94f, 0.84f, 0.55f, 1.0f));

        DrawString(_font, panel.Position + new Vector2(52, 25), "Charters of Trade", HorizontalAlignment.Left, 260, 20, new Color(0.95f, 0.84f, 0.56f, 1.0f));
        DrawString(_font, panel.Position + new Vector2(52, 46), "Prototype Systems Loop", HorizontalAlignment.Left, 260, 11, new Color(0.66f, 0.72f, 0.69f, 1.0f));
        DrawHudPill(panel.Position + new Vector2(326, 17), modeText, 142);
        DrawHudPill(panel.Position + new Vector2(476, 17), $"Seed {snapshot.World.Seed}", 124);
        DrawHudPill(panel.Position + new Vector2(608, 17), $"{snapshot.World.WorldGenVersion}", 112);
    }

    private void DrawLegend(PrototypeSnapshot snapshot)
    {
        var origin = new Vector2(18, 92);
        var panel = new Rect2(origin - new Vector2(10, 14), new Vector2(150, 306));
        DrawRect(panel, new Color(0.050f, 0.055f, 0.051f, 0.86f));
        DrawRect(new Rect2(panel.Position, new Vector2(1, panel.Size.Y)), new Color(0.65f, 0.50f, 0.21f, 0.62f));
        DrawString(_font, origin, "Map Mode", HorizontalAlignment.Left, 118, 13, new Color(0.91f, 0.82f, 0.61f, 1.0f));
        DrawModeRailRow(origin + new Vector2(0, 24), "Routes", PrototypeMapMode.Routes);
        DrawModeRailRow(origin + new Vector2(0, 52), "Profit", PrototypeMapMode.Profit);
        DrawModeRailRow(origin + new Vector2(0, 80), "Demand", PrototypeMapMode.Demand);

        var guide = origin + new Vector2(0, 126);
        DrawString(_font, guide, "Signals", HorizontalAlignment.Left, 118, 13, new Color(0.91f, 0.82f, 0.61f, 1.0f));

        if (_mapMode == PrototypeMapMode.Routes)
        {
            DrawLine(guide + new Vector2(0, 24), guide + new Vector2(42, 24), new Color(0.22f, 0.45f, 0.63f, 1.0f), 5.0f, true);
            DrawString(_font, guide + new Vector2(50, 29), "coast cap", HorizontalAlignment.Left, 82, 12);
            DrawLine(guide + new Vector2(0, 50), guide + new Vector2(42, 50), new Color(0.56f, 0.38f, 0.18f, 1.0f), 3.0f, true);
            DrawString(_font, guide + new Vector2(50, 55), "road cap", HorizontalAlignment.Left, 82, 12);
        }
        else if (_mapMode == PrototypeMapMode.Profit)
        {
            DrawLine(guide + new Vector2(0, 24), guide + new Vector2(42, 24), new Color(0.21f, 0.55f, 0.42f, 1.0f), 4.0f, true);
            DrawString(_font, guide + new Vector2(50, 29), "profit", HorizontalAlignment.Left, 82, 12);
            DrawLine(guide + new Vector2(0, 50), guide + new Vector2(42, 50), new Color(0.62f, 0.17f, 0.14f, 1.0f), 4.0f, true);
            DrawString(_font, guide + new Vector2(50, 55), "loss", HorizontalAlignment.Left, 82, 12);
        }
        else
        {
            DrawCircle(guide + new Vector2(16, 24), 7.0f, new Color(0.62f, 0.17f, 0.14f, 1.0f));
            DrawArc(guide + new Vector2(16, 24), 14.0f, 0.0f, Mathf.Tau * 0.58f, 24, new Color(0.62f, 0.17f, 0.14f, 0.78f), 2.0f, true);
            DrawString(_font, guide + new Vector2(50, 29), "shortage", HorizontalAlignment.Left, 82, 12);
            DrawLine(guide + new Vector2(0, 50), guide + new Vector2(42, 50), new Color(0.71f, 0.48f, 0.14f, 1.0f), 4.0f, true);
            DrawString(_font, guide + new Vector2(50, 55), "pressure", HorizontalAlignment.Left, 82, 12);
        }

        var cityLegend = origin + new Vector2(0, 214);
        DrawCityStamp(cityLegend + new Vector2(12, 0), "charter_town", 7.0f, CityKindColor("charter_town", 1.0));
        DrawString(_font, cityLegend + new Vector2(32, 5), "charter", HorizontalAlignment.Left, 82, 12);
        DrawCityStamp(cityLegend + new Vector2(12, 28), "port", 6.0f, CityKindColor("port", 1.0));
        DrawString(_font, cityLegend + new Vector2(32, 33), "port", HorizontalAlignment.Left, 82, 12);
        DrawCityStamp(cityLegend + new Vector2(12, 56), "market_town", 6.0f, CityKindColor("market_town", 1.0));
        DrawString(_font, cityLegend + new Vector2(32, 61), "market", HorizontalAlignment.Left, 82, 12);

        if (_selectedCityId is null && _selectedRouteId is null)
        {
            DrawMapLabel(new Vector2(178, 92), "Select a city or route for linked inspector details.", minWidth: 292);
        }
    }

    private void DrawHoverLabel(PrototypeSnapshot snapshot)
    {
        if (_hoveredCityId is not null)
        {
            var city = snapshot.Cities.FirstOrDefault(city => city.Id == _hoveredCityId);
            if (city is not null)
            {
                var pressure = SupplyPressure(city);
                if (_mapMode == PrototypeMapMode.Routes || _mapMode == PrototypeMapMode.Demand || city.Id == _selectedCityId || pressure > 0.25)
                {
                    return;
                }

                DrawMapLabel(PointFor(snapshot, city.X, city.Y) + new Vector2(12, -10), CityMapLabel(city, SupplyPressure(city)));
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

    private void DrawRouteArrow(Vector2 start, Vector2 end, Color color, float width, bool emphasized)
    {
        var direction = end - start;
        var length = direction.Length();
        if (length <= 10.0f)
        {
            return;
        }

        var normal = direction / length;
        var perpendicular = new Vector2(-normal.Y, normal.X);
        var center = start + direction * 0.58f;
        var size = emphasized ? 8.5f : 6.5f;
        var points = new[]
        {
            center + normal * size,
            center - normal * size * 0.74f + perpendicular * size * 0.54f,
            center - normal * size * 0.74f - perpendicular * size * 0.54f
        };
        var arrow = color.Lightened(emphasized ? 0.24f : 0.14f);
        arrow.A = Math.Min(1.0f, color.A + 0.07f);
        DrawColoredPolygon(points, arrow);
        DrawPolyline(new[] { points[0], points[1], points[2], points[0] }, new Color(0.030f, 0.026f, 0.018f, 0.70f), Math.Max(1.0f, width * 0.22f), true);
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

    private static string CityMapLabel(PrototypeCityView city, double pressure)
    {
        return pressure > 0.18
            ? $"{city.Name} | need {pressure:0.00}"
            : $"{city.Name} | supply {city.SupplySatisfaction:0.00}";
    }

    private Rect2 DrawMapLabel(Vector2 position, string text, float minWidth = 72.0f, Color? accent = null)
    {
        var clamped = ClampMapLabelPosition(position, text, minWidth);
        var rect = MapLabelRect(clamped, text, minWidth);
        DrawRect(new Rect2(rect.Position + new Vector2(2, 2), rect.Size), new Color(0.0f, 0.0f, 0.0f, 0.30f));
        DrawRect(rect, new Color(0.042f, 0.046f, 0.041f, 0.92f));
        DrawRect(new Rect2(rect.Position, new Vector2(3, rect.Size.Y)), accent ?? new Color(0.62f, 0.50f, 0.28f, 0.72f));
        DrawRect(new Rect2(rect.Position + new Vector2(0, rect.Size.Y - 1), new Vector2(rect.Size.X, 1)), new Color(0.72f, 0.56f, 0.25f, 0.38f));
        DrawString(_font, clamped, text, HorizontalAlignment.Left, rect.Size.X - 10, 12, new Color(0.94f, 0.95f, 0.89f, 1.0f));
        return rect;
    }

    private void DrawPlacedMapLabel(Vector2 anchor, string text, List<Rect2> occupiedLabels, bool preferRight, Color accent)
    {
        var size = MeasureMapLabel(text);
        var right = new[]
        {
            new Vector2(18, -10),
            new Vector2(18, 20),
            new Vector2(-size.X - 14, -10),
            new Vector2(-size.X - 14, 20),
            new Vector2(-size.X / 2.0f, -34)
        };
        var left = new[]
        {
            new Vector2(-size.X - 14, -10),
            new Vector2(-size.X - 14, 20),
            new Vector2(18, -10),
            new Vector2(18, 20),
            new Vector2(-size.X / 2.0f, -34)
        };

        foreach (var offset in preferRight ? right : left)
        {
            var position = ClampMapLabelPosition(anchor + offset, text, size.X);
            var rect = MapLabelRect(position, text, size.X);
            if (IntersectsAny(rect, occupiedLabels))
            {
                continue;
            }

            occupiedLabels.Add(rect.Grow(4.0f));
            DrawMapLabel(position, text, size.X, accent);
            return;
        }

        var fallback = ClampMapLabelPosition(anchor + new Vector2(16, -10), text, size.X);
        var fallbackRect = DrawMapLabel(fallback, text, size.X, accent);
        occupiedLabels.Add(fallbackRect.Grow(4.0f));
    }

    private Vector2 ClampMapLabelPosition(Vector2 position, string text, float minWidth)
    {
        var rect = MapLabelRect(position, text, minWidth);
        var x = Math.Clamp(position.X, 8.0f - (rect.Position.X - position.X), Size.X - rect.Size.X - 8.0f - (rect.Position.X - position.X));
        var y = Math.Clamp(position.Y, 24.0f, Size.Y - 8.0f);
        return new Vector2(x, y);
    }

    private static Rect2 MapLabelRect(Vector2 position, string text, float minWidth)
    {
        return new Rect2(position + new Vector2(-6, -18), MeasureMapLabel(text, minWidth));
    }

    private static Vector2 MeasureMapLabel(string text, float minWidth = 72.0f)
    {
        return new Vector2(Math.Max(minWidth, text.Length * 6.7f + 12.0f), 24.0f);
    }

    private static bool IntersectsAny(Rect2 rect, IEnumerable<Rect2> occupiedLabels)
    {
        return occupiedLabels.Any(occupied => occupied.Intersects(rect));
    }

    private void DrawHudPill(Vector2 position, string text, float width)
    {
        var rect = new Rect2(position, new Vector2(width, 26));
        DrawRect(rect, new Color(0.075f, 0.083f, 0.078f, 0.94f));
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, 1)), new Color(0.63f, 0.50f, 0.25f, 0.42f));
        DrawString(_font, position + new Vector2(10, 18), text, HorizontalAlignment.Left, width - 18, 12, new Color(0.90f, 0.91f, 0.84f, 1.0f));
    }

    private void DrawModeRailRow(Vector2 position, string text, PrototypeMapMode mode)
    {
        var active = _mapMode == mode;
        var rect = new Rect2(position, new Vector2(118, 22));
        DrawRect(rect, active ? new Color(0.13f, 0.105f, 0.055f, 0.92f) : new Color(0.070f, 0.076f, 0.070f, 0.72f));
        if (active)
        {
            DrawRect(new Rect2(rect.Position, new Vector2(3, rect.Size.Y)), new Color(0.82f, 0.61f, 0.24f, 0.95f));
        }

        DrawString(_font, position + new Vector2(10, 16), text, HorizontalAlignment.Left, 94, 12, active ? new Color(0.98f, 0.86f, 0.54f, 1.0f) : new Color(0.72f, 0.75f, 0.70f, 1.0f));
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
        return MapOrigin(snapshot) + new Vector2(x * cell + cell / 2.0f, y * cell + cell / 2.0f);
    }

    private Vector2 CellTopLeftFor(PrototypeSnapshot snapshot, int x, int y)
    {
        var cell = MapScale(snapshot);
        return MapOrigin(snapshot) + new Vector2(x * cell, y * cell);
    }

    private Vector2 MapOrigin(PrototypeSnapshot snapshot)
    {
        var cell = MapScale(snapshot);
        var width = snapshot.World.Width * cell;
        var height = snapshot.World.Height * cell;
        return new Vector2((Size.X - width) / 2.0f, (Size.Y - height) / 2.0f);
    }
}
