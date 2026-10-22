using System;
using System.Collections.Generic;
using MiraAPI.GameOptions;
using MiraAPI.Networking;
using MiraAPI.Roles;
using NightwebRoles.Options;
using UnityEngine;
using Reactor.Networking.Attributes;

namespace NightwebRoles.Roles;

/// <summary>
/// Shadow: Impostor with a Shaded / Unshaded toggle. While Shaded, living Crewmates see a solid
/// black default silhouette with no name, color, cosmetics or pet. The Shadow can still move, kill,
/// vent and sabotage. Impostors and ghosts see it normally, only slightly darker.
/// </summary>
public class ShadowRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ICustomRole
{
    public string RoleName => "Shadow";
    public string RoleDescription => "Slip into the dark.";
    public string RoleLongDescription => "Toggle Shaded: living Crewmates see only a black silhouette of you.";
    public Color RoleColor => Palette.ImpostorRed;   // vanilla Impostor red, same as every other Impostor role
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public CustomRoleConfiguration Configuration => new(this) { CanUseVent = true };
}

/// <summary>
/// Who is shaded, plus the shade/unshade transition. Updated on every client from the Shade RPC.
/// The transition is a deliberate, visible tell: a Crewmate who watches it happen knows the player
/// is the Impostor. The Shadow is never frozen during it and can keep moving, killing and venting.
/// </summary>
public static class ShadowState
{
    static readonly Dictionary<byte, float> Since = new();                  // playerId -> Time.time when shaded
    static readonly Dictionary<byte, (float at, bool on)> Toggle = new();   // last toggle: when, and direction

    public static ShadowOptions Opt => OptionGroupSingleton<ShadowOptions>.Instance;

    /// <summary>Fixed cost of shading/unshading (not a lobby setting). Also the witness tell.</summary>
    public const float TransitionTime = 2f;

    /// <summary>Shaded only counts while the player is alive and currently holds the Shadow role
    /// (so a Mutator that swaps away from Shadow is automatically unshaded).</summary>
    public static bool IsShaded(PlayerControl p) =>
        p != null && p.Data != null && !p.Data.IsDead && p.Data.Role is ShadowRole && Since.ContainsKey(p.PlayerId);

    public static float Elapsed(byte id) => Since.TryGetValue(id, out var t) ? Time.time - t : 0f;

    public static void Set(byte id, bool on)
    {
        Toggle[id] = (Time.time, on);
        if (on) Since[id] = Time.time; else Since.Remove(id);
    }

    /// <summary>0..1 progress of the current shade/unshade transition (1 = finished or none).</summary>
    public static float Progress(byte id)
    {
        if (!Toggle.TryGetValue(id, out var t)) return 1f;
        return Mathf.Clamp01((Time.time - t.at) / TransitionTime);
    }

    public static bool Transitioning(byte id) => Toggle.ContainsKey(id) && Progress(id) < 1f;

    /// <summary>How "dark" the player looks, 0 (normal) to 1 (fully shaded), for smooth tinting.</summary>
    public static float Blend(PlayerControl p)
    {
        if (p?.Data == null || p.Data.IsDead || p.Data.Role is not ShadowRole || !Toggle.TryGetValue(p.PlayerId, out var t)) return 0f;
        var pr = Progress(p.PlayerId);
        return t.on ? pr : 1f - pr;
    }

    /// <summary>Name, cosmetics and pet are hidden only once shading has finished, and stay hidden
    /// until unshading has finished. During the animation the player is still recognisable.</summary>
    public static bool Hidden(PlayerControl p)
    {
        if (!Toggle.TryGetValue(p.PlayerId, out var t)) return false;
        var done = Progress(p.PlayerId) >= 1f;
        return t.on ? done : !done;
    }

    public static void Clear() { Since.Clear(); Toggle.Clear(); }
}

public static class ShadowRpc
{
    // VERIFY: MiraAPI's MethodRpc signature (first arg is the sending player).
    [MethodRpc((uint)NightwebRpc.Shade)]
    public static void RpcSetShaded(PlayerControl player, bool shaded) => ShadowState.Set(player.PlayerId, shaded);
}
