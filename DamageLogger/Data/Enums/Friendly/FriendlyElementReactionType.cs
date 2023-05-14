namespace DamageLogger.Data.Enums.Friendly;

public enum FriendlyElementReactionType : uint
{
    None = 0,
    Overload = 1, // Explode
    Vaporize = 2, // Stream
    Burning = 3,
    Burned = 4,
    Wet = 5,
    Overgrow = 6,
    Melt = 7,
    Freeze = 8,
    AntiFire = 9,
    Rock = 10,
    SlowDown = 11,
    Shock = 12,
    Wind = 13,
    ElectroCharged = 14, // Electric
    Fire = 15,
    Superconduct = 16, // Superconductor
    SwirlPyro = 17, // SwirlFire
    SwirlHydro = 18, // SwirlWater
    SwirlElectro = 19, // SwirlElectric
    SwirlCryo = 20, // SwirlIce
    SwirlFireAccu = 21,
    SwirlWaterAccu = 22,
    SwirlElectricAccu = 23,
    SwirlIceAccu = 24,
    StickRock = 25,
    StickWater = 26,
    CrystallizePyro = 27, // CrystallizeFire
    CrystallizeHydro = 28, // CrystallizeWater
    CrystallizeElectro = 29, // CrystallizeElectric
    CrystallizeCryo = 30, // CrystallizeIce
    Shatter = 31, // FrozenBroken
    StickGrass = 32,
    Quicken = 33, // Overdose
    Aggravate = 34, // OverdoseElectric
    Spread = 35, // OverdoseGrass
    Burgeon = 36, // OvergrowMushroomFire
    Hyperbloom = 37 // OvergrowMushroomElectric
}