namespace ChartersOfTrade.GodotBridge;

public sealed record FirstCharterSeasonScriptedRunResult(
    int TicksRun,
    string EndReason,
    int ScenarioScore,
    int? WinTick,
    int CompletedCharters,
    int DistinctResources,
    int StableNeeds,
    decimal FinalCash,
    int ProductionFocusChanges,
    int RouteSelections);

public static class FirstCharterSeasonScriptedStrategy
{
    private const int MaxActiveRouteOperations = 3;

    public static FirstCharterSeasonScriptedRunResult Run(PrototypeSession session, int maxTicks = FirstCharterSeason.TickLimit)
    {
        ArgumentNullException.ThrowIfNull(session);

        var creditedResources = new HashSet<string>(StringComparer.Ordinal);
        var focusChanges = 0;
        var routeSelections = 0;
        var ticksRun = 0;

        for (var tick = 0; tick < Math.Max(0, maxTicks) && !session.Current.ScenarioObjective.IsComplete; tick++)
        {
            var activations = EnsureRouteNetwork(session, creditedResources);
            foreach (var activation in activations)
            {
                routeSelections += activation.ChangedSelection ? 1 : 0;
                focusChanges += TryFocusProductionFor(session, activation.Contract.FromNode, activation.Contract.ResourceId) ? 1 : 0;
            }

            var activeSelection = SelectBestActiveOperation(session, creditedResources);
            routeSelections += activeSelection.ChangedSelection ? 1 : 0;
            if (activeSelection.Operation is not null)
            {
                focusChanges += TryFocusProductionFor(session, activeSelection.Operation.FromNode, activeSelection.Operation.ResourceId) ? 1 : 0;
            }

            var beforeObjective = session.Current.ScenarioObjective;
            var selectedBeforeTick = session.Current.ActiveRouteOperation;
            var next = session.AdvanceTick();
            ticksRun++;

            if (selectedBeforeTick is not null && next.ScenarioObjective.CompletedCharters > beforeObjective.CompletedCharters)
            {
                creditedResources.Add(selectedBeforeTick.ResourceId);
            }
        }

        var objective = session.Current.ScenarioObjective;
        return new FirstCharterSeasonScriptedRunResult(
            ticksRun,
            objective.EndReason,
            objective.FinalScore,
            objective.IsWon ? objective.CurrentTick : null,
            objective.CompletedCharters,
            objective.DistinctResources,
            objective.StableNeeds,
            session.Current.Company.Cash,
            focusChanges,
            routeSelections);
    }

    private static IReadOnlyList<SelectedContract> EnsureRouteNetwork(PrototypeSession session, HashSet<string> creditedResources)
    {
        var activated = new List<SelectedContract>();
        while (session.Current.ActiveRouteOperations.Count < MaxActiveRouteOperations)
        {
            var selection = SelectNextContract(session, creditedResources);
            if (selection is null)
            {
                break;
            }

            activated.Add(selection);
        }

        return activated;
    }

