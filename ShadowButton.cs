using MiraAPI.Hud;
using MiraAPI.Utilities.Assets;
using NightwebRoles.Roles;
using UnityEngine;

namespace NightwebRoles.Buttons;

// Two separate buttons so each can get its own icon later. Only one is visible at a time:
// Shade while you are unshaded, Unshade while you are shaded.

[RegisterButton]   // VERIFY: attribute name / auto-registration in your MiraAPI version
public class ShadeButton : CustomActionButton
{
    public override string Name => "Shade";
    public override float Cooldown => ShadowState.Opt.ToggleCooldown;
    public override int MaxUses => 0;
    public override ButtonLocation Location => ButtonLocation.BottomLeft;
    public override LoadableAsset<Sprite> Sprite => MiraAssets.Empty;   // TODO: your own Shade icon

    public override bool Enabled(RoleBehaviour role) =>
        role is ShadowRole && !ShadowState.IsShaded(PlayerControl.LocalPlayer);

    public override bool CanUse() => base.CanUse() && !ShadowState.Transitioning(PlayerControl.LocalPlayer.PlayerId);

    protected override void OnClick() => ShadowRpc.RpcSetShaded(PlayerControl.LocalPlayer, true);
}

[RegisterButton]
public class UnshadeButton : CustomActionButton
{
    public override string Name => "Unshade";
    public override float Cooldown => ShadowState.Opt.ToggleCooldown;
    public override int MaxUses => 0;
    public override ButtonLocation Location => ButtonLocation.BottomLeft;   // same slot, swaps with Shade
    public override LoadableAsset<Sprite> Sprite => MiraAssets.Empty;       // TODO: your own Unshade icon

    public override bool Enabled(RoleBehaviour role) =>
        role is ShadowRole && ShadowState.IsShaded(PlayerControl.LocalPlayer);

    public override bool CanUse() => base.CanUse() && !ShadowState.Transitioning(PlayerControl.LocalPlayer.PlayerId);

    protected override void OnClick() => ShadowRpc.RpcSetShaded(PlayerControl.LocalPlayer, false);
}
