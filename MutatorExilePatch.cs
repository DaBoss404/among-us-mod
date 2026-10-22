using HarmonyLib;
using MiraAPI.GameOptions;
using NightwebRoles.Options;
using NightwebRoles.Roles;

namespace NightwebRoles.Patches;

/// <summary>
/// Ejection reveal: a player whose current role is Mutator is announced as "Mutator was ejected."
/// A Mutator that is holding an adopted role is NOT treated specially here: the ejection screen
/// behaves like vanilla for that role.
/// </summary>
[HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
public static class MutatorExilePatch
{
    // VERIFY: the parameter type is NetworkedPlayerInfo in recent game versions
    // (older versions used GameData.PlayerInfo).
    public static void Postfix(ExileController __instance, NetworkedPlayerInfo exiled)
    {
        var player = exiled?.Object;
        if (!OptionGroupSingleton<MutatorOptions>.Instance.RevealOnEjection) return;   // host toggle
        if (player == null || player.Data.Role is not MutatorRole) return;

        // The text is typed out from completeString after Begin returns, so setting it here is enough.
        __instance.completeString = "Mutator was ejected.";
    }
}
