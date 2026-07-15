using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warashibe.Core;

namespace Warashibe.Game
{
    /// <summary>
    /// T-U06 demo bootstrap. On Play it loads the real kibi-01 content (a runtime check of the
    /// T-U05 loader/validator) and renders every UiKit component in one screen for visual review.
    /// All user-facing text comes from the content JSON — no Japanese literals in code.
    /// </summary>
    public sealed class UiKitGallery : MonoBehaviour
    {
        void Start()
        {
            var content = StreamingContentLoader.LoadRoute();
            Debug.Log($"[UiKitGallery] kibi-01 loaded: {content.Stops.Count} stops / {content.Npcs.Count} npcs / {content.Items.Count} items / {content.Recipes.Length} recipe(s)");

            var canvas = BuildCanvas();
            BuildBackground(canvas.transform);

            var root = NewColumn(canvas.transform, "Root", DesignTokens.Sp3);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.offsetMin = new Vector2(DesignTokens.Sp3, DesignTokens.Sp3);
            rootRt.offsetMax = new Vector2(-DesignTokens.Sp3, -DesignTokens.Sp3);
            var rootLayout = root.GetComponent<VerticalLayoutGroup>();
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childAlignment = TextAnchor.UpperLeft;

            UiKit.Text(root.transform, "Warashibe UI Kit — T-U06", DesignTokens.FsTitle, DesignTokens.Ai, TextAlignmentOptions.Left, "Title");

            // ManjuButton (Ai / Shu / Ghost / disabled) — labels from strings.ja.json
            Caption(root.transform, "ManjuButton");
            var row = NewRow(root.transform, "Buttons", DesignTokens.Sp2);
            UiKit.ManjuButton(row.transform, S(content, "btn_talk"), ButtonVariant.Ai);
            UiKit.ManjuButton(row.transform, S(content, "btn_offer"), ButtonVariant.Shu);
            UiKit.ManjuButton(row.transform, S(content, "btn_ask"), ButtonVariant.Ghost);
            UiKit.ManjuButton(row.transform, S(content, "btn_combine"), ButtonVariant.Ai, disabled: true);

            // DialogueBox — a real NPC's name + intro lines
            Caption(root.transform, "DialogueBox");
            var npc = content.Npcs.TryGetValue("npc_child", out var c) ? c : content.Npcs.Values.First();
            UiKit.DialogueBox(root.transform, npc.Name, npc.Intro);

            // InventoryGrid — first items (emoji + ruby name) from items.json
            Caption(root.transform, "InventoryGrid");
            var items = content.Items.Values.Take(6).Select(it => (it.Emoji, it.NameRuby));
            UiKit.InventoryGrid(root.transform, items);

            // StairProgress — value stairs with emoji chain
            Caption(root.transform, "StairProgress");
            var owned = ChainEmojis(content, new[] { "item_wara", "item_abumushi_toy", "item_kibidango" });
            string goal = content.Items.TryGetValue(content.Route.GoalItem, out var g) ? g.Emoji : "🏠";
            UiKit.StairProgress(root.transform, owned, 7, goal);

            // BunBubble — bottom-right, text from strings.ja.json
            var bun = UiKit.BunBubble(canvas.transform, S(content, "nudge_bun"));
            var bunRt = (RectTransform)bun.transform;
            bunRt.anchorMin = bunRt.anchorMax = new Vector2(1f, 0f);
            bunRt.pivot = new Vector2(1f, 0f);
            bunRt.anchoredPosition = new Vector2(-DesignTokens.Sp3, DesignTokens.Sp3);

            // Nested layout + wrapping TMP needs a forced pass so per-item heights settle
            // (otherwise a 2-line dialogue box is allocated 1 line and siblings overlap).
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRt);
        }

        static string S(LoadedContent c, string key) => c.Strings.TryGetValue(key, out var v) ? v : key;

        static List<string> ChainEmojis(LoadedContent c, IEnumerable<string> ids) =>
            ids.Select(id => c.Items.TryGetValue(id, out var it) ? it.Emoji : "?").ToList();

        Canvas BuildCanvas()
        {
            var go = new GameObject("UiKitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        void BuildBackground(Transform parent)
        {
            var go = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = DesignTokens.Bg;
            img.raycastTarget = false;
        }

        static GameObject NewColumn(Transform parent, string name, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            return go;
        }

        static GameObject NewRow(Transform parent, string name, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            return go;
        }

        static void Caption(Transform parent, string text) =>
            UiKit.Text(parent, text, DesignTokens.FsSmall, DesignTokens.Disabled, TextAlignmentOptions.Left, "Caption");
    }
}
