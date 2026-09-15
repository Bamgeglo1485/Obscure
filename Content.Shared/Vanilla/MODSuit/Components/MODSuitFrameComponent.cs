using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

using Content.Shared.Damage;

namespace Content.Shared.Vanilla.MODSuit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MODSuitFrameComponent : Component
{
    // Скорость движения
    [ViewVariables, AutoNetworkedField]
    public float SpeedModifier = 1f;

    // Броня
    [ViewVariables, AutoNetworkedField]
    public DamageModifierSet ArmorModifiers = new();

    // Акшены
    [ViewVariables, AutoNetworkedField]
    public List<EntProtoId> Actions = new();

    // Акшены
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> ActionEntities = new();

    // Вставленные модули
    [ViewVariables]
    public List<Entity<MODSuitModuleComponent>> Modules = new();

    // Для клиентской стороны
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> ModuleEntities = new();

    // Текущий пользователь
    [ViewVariables, AutoNetworkedField]
    public EntityUid? User;

    // Хранилище модулей
    [DataField]
    public string SlotId = "storagebase";

    // Спрайты модулей
    [DataField("sprite"), AutoNetworkedField]
    public ResPath? RSIPath;
}
