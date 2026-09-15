using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

using Content.Shared.Damage;

namespace Content.Shared.Vanilla.MODSuit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MODSuitModuleComponent : Component
{
    // Компненты, которые даёт модуль МОДсьюту
    [DataField]
    public ComponentRegistry Components = new();

    // Компненты, которые даёт модуль носящему
    [DataField]
    public ComponentRegistry UserComponents = new();

    // Акшенсы, которые даёт модуль
    [DataField, AutoNetworkedField]
    public List<EntProtoId> Actions = new();

    // Интерфейсы, которые даются МОДу
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    internal Dictionary<Enum, InterfaceData> Interfaces = new();

    // Модификатор скорости, которую даёт модуль
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.0f;

    // Броня, которую даёт модуль
    [DataField, AutoNetworkedField]
    public DamageModifierSet ArmorModifiers = new();

    // Ключ для ограничения по количеству модулей
    [DataField]
    public string? OccupyKey;

    // Сколько максимум модулей с одинаковым OccupyKey
    // Работает только с указанным OccupyKey
    [DataField]
    public int MaxOccupies = 1;

    // Спрайт для надетого модсьюта
    [DataField, AutoNetworkedField]
    public string? EquippedState;

    // Спрайт для иконки модсьюта
    [DataField, AutoNetworkedField]
    public string? IconState;

    // Приоритет спрайта над другими модулями
    // Чем выше число - тем выше спрайт
    [DataField]
    public int SpritePriority = 1;

    // Текущий модсьют
    [ViewVariables]
    public EntityUid? MODSuit;
}
