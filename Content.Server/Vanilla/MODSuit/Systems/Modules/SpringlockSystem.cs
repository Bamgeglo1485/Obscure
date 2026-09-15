using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

using Robust.Server.Audio;

using Content.Shared.Interaction.Components;
using Content.Shared._Funkystation.Stains.Components;
using Content.Shared._Funkystation.Stains.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Inventory;
using Content.Shared.Vanilla.MODSuit.Modules;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Speech;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Stunnable;
using Content.Shared.Jittering;

using Content.Server.Chat.Systems;

namespace Content.Server.Vanilla.MODSuit.Modules;

public sealed partial class SpringlockSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = null!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpringlockComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<SpringlockComponent>();
        while (enumerator.MoveNext(out var uid, out var spring))
        {
            if (!spring.Locked || spring.LockEnded)
                continue;

            if (spring.NextUpdate > _timing.CurTime)
                continue;

            spring.NextUpdate = _timing.CurTime + spring.LockUpdateDelay;

            var parent = Transform(uid).ParentUid;
            if (!HasComp<InventoryComponent>(parent))
                continue;

            _damageable.TryChangeDamage(parent, spring.Damage);
            _chatSystem.TryEmoteWithChat(parent, new ProtoId<EmotePrototype>("Scream"));
            _stun.TryAddParalyzeDuration(parent, TimeSpan.FromSeconds(2.5f));
            _jitter.AddJitter(parent, 20, 20);

            if (spring.LockEnd < _timing.CurTime)
            {
                spring.LockEnded = true;
                _damageable.TryChangeDamage(parent, spring.FinalDamage);
                RemComp<JitteringComponent>(parent);
            }
        }
    }

    private void OnSolutionChanged(Entity<SpringlockComponent> ent, ref SolutionChangedEvent args)
    {
        if (ent.Comp.Locked)
            return;

        if (!_solution.TryGetSolution(ent.Owner, "stain", out _, out var solution))
            return;

        if (solution.Volume < ent.Comp.ReagentsToLock)
            return;

        var parent = Transform(ent).ParentUid;
        if (!HasComp<InventoryComponent>(parent))
            return;

        ent.Comp.Locked = true;
        ent.Comp.LockEnd = _timing.CurTime + ent.Comp.LockDelay;
        ent.Comp.NextUpdate = _timing.CurTime;

        EnsureComp<UnremoveableComponent>(ent);
        _popup.PopupEntity("Пружинный механизм ЗАЩЁЛКИВАЕТСЯ!!!", ent.Owner, PopupType.LargeCaution);

        if (ent.Comp.Sound != null)
            _audio.PlayPvs(ent.Comp.Sound, parent);
    }
}
