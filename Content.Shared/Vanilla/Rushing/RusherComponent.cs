using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Vanilla.Rushing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RusherComponent : Component
{
    [DataField]
    public float StaminaLoss = 25f;

    [DataField, AutoNetworkedField]
    public float Speed = 6f;

    [DataField, AutoNetworkedField]
    public float DistanceModifier = 0.35f;

    [DataField]
    public TimeSpan KnockdownOthersDelay = TimeSpan.FromSeconds(2.5f);

    [DataField, AutoNetworkedField]
    public bool Rushing = false;

    [DataField, AutoNetworkedField]
    public bool KnockdownOthers = false;

    [DataField]
    public float KnockdownOthersStaminaLoss = 25f;
}
