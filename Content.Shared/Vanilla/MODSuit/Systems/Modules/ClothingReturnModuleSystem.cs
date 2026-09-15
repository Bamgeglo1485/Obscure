using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Inventory.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Vanilla.MODSuit;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Trigger;
using Content.Shared.DoAfter;

using Robust.Shared.Timing;

namespace Content.Shared.Vanilla.MODSuit.Modules;

public sealed partial class ClothingReturnModuleSystem : XOnTriggerSystem<ClothingReturnOnTriggerComponent>
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingReturnModuleComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ClothingReturnModuleComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<MODSuitModuleComponent>(ent, out var module))
            return;

        var button = Spawn(ent.Comp.ReturnButtonProto, Transform(ent).Coordinates);
        EnsureComp<ClothingReturnOnTriggerComponent>(button, out var buttonComp);
        buttonComp.Module = (ent.Owner, module);
    }

    protected override void OnTrigger(Entity<ClothingReturnOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (ent.Comp.Module is null)
            return;

        var module = ent.Comp.Module.Value;
        if (module.Comp.MODSuit is null)
            return;

        var modSuit = module.Comp.MODSuit.Value;

        Spawn(ent.Comp.TeleportEffect, Transform(modSuit).Coordinates);

        _inventorySystem.TryUnequip(target, "outerClothing");
        if (!_inventorySystem.TryEquip(target, modSuit, "outerClothing", true, true))
            return;

        Spawn(ent.Comp.TeleportEffect, Transform(target).Coordinates);

        if (HasComp<ToggleableClothingComponent>(modSuit))
        {
            var doafter = new DoAfterArgs(EntityManager, target, TimeSpan.FromSeconds(0), new ToggleClothingDoAfterEvent(), modSuit, target, modSuit)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                DistanceThreshold = 2,
            };

            _doAfter.TryStartDoAfter(doafter);
        }

        args.Handled = true;
    }
}
