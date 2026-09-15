using Robust.Shared.Timing;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared.Vanilla.MODSuit.Modules;

[RegisterComponent]
public sealed partial class SpringlockComponent : Component
{
    // Случился ли ПРИКОЛ
    [DataField]
    public bool Locked = false;

    // Закончился ли ПРИКОЛ
    [ViewVariables]
    public bool LockEnded = false;

    // Ускорение
    [DataField]
    public float SpeedModifier = 1.5f;

    // Количество нанесённых реагентов для ПРИКОЛА
    [DataField]
    public float ReagentsToLock = 3f;

    // Текущий пользователь
    [ViewVariables]
    public EntityUid? User;

    // Длительность спринглока
    [DataField]
    public TimeSpan LockDelay = TimeSpan.FromSeconds(14.5f);

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Slash", FixedPoint2.New(5) },
            { "Piercing", FixedPoint2.New(5) },
            { "Blunt", FixedPoint2.New(5) }
        }
    };

    [DataField]
    public DamageSpecifier FinalDamage = new()
    {
        DamageDict = new()
        {
            { "Slash", FixedPoint2.New(100) },
            { "Piercing", FixedPoint2.New(10) }
        }
    };

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Vanilla/Effects/springlock.ogg");

    // Длительность апдейта 
    [DataField]
    public TimeSpan LockUpdateDelay = TimeSpan.FromSeconds(2f);

    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan LockEnd = TimeSpan.Zero;
}
