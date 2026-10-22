using System;
using System.Collections.Generic;
using NightwebRoles.Roles;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace NightwebRoles.Buttons;

/// <summary>
/// Simple pick-a-role popup: a dim backdrop, one clickable row per candidate role, a Cancel row.
/// This is the most version-sensitive file (it builds UI from game prefabs). If it misbehaves,
/// only this file needs to change; the rest of the Mutator does not care how the choice is made.
/// </summary>
public static class MutatorMenu
{
    static GameObject _root;
    public static bool IsOpen => _root != null;

    public static void Open()
    {
        if (IsOpen) return;
        var hud = HudManager.Instance;
        _root = new GameObject("MutatorMenu");
        _root.transform.SetParent(hud.transform, false);
        _root.transform.localPosition = new Vector3(0f, 0f, -50f);

        Box("Backdrop", Vector2.zero, new Vector2(30f, 20f), new Color(0, 0, 0, 0.55f), Close, 0);

        var roles = MutatorState.Candidates();
        var rows = roles.Count + 1;
        var top = (rows - 1) * 0.35f;
        Box("Panel", new Vector2(0, 0), new Vector2(3.6f, rows * 0.7f + 0.9f), new Color(0.08f, 0.08f, 0.1f, 0.95f), null, 1);
        Label("Choose a role", new Vector3(0, top + 0.65f, -2f), 2.4f);

        for (var i = 0; i < roles.Count; i++)
        {
            var role = roles[i];
            Box($"Row{i}", new Vector2(0, top - i * 0.7f), new Vector2(3.1f, 0.55f), new Color(0.25f, 0.12f, 0.12f, 1f),
                () => Choose(role), 2, role.NiceName);
        }
        Box("Cancel", new Vector2(0, top - roles.Count * 0.7f), new Vector2(3.1f, 0.55f), new Color(0.2f, 0.2f, 0.2f, 1f),
            Close, 2, "Cancel");
    }

    static void Choose(RoleBehaviour role)
    {
        Close();
        var me = PlayerControl.LocalPlayer;
        if (MutatorState.UsesLeft(me) <= 0) return;
        MutatorRpc.RpcMutate(me, (ushort)role.Role);   // no animation or sound: no visual tell
    }

    public static void Close() { if (_root != null) UnityEngine.Object.Destroy(_root); _root = null; }

    // ---- tiny UI helpers ----
    static Sprite _white;
    static Sprite White()
    {
        if (_white != null) return _white;
        var t = new Texture2D(1, 1); t.SetPixel(0, 0, Color.white); t.Apply();
        return _white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    static void Box(string name, Vector2 pos, Vector2 size, Color color, Action onClick, int order, string text = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_root.transform, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, -order);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = White(); sr.color = color; sr.sortingOrder = 200 + order;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        if (text != null)
        {
            Label(text, new Vector3(pos.x, pos.y, -order - 1), 2.2f);
        }
        if (onClick == null) return;
        var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
        var btn = go.AddComponent<PassiveButton>();
        btn.Colliders = new Collider2D[] { col };
        btn.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        btn.OnClick.AddListener((UnityAction)onClick);
        btn.OnMouseOver = new UnityEvent(); btn.OnMouseOut = new UnityEvent();
    }

    static TextMeshPro Label(string text, Vector3 pos, float size)
    {
        // Clone an existing HUD label so the font and material match the game.
        var t = UnityEngine.Object.Instantiate(HudManager.Instance.KillButton.buttonLabelText, _root.transform);
        t.transform.localPosition = pos; t.transform.localScale = Vector3.one * 0.5f;
        t.text = text; t.fontSize = size; t.alignment = TextAlignmentOptions.Center;
        t.sortingOrder = 300;
        return t;
    }
}
