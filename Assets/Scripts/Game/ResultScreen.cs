using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warashibe.Core;

namespace Warashibe.Game
{
    /// <summary>
    /// Result / リザルト (docs/01 §1 RouteClear, §10 結果カード, docs/04 §S7). Shows the growth chain,
    /// the 目利き rank noren (color by rank), a result-card preview, and もういちど / タイトルへ. The
    /// "みんなにみせる" share button is parent-gated OFF by default (v3.2) → omitted here (a later
    /// ticket wires the OS share sheet). View + input only.
    /// </summary>
    public sealed class ResultScreen : MonoBehaviour
    {
        public static bool AnimateFx = true;

        GameSession _session;
        Action _onReplay, _onTitle;

        public void Begin(GameSession session, Action onReplay, Action onTitle)
        {
            _session = session;
            _onReplay = onReplay;
            _onTitle = onTitle;
            Build();
        }

        void Build()
        {
            var canvas = UiKit.ScreenCanvas("ResultCanvas");
            canvas.transform.SetParent(transform, false); // tie canvas to this screen's lifetime (T-U12)
            UiKit.FullscreenBg(canvas.transform, DesignTokens.Bg);

            var col = Column(canvas.transform);
            UiKit.Text(col, Str("clear_title"), DesignTokens.FsTitle, DesignTokens.Ai, TextAlignmentOptions.Center, "Title");

            BuildCard(col);

            var row = Row(col);
            var replay = UiKit.ManjuButton(row, Str("btn_replay"), ButtonVariant.Shu);
            replay.GetComponent<Button>().onClick.AddListener(() => _onReplay?.Invoke());
            var title = UiKit.ManjuButton(row, "🏠", ButtonVariant.Ghost); // back to title (home glyph)
            title.GetComponent<Button>().onClick.AddListener(() => _onTitle?.Invoke());

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)col);
        }

        void BuildCard(Transform parent)
        {
            var card = UiKit.Panel(parent, DesignTokens.White, (int)DesignTokens.Radius, "ResultCard").gameObject;
            var v = card.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset((int)DesignTokens.Sp3, (int)DesignTokens.Sp3, (int)DesignTokens.Sp3, (int)DesignTokens.Sp3);
            v.spacing = DesignTokens.Sp2;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childAlignment = TextAnchor.MiddleCenter;
            card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // route name
            UiKit.Text(card.transform, _session.Content.Route.Title, DesignTokens.FsSerifName, DesignTokens.Ink,
                TextAlignmentOptions.Center, "RouteName");

            // growth chain (all steps filled at RouteClear)
            string goal = _session.Content.Items.TryGetValue(_session.Content.Route.GoalItem, out var g) ? g.Emoji : "🏠";
            UiKit.StairProgress(card.transform, _session.ChainEmojisOwned(), _session.ChainTotalSteps(), goal);

            // 目利き rank noren (color by rank)
            var (rankColor, rankKey) = RankStyle(_session.Rank());
            var noren = UiKit.Panel(card.transform, rankColor, (int)DesignTokens.Sp1, "Noren").gameObject;
            var nh = noren.AddComponent<HorizontalLayoutGroup>();
            nh.padding = new RectOffset((int)DesignTokens.Sp3, (int)DesignTokens.Sp3, (int)DesignTokens.Sp1, (int)DesignTokens.Sp1);
            nh.childAlignment = TextAnchor.MiddleCenter;
            nh.childControlWidth = nh.childControlHeight = true;
            noren.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            UiKit.Text(noren.transform, Str(rankKey), DesignTokens.FsSerifName, DesignTokens.White,
                TextAlignmentOptions.Center, "Rank");

            // trade count + ぶん
            UiKit.Text(card.transform, "🤝 " + TradeCount() + "   🐝", DesignTokens.FsBody, DesignTokens.Disabled,
                TextAlignmentOptions.Center, "Meta");

            if (AnimateFx) StartCoroutine(PopIn(noren.GetComponent<RectTransform>()));
        }

        (Color, string) RankStyle(RouteRank r)
        {
            switch (r)
            {
                case RouteRank.Choja: return (DesignTokens.Gold, "rank_choja");
                case RouteRank.Daishonin: return (DesignTokens.Shu, "rank_daishonin");
                case RouteRank.Gyoshonin: return (DesignTokens.Ai, "rank_gyoshonin");
                default: return (DesignTokens.Wakakusa, "rank_minarai");
            }
        }

        int TradeCount()
        {
            int n = 0;
            foreach (var loc in _session.Content.Route.Stops)
                if (_session.IsStopCleared(loc) && _session.TradingNpc(_session.Content.Stops[loc]) != null) n++;
            return n;
        }

        static IEnumerator PopIn(RectTransform rt)
        {
            float d = DesignTokens.DurStairRise;
            rt.localScale = Vector3.zero;
            for (float t = 0f; t < d; t += Time.deltaTime)
            {
                float s = Mathf.SmoothStep(0f, 1f, t / d);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        string Str(string key) => _session.Content.Strings.TryGetValue(key, out var v) ? v : key;

        static Transform Column(Transform parent)
        {
            var go = new GameObject("Column", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(DesignTokens.Sp3, DesignTokens.Sp3);
            rt.offsetMax = new Vector2(-DesignTokens.Sp3, -DesignTokens.Sp3);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = DesignTokens.Sp3;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.MiddleCenter;
            return go.transform;
        }

        static Transform Row(Transform parent)
        {
            var go = new GameObject("Buttons", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = DesignTokens.Sp2;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleCenter;
            return go.transform;
        }
    }
}
