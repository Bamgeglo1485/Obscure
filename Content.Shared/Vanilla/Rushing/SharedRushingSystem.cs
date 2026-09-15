using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Gravity;
using Content.Shared.CombatMode;
using Content.Shared.Standing;

using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Vanilla.Rushing;

public sealed partial class SharedRushingSystem : EntitySystem
{
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RusherComponent, KnockedDownEvent>(OnDown);
        SubscribeLocalEvent<RusherComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<RusherComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<RusherComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.Rushing || !ent.Comp.KnockdownOthers)
            return;

        var other = args.OtherEntity;

        if (other == ent.Owner)
            return;

        if (_gravity.IsWeightless(other))
            return;

        if (!TryComp<CrawlerComponent>(other, out var otherCrawler))
            return;

        _stun.TryKnockdown((other, otherCrawler), ent.Comp.KnockdownOthersDelay, true, true, false);
        ent.Comp.KnockdownOthers = false;

        if (TryComp<StaminaComponent>(other, out var otherStamina))
            _stamina.TakeStaminaDamage(other, ent.Comp.KnockdownOthersStaminaLoss, otherStamina, ent);

        if (TryComp<StaminaComponent>(ent, out var stamina))
            _stamina.TakeStaminaDamage(ent, ent.Comp.KnockdownOthersStaminaLoss, stamina, ent, ent, ignoreResist: true);
    }

    private void OnLand(Entity<RusherComponent> ent, ref LandEvent ev)
    {
        ent.Comp.Rushing = false;
        Dirty(ent);
    }

    private void OnDown(Entity<RusherComponent> ent, ref KnockedDownEvent ev)
    {
        if (!TryComp<InputMoverComponent>(ent, out var input))
            return;

        if (!input.HasDirectionalMovement)
            return;

        if (_gravity.IsWeightless(ent.Owner))
            return;

        if (!TryComp<StaminaComponent>(ent, out var stamina) || stamina.CritThreshold - stamina.StaminaDamage <= ent.Comp.StaminaLoss)
            return;

        if (!TryComp<KnockedDownComponent>(ent, out var knockedDown))
            return;

        if (knockedDown.AutoStand)
            return;

        var transform = Transform(ent);
        var direction = input.WishDir * ent.Comp.DistanceModifier;

        _throwing.TryThrow(ent, direction, ent.Comp.Speed);
        _stamina.TakeStaminaDamage(ent, ent.Comp.StaminaLoss, stamina, ent, ent, ignoreResist: true);

        ent.Comp.Rushing = true;

        if (TryComp<CombatModeComponent>(ent, out var combat) && combat.IsInCombatMode)
            ent.Comp.KnockdownOthers = true;

        Dirty(ent);
    }
}
