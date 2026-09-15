using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Vanilla.MODSuit.Modules;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Vanilla.MODSuit.Modules;

public sealed partial class SharedSpringlockSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpringlockComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMoveSpeed);
    }

    private void OnRefreshMoveSpeed(Entity<SpringlockComponent> ent, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (!ent.Comp.Locked)
            args.Args.ModifySpeed(ent.Comp.SpeedModifier, ent.Comp.SpeedModifier);
    }
}
