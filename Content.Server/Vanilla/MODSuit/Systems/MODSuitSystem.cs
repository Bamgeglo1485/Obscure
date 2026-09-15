using Content.Shared.Containers.ItemSlots;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Vanilla.MODSuit;
using Content.Shared.Atmos.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Actions.Events;
using Content.Shared.Vanilla.MODSuit.Modules;

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

namespace Content.Server.Vanilla.MODSuit;

public sealed partial class MODSuitSystem : SharedMODSuitSystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    [SubscribeLocalEvent]
    private void OnActionAttempt(Entity<ActionRequireMODFuelComponent> ent, ref ActionAttemptEvent args)
    {
        if (!GetMODSuit(args.User, out var modSuit))
        {
            args.Cancelled = true;
            return;
        }

        if (!TryComp<MODSuitFuelComponent>(modSuit, out var fuel))
        {
            args.Cancelled = true;
            return;
        }

        if (TryUseFuel((modSuit, fuel), ent.Comp.Quantity) != 0f)
            return;

        args.Reason = "Недостаточно топлива в МОДе";
        args.Cancelled = true;
    }

    /// <summary>
    /// Возвращает мензурку со слота МОДсьюта
    /// </summary>
    public bool TryGetBeakerFromSlot(Entity<MODSuitFuelComponent> ent, [NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.FuelSlotId, out var slot))
        {
            solution = null;
            return false;
        }

        if (!TryComp<SolutionComponent>(slot.Item, out var solutionComp))
        {
            solution = null;
            return false;
        }

        solution = (slot.Item.Value, solutionComp);
        return true;
    }

    /// <summary>
    /// Возвращает мензурку со слота МОДсьюта
    /// </summary>
    public bool TryGetTankFromSlot(Entity<MODSuitFuelComponent> ent, [NotNullWhen(true)] out Entity<GasTankComponent>? tank)
    {
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.FuelSlotId, out var slot))
        {
            tank = null;
            return false;
        }

        if (!TryComp<GasTankComponent>(slot.Item, out var tankComp))
        {
            tank = null;
            return false;
        }

        tank = (slot.Item.Value, tankComp);
        return true;
    }

    // Использует топлива из хранилищ и возвращает коэффициент горения
    public float TryUseFuel(Entity<MODSuitFuelComponent> ent, float quantity = 1.0f)
    {
        return TryUseLiquidFuel(ent, quantity * 5f);
    }

    // Использует жидкости из хранилища и возвращает коэффициент горения
    public float TryUseLiquidFuel(Entity<MODSuitFuelComponent> ent, float unitsToUse = 5.0f)
    {
        if (!TryGetBeakerFromSlot(ent, out var solutionComp))
            return 0f;

        var solution = solutionComp.Value.Comp.Solution;

        if (solution.Volume <= 0)
            return 0f;

        var units = FixedPoint2.New(unitsToUse);
        var totalVolume = FixedPoint2.Zero;

        var flammableReagents = new Dictionary<ReagentPrototype, FixedPoint2>();

        foreach (var reagent in solution.Contents)
        {
            var proto = ProtoMan.Index<ReagentPrototype>(reagent.Reagent.Prototype);

            if (proto.Flammability > 0)
            {
                flammableReagents[proto] = reagent.Quantity;
                totalVolume += reagent.Quantity;
            }
        }

        if (flammableReagents.Count == 0 || totalVolume == 0)
            return 0f;

        var availableVolume = FixedPoint2.Min(units, solution.Volume);
        var useFraction = (float)(availableVolume / solution.Volume);

        if (availableVolume < units)
            units = availableVolume;

        var weightedFlammability = 0f;
        var usedVolume = FixedPoint2.Zero;

        foreach (var (proto, quantity) in flammableReagents)
        {
            var proportion = (float)(quantity / totalVolume);
            var amountToUse = FixedPoint2.Min(quantity, units * proportion);

            solution.RemoveReagent(proto.ID, amountToUse);

            weightedFlammability += proto.Flammability * proportion;
            usedVolume += amountToUse;
        }

        _solutionContainerSystem.UpdateChemicals(solutionComp.Value);

        return weightedFlammability * (float)usedVolume;
    }
}
