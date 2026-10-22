using System.Collections.Generic;
using HarmonyLib;
using NightwebRoles.Roles;
using UnityEngine;

namespace NightwebRoles.Patches;

/// <summary>
/// Applies the Shadow look locally, per viewer, from a 0..1 blend (see ShadowState.Blend):
///  - living Crewmates: body fades to a solid black silhouette. Name, cosmetics and pet stay visible
///    during the animation (so a witness can see who it was) and disappear only once fully shaded.
///  - Impostors and ghosts: the same animation, but only ever to a slightly darker normal look.
/// Nothing but the shaded flag crosses the network.
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class ShadowVisualPatch
{
    static readonly HashSet<byte> Active = new();
    const float DimFactor = 0.7f;

    public static void Postfix(PlayerControl __instance)
    {
        var me = PlayerControl.LocalPlayer;
        if (me == null || __instance.Data == null || __instance.cosmetics == null) return;
        var id = __instance.PlayerId;

        // Auto-unshade when the host-set maximum shaded time runs out (owner only).
        if (__instance.AmOwner && ShadowState.IsShaded(__instance)
            && ShadowState.Elapsed(id) >= ShadowState.Opt.MaxShadedTime)
            ShadowRpc.RpcSetShaded(__instance, false);

        var b = ShadowState.Blend(__instance);
        var hidden = ShadowState.Hidden(__instance) && __instance.Data.Role is ShadowRole && !__instance.Data.IsDead;
        if (b <= 0f && !hidden)
        {
            if (Active.Remove(id)) Restore(__instance);
            return;
        }
        Active.Add(id);

        var c = __instance.Data.DefaultOutfit.ColorId;
        Color body = Palette.PlayerColors[c], back = Palette.ShadowColors[c], visor = Palette.VisorColor;
        var knowsTruth = me.Data.IsDead || me.Data.Role.IsImpostor;

        if (knowsTruth)
        {
            var f = Mathf.Lerp(1f, DimFactor, b);
            SetBody(__instance, Scale(body, f), Scale(back, f), Scale(visor, f));
            ShowCosmetics(__instance, true);
        }
        else
        {
            var f = 1f - b;                       // toward black
            SetBody(__instance, Scale(body, f), Scale(back, f), Scale(visor, f));
            ShowCosmetics(__instance, !hidden);   // hidden only once shading has finished (see ShadowState.Hidden)
        }
    }

    static Color Scale(Color c, float f) => new(c.r * f, c.g * f, c.b * f, c.a);

    static void ShowCosmetics(PlayerControl p, bool visible)
    {
        var c = p.cosmetics;
        c.nameText.enabled = visible;
        if (c.colorBlindText != null) c.colorBlindText.gameObject.SetActive(visible);
        if (c.hat != null) c.hat.gameObject.SetActive(visible);
        if (c.skin != null) c.skin.gameObject.SetActive(visible);
        if (c.visor != null) c.visor.gameObject.SetActive(visible);
        if (c.currentPet != null) c.currentPet.gameObject.SetActive(visible);
    }

    static void Restore(PlayerControl p)
    {
        ShowCosmetics(p, true);
        p.RawSetColor(p.Data.DefaultOutfit.ColorId);                      // VERIFY: resets body/back/visor colors
    }

    static void SetBody(PlayerControl p, Color body, Color back, Color visor)
    {
        var m = p.cosmetics.currentBodySprite.BodySprite.material;
        m.SetColor(PlayerMaterial.BodyColor, body);
        m.SetColor(PlayerMaterial.BackColor, back);
        m.SetColor(PlayerMaterial.VisorColor, visor);
    }
}

[HarmonyPatch]
public static class ShadowResetPatches
{
    // Everyone is unshaded at the start of a new round and a new game.
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    [HarmonyPostfix]
    public static void AfterMeeting() => ShadowState.Clear();

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    [HarmonyPostfix]
    public static void NewGame() => ShadowState.Clear();
}
