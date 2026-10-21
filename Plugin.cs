using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MiraAPI.PluginLoading;
using Reactor;

namespace NightwebRoles;

// VERIFY against the MiraAPI example mod: attribute and interface names shift between releases.
[BepInAutoPlugin("com.nightweb.roles", "Nightweb Roles")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraApiPlugin.Id)]
public partial class NightwebPlugin : BasePlugin, IMiraPlugin
{
    public string OptionsTitleText => "Nightweb";
    public ConfigFile GetConfigFile() => Config;

    public Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Harmony.PatchAll();
        Log.LogInfo("Nightweb Roles loaded");
    }
}
