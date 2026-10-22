using MiraAPI.Hud;
using MiraAPI.Utilities.Assets;
using NightwebRoles.Roles;
using UnityEngine;

namespace NightwebRoles.Buttons;

/// <summary>HUD button that opens the role menu. Shown while the player is (or was this round) a Mutator.</summary>
[RegisterButton]   // VERIFY: attribute name / auto-registration in your MiraAPI version
public class MutateButton : CustomActionButton
{
    public override string Name => "Mutate";
    public override float Cooldown => 1f;
    public override int MaxUses => 0;                       // limit is tracked by MutatorState
    public override ButtonLocation Location => ButtonLocation.BottomRight;
    public override LoadableAsset<Sprite> Sprite => MiraAssets.Empty;   // TODO: your own icon

    public override bool Enabled(RoleBehaviour role) =>
        MutatorState.IsMutator(PlayerControl.LocalPlayer) && !PlayerControl.LocalPlayer.Data.IsDead;

    public override bool CanUse() =>
        base.CanUse() && MutatorState.UsesLeft(PlayerControl.LocalPlayer) > 0 && !MutatorMenu.IsOpen;

    protected override void OnClick() => MutatorMenu.Open();
}
