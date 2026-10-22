using System;
using System.Collections;
using HarmonyLib;
using Reactor.Utilities;
using Reactor.Utilities.Attributes;
using UnityEngine;
using MiraAPI.GameOptions;
using NightwebRoles.Options;
using NightwebRoles.Roles;

namespace NightwebRoles.Patches;

/// <summary>Attached to a dead body. Look over time, as a share of DesaturationTime (10s example):
///   0%-20%   (0-1.99s)  fully normal
///   20%-70%  (2-6.99s)  mostly identifiable
///   70%-100% (7-9.99s)  almost unidentifiable
///   100%     (10s)      completely grey, unidentifiable
/// Everything scales with the setting.</summary>
[RegisterInIl2Cpp]
public class RecluseBodyFade(IntPtr ptr) : MonoBehaviour(ptr)
{
    static RecluseOptions Opt => OptionGroupSingleton<RecluseOptions>.Instance;
    static float Total => Opt.DesaturationTime;        // death -> fully grey

    // (share of Total at which the stage starts, saturation during that stage). 1 = normal color, 0 = fully grey.
    static readonly (float at, float sat)[] Stages = { (0f, 1f), (0.2f, 0.6f), (0.7f, 0.15f), (1f, 0f) };
    const float NormalUntil = 0.2f;
    public byte VictimId;
    public bool FullyFaded => Saturation <= 0.001f;
    public float Saturation { get; private set; } = 1f;
    float _age;
    SpriteRenderer[] _renders;

    void Start() => _renders = GetComponentsInChildren<SpriteRenderer>();

    void Update()
    {
        _age += Time.deltaTime;
        Saturation = SaturationAt(_age / Total);
        if (_renders == null || _age < Total * NormalUntil) return;
        var id = GameData.Instance.GetPlayerById(VictimId)?.DefaultOutfit.ColorId ?? 0;
        foreach (var r in _renders)
        {
            r.material.SetColor(PlayerMaterial.BodyColor, Fade(Palette.PlayerColors[id], Saturation, FadedBody));
            r.material.SetColor(PlayerMaterial.BackColor, Fade(Palette.ShadowColors[id], Saturation, FadedBack));
        }
    }

    static float SaturationAt(float share)
    {
        var sat = Stages[0].sat;
        foreach (var st in Stages)
            if (share >= st.at) sat = st.sat;
        return sat;
    }

    // Fully faded look: one fixed color for every body, so brightness can't give away who it was.
    // Measured from screenshots of the game's own colors (front body / shadow side), roughly:
    //   Black (0.30, 0.34, 0.36) / dark     Grey (0.52, 0.59, 0.65) / (0.27, 0.34, 0.40)
    // Both are cool blue-greys. This is a warm ash tone at about the midpoint of their brightness, so it
    // differs from both in brightness AND hue.
    // VERIFY in game next to Black and Grey players (and Tan/Brown) and tune.
    static readonly Color FadedBody = new(0.50f, 0.45f, 0.42f, 1f);
    static readonly Color FadedBack = new(0.27f, 0.23f, 0.21f, 1f);

    static Color Fade(Color c, float s, Color faded)
    {
        faded.a = c.a;
        return Color.Lerp(faded, c, s);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class RecluseMurderPatch
{
    public static void Postfix(PlayerControl __instance, PlayerControl target)
    {
        if (__instance.Data?.Role is not RecluseRole) return;
        Coroutines.Start(AttachWhenBodySpawns(target.PlayerId));
    }

    static IEnumerator AttachWhenBodySpawns(byte victimId)
    {
        // The DeadBody is spawned a moment after MurderPlayer runs.
        for (var i = 0; i < 30; i++)
        {
            foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
                if (body.ParentId == victimId && body.GetComponent<RecluseBodyFade>() == null)
                {
                    body.gameObject.AddComponent<RecluseBodyFade>().VictimId = victimId;
                    yield break;
                }
            yield return null;
        }
    }
}
