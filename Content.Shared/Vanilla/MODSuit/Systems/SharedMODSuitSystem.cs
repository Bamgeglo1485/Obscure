using Robust.Shared.Serialization;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Network;

using Content.Shared.Movement.Systems;
using Content.Shared.Inventory;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Armor;
using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Vanilla.MODSuit;

public abstract partial class SharedMODSuitSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MODSuitFrameComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMoveSpeed);

        SubscribeLocalEvent<MODSuitFrameComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(OnCoefficientQuery);
        SubscribeLocalEvent<MODSuitFrameComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);

        SubscribeLocalEvent<MODSuitFrameComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<MODSuitFrameComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<MODSuitFrameComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<MODSuitFrameComponent, ContainerIsRemovingAttemptEvent>(OnRemoveAttempt);

        SubscribeLocalEvent<MODSuitFrameComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MODSuitFrameComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    public bool GetMODSuit(EntityUid uid, out Entity<MODSuitFrameComponent> modSuit)
    {
        modSuit = default;

        if (uid == EntityUid.Invalid || Deleted(uid))
            return false;

        if (TryComp<InventoryComponent>(uid, out _))
        {
            if (_inventorySystem.TryGetSlotEntity(uid, "outerClothing", out var outerClothing))
            {
                if (TryComp<MODSuitFrameComponent>(outerClothing, out var outerFrameComp))
                {
                    modSuit = (outerClothing.Value, outerFrameComp);
                    return true;
                }
            }
        }

        var current = uid;
        for (var i = 0; i < 5; i++)
        {
            var parent = Transform(current).ParentUid;

            if (parent == EntityUid.Invalid || Deleted(parent))
                break;

            if (TryComp<MODSuitFrameComponent>(parent, out var parentFrameComp))
            {
                modSuit = (parent, parentFrameComp);
                return true;
            }

            if (TryComp<InventoryComponent>(parent, out _))
            {
                if (_inventorySystem.TryGetSlotEntity(parent, "outerClothing", out var outerClothing))
                {
                    if (TryComp<MODSuitFrameComponent>(outerClothing, out var outerFrameComp))
                    {
                        modSuit = (outerClothing.Value, outerFrameComp);
                        return true;
                    }
                }
            }

            current = parent;
        }

        return false;
    }

    private void OnRefreshMoveSpeed(Entity<MODSuitFrameComponent> ent, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        args.Args.ModifySpeed(ent.Comp.SpeedModifier, ent.Comp.SpeedModifier);
    }

    private void OnCoefficientQuery(Entity<MODSuitFrameComponent> ent, ref InventoryRelayedEvent<CoefficientQueryEvent> args)
    {
        foreach (var (damageType, coefficient) in ent.Comp.ArmorModifiers.Coefficients)
        {
            if (args.Args.DamageModifiers.Coefficients.TryGetValue(damageType, out var current))
            {
                args.Args.DamageModifiers.Coefficients[damageType] = current * coefficient;
            }
            else
            {
                args.Args.DamageModifiers.Coefficients[damageType] = coefficient;
            }
        }
    }

    private void OnDamageModify(Entity<MODSuitFrameComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, ent.Comp.ArmorModifiers);
    }

    private void OnInsertAttempt(Entity<MODSuitFrameComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (!TryComp<MODSuitModuleComponent>(args.EntityUid, out var module))
            return;

        if (!BaseContainerChecks(ent, args))
            return;

        // НЕЛЬЗЯ ВЗАИМОДЕЙСТВОВАТЬ С МОДУЛЯМИ, ЕСЛИ МОДСЬЮТ НАДЕТ
        var parent = Transform(ent.Owner).ParentUid;
        if (HasComp<InventoryComponent>(parent))
        {
            _popup.PopupEntity("МОД должен быть снятым для взаимодействия", args.EntityUid);
            args.Cancel();
        }

        if (IsModuleOccupied((args.EntityUid, module), ent.Comp))
        {
            _popup.PopupEntity("Слишком много модулей одного типа", args.EntityUid);
            args.Cancel();
        }
    }

    // если вставлен модуль
    private void OnInserted(Entity<MODSuitFrameComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ent.Comp.SlotId)
            return;

        var entity = args.Entity;

        if (!TryComp<MODSuitModuleComponent>(entity, out var module))
            return;

        ent.Comp.Modules.Add((entity, module));
        ent.Comp.ModuleEntities.Add(entity);

        module.MODSuit = ent.Owner;

        // добавляем компоненты модуля МОДу
        if (module.Components.Count > 0)
            EntityManager.AddComponents(ent, module.Components, true);

        foreach (var action in module.Actions)
        {
            ent.Comp.Actions.Add(action);
        }

        // добавляем интерфейсы
        AddModuleInterfaces(ent, module);

        UpdateModifiers(ent);

        Dirty(ent);

        var ev = new MODSuitModuleInserted(GetNetEntity(entity), GetNetEntity(ent));
        RaiseNetworkEvent(ev);
    }

    private void OnRemoveAttempt(Entity<MODSuitFrameComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (!TryComp<MODSuitModuleComponent>(args.EntityUid, out var module))
            return;

        if (!BaseContainerChecks(ent, args))
            return;

        // НЕЛЬЗЯ ВЗАИМОДЕЙСТВОВАТЬ С МОДУЛЯМИ, ЕСЛИ МОДСЬЮТ НАДЕТ
        var parent = Transform(ent.Owner).ParentUid;
        if (HasComp<InventoryComponent>(parent))
        {
            _popup.PopupEntity("МОД должен быть снятым для взаимодействия", args.EntityUid);
            args.Cancel();
        }
    }

    // если убран модуль
    private void OnRemoved(Entity<MODSuitFrameComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ent.Comp.SlotId)
            return;

        var entity = args.Entity;

        if (!TryComp<MODSuitModuleComponent>(entity, out var module))
            return;

        ent.Comp.Modules.Remove((entity, module));
        ent.Comp.ModuleEntities.Remove(entity);

        module.MODSuit = null;

        // убираем компоненты модуля
        if (module.Components.Count > 0)
            EntityManager.RemoveComponents(ent, module.Components);

        // убираем интерфейсы
        RemoveModuleInterfaces(ent, module);

        UpdateModifiers(ent);

        foreach (var action in module.Actions)
        {
            ent.Comp.Actions.Remove(action);
        }

        Dirty(ent);

        var ev = new MODSuitModuleRemoved(GetNetEntity(entity), GetNetEntity(ent));
        RaiseNetworkEvent(ev);
    }

    // если МОД надет
    private void OnGotEquipped(Entity<MODSuitFrameComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.User = args.EquipTarget;
        if (ent.Comp.User != null)
        {
            foreach (var module in ent.Comp.Modules)
            {
                if (module.Comp.UserComponents.Count > 0)
                    EntityManager.AddComponents(ent.Comp.User.Value, module.Comp.UserComponents, true);
            }
        }

        foreach (var action in ent.Comp.Actions)
        {
            var actionEnt = _actions.AddAction(args.EquipTarget, action);
            if (actionEnt != null)
                ent.Comp.ActionEntities.Add(actionEnt.Value);
        }
    }

    // если МОД снят
    private void OnGotUnequipped(Entity<MODSuitFrameComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.User = null;
        foreach (var module in ent.Comp.Modules)
        {
            if (module.Comp.UserComponents.Count > 0)
                EntityManager.RemoveComponents(args.EquipTarget, module.Comp.UserComponents);
        }

        foreach (var action in ent.Comp.ActionEntities)
        {
            _actions.RemoveAction(args.EquipTarget, action);
        }
    }

    private void AddModuleInterfaces(Entity<MODSuitFrameComponent> modSuit, MODSuitModuleComponent module)
    {
        if (module.Interfaces.Count == 0)
            return;

        foreach (var (key, interfaceData) in module.Interfaces)
        {
            if (!_uiSystem.HasUi(modSuit.Owner, key))
            {
                _uiSystem.SetUi(modSuit.Owner, key, interfaceData);

                if (modSuit.Comp.User != null)
                    _uiSystem.OpenUi(modSuit.Owner, key, modSuit.Comp.User.Value);
            }
        }
    }

    private void RemoveModuleInterfaces(Entity<MODSuitFrameComponent> modSuit, MODSuitModuleComponent module)
    {
        if (module.Interfaces.Count == 0)
            return;

        if (!TryComp<UserInterfaceComponent>(modSuit.Owner, out _))
            return;

        foreach (var (key, _) in module.Interfaces)
        {
            if (_uiSystem.HasUi(modSuit.Owner, key))
            {
                _uiSystem.CloseUi(modSuit.Owner, key);

                _uiSystem.SetUiState(modSuit.Owner, key, null);
            }
        }
    }

    private void UpdateModifiers(Entity<MODSuitFrameComponent> ent)
    {
        ent.Comp.SpeedModifier = 1.0f;
        ent.Comp.ArmorModifiers = new DamageModifierSet();

        foreach (var module in ent.Comp.Modules)
        {
            if (Deleted(module))
                continue;

            if (module.Comp.SpeedModifier != 1.0f)
                ent.Comp.SpeedModifier *= module.Comp.SpeedModifier;

            if (module.Comp.ArmorModifiers != null)
            {
                foreach (var (damageType, coefficient) in module.Comp.ArmorModifiers.Coefficients)
                {
                    if (ent.Comp.ArmorModifiers.Coefficients.TryGetValue(damageType, out var currentCoeff))
                    {
                        ent.Comp.ArmorModifiers.Coefficients[damageType] = currentCoeff * coefficient;
                    }
                    else
                    {
                        ent.Comp.ArmorModifiers.Coefficients[damageType] = coefficient;
                    }
                }
            }
        }
    }

    private bool BaseContainerChecks(Entity<MODSuitFrameComponent> ent, ContainerAttemptEventBase args)
    {
        if (args.Container.ID != ent.Comp.SlotId)
            return false;

        if (_timing.ApplyingState)
            return false;

        if (args.Cancelled)
            return false;

        return true;
    }

    private bool IsModuleOccupied(Entity<MODSuitModuleComponent> ent, MODSuitFrameComponent frame)
    {
        if (ent.Comp.OccupyKey == null)
            return false;

        var occupies = 0;

        foreach (var module in frame.ModuleEntities)
        {
            if (!TryComp<MODSuitModuleComponent>(module, out var comp))
                continue;

            if (comp.OccupyKey != null && comp.OccupyKey == ent.Comp.OccupyKey)
                occupies++;
        }

        if (occupies >= ent.Comp.MaxOccupies)
            return true;

        return false;
    }
}

[Serializable, NetSerializable]
public sealed class MODSuitModuleInserted : EntityEventArgs
{
    public NetEntity Module;
    public NetEntity MODSuit;

    public MODSuitModuleInserted(NetEntity module, NetEntity modSuit)
    {
        Module = module;
        MODSuit = modSuit;
    }
}

[Serializable, NetSerializable]
public sealed class MODSuitModuleRemoved : EntityEventArgs
{
    public NetEntity Module;
    public NetEntity MODSuit;

    public MODSuitModuleRemoved(NetEntity module, NetEntity modSuit)
    {
        Module = module;
        MODSuit = modSuit;
    }
}
