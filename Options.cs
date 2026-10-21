using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using NightwebRoles.Roles;

namespace NightwebRoles.Options;

// VERIFY against the MiraAPI example mod: group base class and attribute argument names.
// MiraAPI syncs these from the host to every client, so patches just read them.

public class RecluseOptions : AbstractOptionGroup<RecluseRole>
{
    public override string GroupName => "Recluse";

    // Total time from death until the body is fully desaturated (unidentifiable). Stages scale with it:
    // 0-20% fully normal, 20-70% mostly identifiable, 70-100% almost unidentifiable, 100% completely grey.
    [ModdedNumberOption("Desaturation Time", min: 5, max: 60, increment: 1, suffixType: MiraNumberSuffixes.Seconds)]
    public float DesaturationTime { get; set; } = 22f;
}


public class ShadowOptions : AbstractOptionGroup<ShadowRole>
{
    public override string GroupName => "Shadow";

    [ModdedNumberOption("Toggle Cooldown", min: 1, max: 60, increment: 1, suffixType: MiraNumberSuffixes.Seconds)]
    public float ToggleCooldown { get; set; } = 15f;

    [ModdedNumberOption("Max Shaded Time", min: 5, max: 60, increment: 1, suffixType: MiraNumberSuffixes.Seconds)]
    public float MaxShadedTime { get; set; } = 30f;
}

public class MutatorOptions : AbstractOptionGroup<MutatorRole>
{
    public override string GroupName => "Mutator";

    // The Mutator's only setting. Swaps are fixed at 1 per round (see MutatorState.MaxPerRound).
    [ModdedToggleOption("Reveal Mutator On Ejection")]
    public bool RevealOnEjection { get; set; } = true;
}
