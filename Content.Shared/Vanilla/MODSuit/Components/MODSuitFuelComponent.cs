using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

using Content.Shared.Damage;

namespace Content.Shared.Vanilla.MODSuit;

[RegisterComponent]
public sealed partial class MODSuitFuelComponent : Component
{
    // Хранилище топлива
    [DataField]
    public string FuelSlotId = "fuel_slot";
}
