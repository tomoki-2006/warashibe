using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warashibe.Core;

namespace Warashibe.Game
{
    /// <summary>
    /// Map / 絵巻 (docs/01 §6, docs/04 §S2). The whole route is shown as a row of stop nodes with the
    /// value stairs beneath. Reachable stops (cleared + the next one) are tappable; the rest are misted
    /// (靄, disabled). Tapping walks the traveler there (2.0s, tap-to-skip 0.5s → 即到着) then enters.
    /// View + input only; GameFlow owns progression.
    /// </summary>
    public sealed class MapScreen : MonoBehaviour
    {
        public static bool AnimateFx = true; // false = no walk animation (headless capture)

        const float WalkDur = 2.0f;      // docs/01 §6 移動演出
        const float WalkSkipDur = 0.5f;  // docs/01 §6 スキップ

        // Placeholder stop icons (docs/04 §S2 example art); "📍" if a route has more stops than this.
        static readonly string[] StopIcons = { "🏔️", "⛩️", "🍵", "🏘️", "⚓", "🏮", "🏯" };

        GameSession _session;
        int _reachableMax;
        int _current;
        Action<int> _onStopSelected;

        Transform _row;
        Transform _canvas;
        RectTransform _marker;
        readonly List<RectTransform> _nodes = new List<RectTransform>();
        bool _walking;
        bool _skip;

        public void Begin(GameSession session, int reachableMax, int current, Action<int> onStopSelected)
        {
            _session = session;
            _reachableMax = reachableMax;
            _current = current;
            _onStopSelected = onStopSelected;
            Build();
        }

        void Build()
        {
            var canvas = UiKit.ScreenCanvas("MapCanvas");
            canvas.transform.SetParent(transform, false); // tie canvas to this screen's lifetime (T-U12)
            _canvas = canvas.transform;
            UiKit.FullscreenBg(_canvas, DesignTokens.Bg);

            var rowGo = new GameObject("StopRow", typeof(RectTransform));
            rowGo.transform.SetParent(_canvas, false);
            var rowRt = (RectTransform)rowGo.transform;
            rowRt.anchorMin = rowRt.anchorMax = rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, DesignTokens.Sp4);
            var h = rowGo.AddComponent<HorizontalLayoutGroup>();
            h.spacing = DesignTokens.Sp1;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleCenter;
            rowGo.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _row = rowGo.transform;

            var stops = _session.Content.Route.Stops;
            for (int i = 0; i < stops.Length; i++)
            {
                int idx = i;
                bool reachable = i <= _reachableMax;
                bool cleared = _session.IsStopCleared(stops[i]);
                var node = BuildNode(_row, i, reachable, cleared);
                _nodes.Add(node);
                if (reachable)
                {
                    var btn = node.GetComponent<Button>();
                    if (btn != null) btn.onClick.AddListener(() => OnTap(idx));
                }
            }

            _marker = MakeMarker(_row);
            BuildStairs(_canvas);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rowRt);
            PlaceMarker(_current);
        }

        RectTransform BuildNode(Transform parent, int i, bool reachable, bool cleared)
        {
            var icon = i < StopIcons.Length ? StopIcons[i] : "📍";
            // reachable → tappable manju; unreachable → disabled (= 靄)
            var go = UiKit.ManjuButton(parent, icon, ButtonVariant.Ghost, disabled: !reachable);
            var le = go.GetComponent<LayoutElement>();
            le.minWidth = DesignTokens.TapMin;
            if (cleared)
                UiKit.Text(go.transform, "✓", DesignTokens.FsSmall, DesignTokens.Wakakusa,
                    TextAlignmentOptions.BottomRight, "Done");
            return (RectTransform)go.transform;
        }

        RectTransform MakeMarker(Transform parent)
        {
            var go = new GameObject("Marker", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            rt.sizeDelta = new Vector2(72f, 40f);
            UiKit.Text(rt, "🚶🐝", 28f, DesignTokens.Ink, TextAlignmentOptions.Center, "Traveler");
            return rt;
        }

        // Use localPosition (not anchoredPosition): the nodes are positioned by a layout group, so
        // their anchoredPosition depends on their anchors — localPosition is the reliable seam.
        Vector3 NodeMarkerPos(int index) =>
            _nodes[index].localPosition + new Vector3(0f, DesignTokens.Sp4, 0f);

        void PlaceMarker(int index)
        {
            if (index >= 0 && index < _nodes.Count) _marker.localPosition = NodeMarkerPos(index);
        }

        void OnTap(int target)
        {
            if (_walking) return;
            _walking = true;
            if (AnimateFx) StartCoroutine(WalkTo(target));
            else Arrive(target);
        }

        IEnumerator WalkTo(int target)
        {
            var skipBtn = FullscreenSkip();
            var from = _marker.localPosition;
            var to = NodeMarkerPos(target);
            float dur = WalkDur;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                if (_skip) dur = Mathf.Min(dur, WalkSkipDur); // tap-to-skip: shorten remaining (docs/01 §6)
                _marker.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            _marker.localPosition = to;
            if (skipBtn != null) Destroy(skipBtn);
            Arrive(target);
        }

        GameObject FullscreenSkip()
        {
            var go = new GameObject("Skip", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_canvas, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); // invisible catcher
            go.GetComponent<Button>().transition = Selectable.Transition.None;
            go.GetComponent<Button>().onClick.AddListener(() => _skip = true);
            return go;
        }

        void Arrive(int target)
        {
            _current = target;
            PlaceMarker(target);
            _onStopSelected?.Invoke(target);
        }

        void BuildStairs(Transform canvas)
        {
            var holder = new GameObject("Stairs", typeof(RectTransform));
            holder.transform.SetParent(canvas, false);
            var rt = (RectTransform)holder.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(DesignTokens.Sp3, DesignTokens.Sp3);
            rt.offsetMax = new Vector2(-DesignTokens.Sp3, DesignTokens.Sp4 * 2.5f);
            var v = holder.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childAlignment = TextAnchor.LowerCenter;

            string goal = _session.Content.Items.TryGetValue(_session.Content.Route.GoalItem, out var g)
                ? g.Emoji : "🏠";
            UiKit.StairProgress(holder.transform, _session.ChainEmojisOwned(), _session.ChainTotalSteps(), goal);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
