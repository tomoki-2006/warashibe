using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warashibe.Core;

namespace Warashibe.Game
{
    /// <summary>
    /// Title (docs/01 §1). はじめる starts a new game. つづきから is shown only when a save exists
    /// (docs/01 §9: no save → the button is hidden). View + input only; GameFlow owns the state.
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        public void Begin(LoadedContent content, bool hasSave, Action onStart, Action onContinue = null)
        {
            var canvas = UiKit.ScreenCanvas("TitleCanvas");
            canvas.transform.SetParent(transform, false); // tie canvas to this screen's lifetime (T-U12)
            UiKit.FullscreenBg(canvas.transform, DesignTokens.Bg);

            var col = Column(canvas.transform);
            UiKit.Text(col, content.Route.Title, DesignTokens.FsTitle, DesignTokens.Ai, TextAlignmentOptions.Center, "Title");
            UiKit.Text(col, "🐝", 64f, DesignTokens.Gold, TextAlignmentOptions.Center, "Bun");

            var start = UiKit.ManjuButton(col, Str(content, "btn_start"), ButtonVariant.Shu);
            start.GetComponent<Button>().onClick.AddListener(() => onStart?.Invoke());

            if (hasSave && onContinue != null)
            {
                var cont = UiKit.ManjuButton(col, Str(content, "btn_continue"), ButtonVariant.Ghost);
                cont.GetComponent<Button>().onClick.AddListener(() => onContinue.Invoke());
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)col);
        }

        static string Str(LoadedContent c, string key) => c.Strings.TryGetValue(key, out var v) ? v : key;

        static Transform Column(Transform parent)
        {
            var go = new GameObject("Column", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(320f, 0f);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = DesignTokens.Sp4;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childAlignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.transform;
        }
    }
}
