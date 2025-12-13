public enum WeightClass
{
    Negative, // Balloons (Floats up in air)
    Light,    // Feathers (Floats on water, pushed by wind)
    Medium,   // Player, Crates (Standard physics, neutral buoyancy)
    Heavy,    // Metal, Rocks (Sinks, activates heavy switches)
    Massive   // Unmovable geometry
}