    private static ActiveOperationSelection SelectBestActiveOperation(PrototypeSession session, HashSet<string> creditedResources)
    {
        var snapshot = session.Current;
        var activeOperations = snapshot.ActiveRouteOperations;
        if (activeOperations.Count == 0)
        {
            return new ActiveOperationSelection(null, ChangedSelection: false);
        }

        var needsDistinctResource = snapshot.ScenarioObjective.DistinctResources < FirstCharterSeason.RequiredDistinctResources;
        var incoming = activeOperations
            .Select(operation => new
            {
                Operation = operation,
                RemainingTicks = snapshot.RouteTransits
                    .Where(transit => string.Equals(transit.OperationId, operation.Id, StringComparison.Ordinal))
                    .Select(transit => transit.RemainingTicks)
                    .DefaultIfEmpty(int.MaxValue)
                    .Min()
            })
            .Where(item => item.RemainingTicks <= 1)
            .OrderByDescending(item => needsDistinctResource && !creditedResources.Contains(item.Operation.ResourceId))
            .ThenBy(item => item.RemainingTicks)
            .ThenByDescending(item => item.Operation.ShipmentPriority)
            .ThenByDescending(item => item.Operation.ExpectedNet)
            .ThenBy(item => item.Operation.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (incoming is not null)
        {
            return SelectActiveOperation(session, snapshot, incoming.Operation);
        }

        var chosen = activeOperations
            .Where(operation => operation.CanDispatch
                || snapshot.RouteTransits.Any(transit => string.Equals(transit.OperationId, operation.Id, StringComparison.Ordinal)))
            .OrderByDescending(operation => needsDistinctResource && !creditedResources.Contains(operation.ResourceId))
            .ThenByDescending(operation => operation.CanDispatch)
            .ThenByDescending(operation => operation.ShipmentPriority)
            .ThenByDescending(operation => operation.ExpectedNet)
            .ThenBy(operation => operation.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        return chosen is null
            ? new ActiveOperationSelection(null, ChangedSelection: false)
            : SelectActiveOperation(session, snapshot, chosen);
    }

    private static ActiveOperationSelection SelectActiveOperation(
        PrototypeSession session,
        PrototypeSnapshot snapshot,
        PrototypeRouteOperationView operation)
    {
        var changedSelection = !string.Equals(snapshot.SelectedContractId, operation.SourceContractId, StringComparison.Ordinal);
        return session.SelectActiveRouteOperation(operation.Id)
            ? new ActiveOperationSelection(operation, changedSelection)
            : new ActiveOperationSelection(null, ChangedSelection: false);
    }

    private static SelectedContract? SelectNextContract(PrototypeSession session, HashSet<string> creditedResources)
    {
        var snapshot = session.Current;
        var needsDistinctResource = snapshot.ScenarioObjective.DistinctResources < FirstCharterSeason.RequiredDistinctResources;
        var activeContractIds = snapshot.ActiveRouteOperations
            .Select(operation => operation.SourceContractId)
            .ToHashSet(StringComparer.Ordinal);
        var candidates = snapshot.AvailableContracts
            .Where(contract => !activeContractIds.Contains(contract.Id))
            .Where(contract => contract.ExpectedNet >= 0m)
            .DefaultIfEmpty()
            .ToArray();
        if (candidates.Length == 1 && candidates[0] is null)
        {
            candidates = snapshot.AvailableContracts
                .Where(contract => !activeContractIds.Contains(contract.Id))
                .ToArray();
        }

        var contract = candidates
            .Where(contract => contract is not null)
            .Select(contract => contract!)
            .OrderByDescending(contract => needsDistinctResource && !creditedResources.Contains(contract.ResourceId))
            .ThenByDescending(contract => contract.ShipmentPriority)
            .ThenByDescending(contract => contract.ExpectedNet)
            .ThenByDescending(contract => contract.Units)
            .ThenBy(contract => contract.Id, StringComparer.Ordinal)
            .FirstOrDefault();

        if (contract is null)
        {
            return null;
        }

        var changedSelection = !string.Equals(snapshot.SelectedContractId, contract.Id, StringComparison.Ordinal);
        if (!session.SelectRouteContract(contract.Id))
        {
            return null;
        }

        session.SetRoutePriorityResource(contract.RouteId, contract.ResourceId);
        return new SelectedContract(contract, changedSelection);
    }

    private static bool TryFocusProductionFor(PrototypeSession session, string cityId, string resourceId)
    {
        var chain = session.Current.ProductionChainOpportunities
            .Where(opportunity => string.Equals(opportunity.CityId, cityId, StringComparison.Ordinal)
                && opportunity.Outputs.Any(output => string.Equals(output.ResourceId, resourceId, StringComparison.Ordinal)))
            .OrderByDescending(opportunity => opportunity.IsReady)
            .ThenBy(opportunity => opportunity.MissingInputUnits)
            .ThenByDescending(opportunity => opportunity.ExpectedMargin)
            .ThenBy(opportunity => opportunity.RecipeId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (chain is null)
        {
            return false;
        }

        var currentPolicy = session.Current.ProductionPolicies.FirstOrDefault(policy => string.Equals(policy.CityId, cityId, StringComparison.Ordinal));
        if (currentPolicy is not null
            && string.Equals(currentPolicy.Mode, PrototypeSession.FocusProductionMode, StringComparison.Ordinal)
            && string.Equals(currentPolicy.FocusRecipeId, chain.RecipeId, StringComparison.Ordinal))
        {
            return false;
        }

        return session.SetProductionFocus(cityId, chain.RecipeId);
    }

    private sealed record SelectedContract(PrototypeRouteContractView Contract, bool ChangedSelection);

    private sealed record ActiveOperationSelection(PrototypeRouteOperationView? Operation, bool ChangedSelection);
}
