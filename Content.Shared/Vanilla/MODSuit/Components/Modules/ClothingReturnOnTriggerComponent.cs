using Content.Shared.Trigger.Components.Effects;

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vanilla.MODSuit.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClothingReturnOnTriggerComponent : BaseXOnTriggerComponent
{
    [ViewVariables]
    public Entity<MODSuitModuleComponent>? Module;

    [DataField]
    public EntProtoId TeleportEffect = "EffectFlashBluespaceExplosion";
}
