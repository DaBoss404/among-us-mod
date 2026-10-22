using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Networking;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using NightwebRoles.Options;
using UnityEngine;

namespace NightwebRoles.Roles;

public enum NightwebRpc : uint { Mutate = 0xB101, Shade = 0xB102 }

/// <summary>
/// Mutator: base Impostor. In the active phase it can swap to any enabled Impostor role,
/// including the vanilla Shapeshifter, Phantom and Viper (but not itself or plain Impostor). The swap replaces the current kit, never stacks, and
/// reverts to Mutator after the next meeting. No visual tell: it is just a role change.
/// </summary>
public class MutatorRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ICustomRole
{
    public string RoleName => "Mutator";
    public string RoleDescription => "Become another Impostor.";
    public string RoleLongDescription => "Once per round, adopt the kit of any enabled Impostor role (Shapeshifter, Phantom, Viper and more). You revert after the next meeting.";
    public Color RoleColor => Palette.ImpostorRed;   // vanilla Impostor red, same as every other Impostor role
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public CustomRoleConfiguration Configuration => new(this) { CanUseVent = true };
}

/// <summary>Per-round bookkeeping. Updated on every client from the Mutate RPC.</summary>
public static class MutatorState
{
    // playerId -> swaps used this round. A key exists once a player has mutated, which lets
    // us keep showing the Mutate button after their role has changed to something else.
    static readonly Dictionary<byte, int> Used = new();

    public const int MaxPerRound = 1;     // one swap per round, not a lobby setting

    public static bool IsMutator(PlayerControl p) =>
        p != null && p.Data != null && (p.Data.Role is MutatorRole || Used.ContainsKey(p.PlayerId));

    public static int UsesLeft(PlayerControl p) => MaxPerRound - (Used.TryGetValue(p.PlayerId, out var n) ? n : 0);

    public static void Record(byte id) => Used[id] = (Used.TryGetValue(id, out var n) ? n : 0) + 1;
    public static IEnumerable<byte> Mutators() => Used.Keys.ToArray();
    public static void ResetCounts() { foreach (var k in Used.Keys.ToArray()) Used[k] = 0; }
    public static void Clear() => Used.Clear();

    /// <summary>Enabled Impostor-team roles (Shapeshifter, Phantom, Viper, custom ones), minus plain Impostor and Mutator itself.</summary>
    public static List<RoleBehaviour> Candidates()
    {
        var opts = GameOptionsManager.Instance.CurrentGameOptions.RoleOptions;
        return RoleManager.Instance.AllRoles.ToArray()
            .Where(r => r.TeamType == RoleTeamTypes.Impostor
                        && r.Role != RoleTypes.Impostor
                        && r is not MutatorRole
                        && opts.GetNumPerGame(r.Role) > 0)
            .ToList();
    }
}

public static class MutatorRpc
{
    // VERIFY: MiraAPI's MethodRpc signature (first arg is the sending player).
    [MethodRpc((uint)NightwebRpc.Mutate)]
    public static void RpcMutate(PlayerControl player, ushort roleType)
    {
        MutatorState.Record(player.PlayerId);
        if (AmongUsClient.Instance.AmHost) player.RpcSetRole((RoleTypes)roleType, true);
    }

    public static void Revert(PlayerControl p)
    {
        var mutator = (RoleTypes)RoleId.Get<MutatorRole>();
        if (p.Data.Role.Role != mutator) p.RpcSetRole(mutator, true);
    }
}

[HarmonyPatch]
public static class MutatorPatches
{
    // New round = meeting just ended: host reverts everyone, all clients refill swaps.
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    [HarmonyPostfix]
    public static void AfterMeeting()
    {
        if (AmongUsClient.Instance.AmHost)
            foreach (var id in MutatorState.Mutators())
            {
                var p = GameData.Instance.GetPlayerById(id)?.Object;
                if (p != null && !p.Data.IsDead) MutatorRpc.Revert(p);
            }
        MutatorState.ResetCounts();
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    [HarmonyPostfix]
    public static void NewGame() => MutatorState.Clear();
}
