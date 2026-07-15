using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warashibe.Game
{
    public enum ButtonVariant { Ai, Shu, Ghost }

    /// <summary>
    /// Code-generated uGUI + TextMeshPro UI kit (T-U06). Every visual value comes from
    /// <see cref="DesignTokens"/> (docs/04). Placeholder art = emoji / TMP text (no prefabs).
    /// Rounded shapes are generated procedurally so they work in builds (no Editor-only sprites).
    /// </summary>
    public static class UiKit
    {
        // ---- rounded-rect sprite generation (9-sliced, build-safe) ----
        static readonly Dictionary<int, Sprite> RoundedCache = new Dictionary<int, Sprite>();

        public static Sprite RoundedSprite(int radius)
        {
            radius = Mathf.Max(1, radius);
            if (RoundedCache.TryGetValue(radius, out var cached) && cached != null) return cached;
            var sprite = GenerateRounded(radius);
            RoundedCache[radius] = sprite;
            return sprite;
        }

        static Sprite GenerateRounded(int r)
        {
            int size = r * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float cx = fx < r ? r : (fx > size - r ? size - r : fx);
                    float cy = fy < r ? r : (fy > size - r ? size - r : fy);
                    float d = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                    byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        // ---- primitives ----
        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Panel(Transform parent, Color color, int radius, string name = "Panel")
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RoundedSprite(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string content, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, string name = "Text")
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = content;                          // default font = BIZ UDGothic (TMP Settings)
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        // ---- components (docs/04 §3) ----

        /// <summary>Manju (pill) button. variant: Ai / Shu / Ghost. docs/04 §3 ManjuButton.</summary>
        public static GameObject ManjuButton(Transform parent, string label, ButtonVariant variant, bool disabled = false)
        {
            var rt = NewRect("ManjuButton", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RoundedSprite(Mathf.RoundToInt(DesignTokens.TapMin / 2f)); // pill
            img.type = Image.Type.Sliced;

            Color bg, fg;
            switch (variant)
            {
                case ButtonVariant.Shu: bg = DesignTokens.Shu; fg = DesignTokens.White; break;
                case ButtonVariant.Ghost: bg = DesignTokens.White; fg = DesignTokens.Ai; break;
                default: bg = DesignTokens.Ai; fg = DesignTokens.White; break;
            }
            if (disabled) { bg = DesignTokens.Disabled; fg = DesignTokens.White; }
            img.color = bg;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.interactable = !disabled;
            btn.targetGraphic = img;

            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)DesignTokens.Sp3, (int)DesignTokens.Sp3, (int)DesignTokens.Sp1, (int)DesignTokens.Sp1);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = DesignTokens.TapMin;
            le.minWidth = DesignTokens.TapMin * 2f;

            Text(rt, label, DesignTokens.FsBtn, fg, TextAlignmentOptions.Center, "Label");
            return rt.gameObject;
        }

        /// <summary>Dialogue box with a wooden NameTag and up to a couple of lines. docs/04 §3 DialogueBox/NameTag.</summary>
        public static GameObject DialogueBox(Transform parent, string speaker, IEnumerable<string> lines)
        {
            var box = Panel(parent, DesignTokens.White, (int)DesignTokens.Radius, "DialogueBox").gameObject;
            var v = box.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset((int)DesignTokens.Sp2, (int)DesignTokens.Sp2, (int)DesignTokens.Sp2, (int)DesignTokens.Sp2);
            v.spacing = DesignTokens.Sp1;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;   // dialogue lines fill the box width and wrap
            v.childForceExpandHeight = false;

            // NameTag on its own full-width row so the tag hugs its content on the left (wood-ish label).
            var row = NewRect("NameRow", box.transform);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;

            var tag = Panel(row, DesignTokens.Ink, (int)DesignTokens.Sp1, "NameTag").gameObject;
            var th = tag.AddComponent<HorizontalLayoutGroup>();
            th.padding = new RectOffset((int)DesignTokens.Sp2, (int)DesignTokens.Sp2, 4, 4);
            th.childControlWidth = th.childControlHeight = true;
            Text(tag.transform, speaker, DesignTokens.FsSerifName, DesignTokens.White, TextAlignmentOptions.Left, "Name");

            var body = string.Join("\n", lines ?? new string[0]);
            var line = Text(box.transform, body, DesignTokens.FsBody, DesignTokens.Ink, TextAlignmentOptions.TopLeft, "Lines");
            line.lineSpacing = (DesignTokens.LineHeight - 1f) * 100f;
            return box;
        }

        /// <summary>3-column inventory grid: 96px frames, 64px emoji + ruby name. docs/04 §S4.</summary>
        public static GameObject InventoryGrid(Transform parent, IEnumerable<(string emoji, string name)> items)
        {
            var grid = Panel(parent, DesignTokens.Bg, (int)DesignTokens.Radius, "InventoryGrid").gameObject;
            var g = grid.AddComponent<GridLayoutGroup>();
            g.padding = new RectOffset((int)DesignTokens.Sp2, (int)DesignTokens.Sp2, (int)DesignTokens.Sp2, (int)DesignTokens.Sp2);
            g.cellSize = new Vector2(96f, 96f);
            g.spacing = new Vector2(DesignTokens.Sp1, DesignTokens.Sp1);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;

            foreach (var (emoji, name) in items)
            {
                var cell = Panel(grid.transform, DesignTokens.White, (int)DesignTokens.Radius, "Cell").gameObject;
                var cv = cell.AddComponent<VerticalLayoutGroup>();
                cv.childAlignment = TextAnchor.MiddleCenter;
                cv.childControlWidth = cv.childControlHeight = true;
                Text(cell.transform, emoji, 64f, DesignTokens.Ink, TextAlignmentOptions.Center, "Icon");
                Text(cell.transform, name, DesignTokens.FsSmall, DesignTokens.Ink, TextAlignmentOptions.Center, "Name");
            }
            return grid;
        }

        /// <summary>Value stairs: emoji for owned items, "＿" for future, goal always shown. docs/04 §S2.</summary>
        public static GameObject StairProgress(Transform parent, IReadOnlyList<string> owned, int totalSteps, string goalEmoji)
        {
            var bar = Panel(parent, DesignTokens.White, (int)DesignTokens.Radius, "StairProgress").gameObject;
            var h = bar.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset((int)DesignTokens.Sp2, (int)DesignTokens.Sp2, 0, 0);
            h.spacing = 4f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            var barLe = bar.AddComponent<LayoutElement>();
            barLe.minHeight = barLe.preferredHeight = 88f;

            for (int i = 0; i < totalSteps; i++)
            {
                if (i > 0) Text(bar.transform, "→", DesignTokens.FsBody, DesignTokens.Disabled, TextAlignmentOptions.Center, "Arrow");
                string glyph = i < owned.Count ? owned[i] : "＿";
                Text(bar.transform, glyph, 32f, DesignTokens.Wakakusa, TextAlignmentOptions.Center, "Step");
            }
            Text(bar.transform, "→", DesignTokens.FsBody, DesignTokens.Disabled, TextAlignmentOptions.Center, "Arrow");
            Text(bar.transform, goalEmoji, 32f, DesignTokens.Gold, TextAlignmentOptions.Center, "Goal");
            return bar;
        }

        /// <summary>Bun's speech bubble (bottom-right companion). docs/04 §3 BunBubble.</summary>
        public static GameObject BunBubble(Transform parent, string text)
        {
            var bubble = Panel(parent, DesignTokens.White, (int)DesignTokens.Radius, "BunBubble").gameObject;
            var h = bubble.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset((int)DesignTokens.Sp2, (int)DesignTokens.Sp2, (int)DesignTokens.Sp1, (int)DesignTokens.Sp1);
            h.spacing = DesignTokens.Sp1;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            bubble.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Text(bubble.transform, "🐝", 28f, DesignTokens.Gold, TextAlignmentOptions.Center, "Bun");
            Text(bubble.transform, text, DesignTokens.FsSmall, DesignTokens.Ink, TextAlignmentOptions.Left, "Say");
            return bubble;
        }
    }
}
