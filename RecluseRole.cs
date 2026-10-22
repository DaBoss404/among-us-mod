using System;
using MiraAPI.Roles;
using UnityEngine;

namespace NightwebRoles.Roles;

/// <summary>
/// Recluse: Impostor whose kill is a Bite. Bite itself is the vanilla kill button
/// (normal kill cooldown); the web/fade effect is added by RecluseMurderPatch.
/// </summary>
[RegisterCustomRole]
public class RecluseRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ICustomRole
{
    public string RoleName => "Recluse";
    public string RoleDescription => "Bite your prey. Webs hide the evidence.";
    public string RoleLongDescription =>
        "Your Bite replaces Kill. The body stays normal for a few seconds, then fades until it can no longer be identified.";
    public Color RoleColor => Palette.ImpostorRed;   // vanilla Impostor red, same as every other Impostor role
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;

    public CustomRoleConfiguration Configuration => new(this)
    {
        // Mutator needs this role to be adoptable later; keep the kit self-contained.
        CanUseVent = true,
    };
}
