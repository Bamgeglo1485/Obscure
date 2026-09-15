using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vanilla.MODSuit.Modules;

[RegisterComponent]
public sealed partial class ClothingReturnModuleComponent : Component
{
    [DataField]
    public EntProtoId ReturnButtonProto = "ClothingReturnButton";
}
