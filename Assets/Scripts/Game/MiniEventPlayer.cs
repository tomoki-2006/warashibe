using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warashibe.Core;
using Event = Warashibe.Core.Event; // disambiguate from UnityEngine.Event

namespace Warashibe.Game
{
    /// <summary>
    /// T-U09 — plays the three docs/03 §7 mini-events on a fullscreen overlay, then calls back:
    ///   • tap_catch  (アブ捕獲, docs/01 §8): a forgiving figure-8 tap game — no fail, no timer.
    ///   • map_choice (馬の世話): prompt + choices, retry-until-correct.
    ///   • cutscene   (舟): a short auto-scroll, skippable after a grace window.
    /// All completion effects (gives / replace_item / advance_to) run through Core via
    /// <see cref="GameSession.ApplyEvent"/>; this class is view + input only, and holds no Japanese
    /// literals (every line comes from the Event content).
    ///
    /// <see cref="AnimateFx"/> mirrors <see cref="EncounterScreen.AnimateFx"/>: set false for a
    /// deterministic/headless capture (no frame-timed flight/scroll; drive via the Debug* methods).
    /// </summary>
    public sealed class MiniEventPlayer : MonoBehaviour
    {
        public static bool AnimateFx = true;

        // Compositing / motion tuning (not docs/04 tokens): a dim scrim and a slow 8-figure.
        const float ScrimAlpha = 0.55f;      // backdrop dim over the encounter
        const float SlowAngular = 1.1f;      // base figure-8 angular speed (rad/s) — "ゆっくり" (§8)
        const float FieldAmpX = 0.34f;       // 8-figure half-width as a fraction of the field
        const float FieldAmpY = 0.30f;       // 8-figure half-height as a fraction of the field
        const float AbuEmoji = 72f;          // catch-target glyph size
        const float BoatEmoji = 64f;         // cutscene boat glyph size

        GameSession _session;
        Event _event;
        Action _onComplete;

        Transform _layer;    // fullscreen root this player owns (destroyed on Complete)
        Transform _field;    // free-positioned layer (abu / boat)
        Transform _bottom;   // bottom content column (pips / prompt / choices / result / buttons)
        bool _done;

        // tap_catch state
        int _taps;
        float _speed;
        RectTransform _abu;
        Coroutine _flight;
        readonly List<TextMeshProUGUI> _pips = new List<TextMeshProUGUI>();

        // ---- entry ----

        /// <summary>Play <paramref name="ev"/> under <paramref name="parent"/> (the game canvas),
        /// invoking <paramref name="onComplete"/> after its effects are applied.</summary>
        public void Play(GameSession session, Event ev, Transform parent, Action onComplete)
        {
            _session = session;
            _event = ev;
            _onComplete = onComplete;
            _done = false;

            BuildLayer(parent);
            switch (ev.Kind)
            {
                case EventKind.TapCatch: BuildTapCatch(); break;
                case EventKind.MapChoice: RenderChoices(); break;
                case EventKind.Cutscene: BuildCutscene(); break;
                default: Complete(); break; // unknown kind: pass straight through
            }
        }

        // ---- shared completion ----

        void Complete()
        {
            if (_done) return;
            _done = true;
            if (_flight != null) StopCoroutine(_flight);
            _session?.ApplyEvent(_event);       // gives / replace_item / advance_to through Core
            if (_layer != null) Destroy(_layer.gameObject);
            _onComplete?.Invoke();
        }

        // Clear the bottom column, show the given lines, then a ▶ button that completes.
        void ShowResultThenComplete(IEnumerable<string> lines)
        {
            if (_flight != null) { StopCoroutine(_flight); _flight = null; }
            if (_abu != null) { Destroy(_abu.gameObject); _abu = null; }
            ClearBottom();
            ResultPanel(_bottom, lines);
            var row = BottomRow("Continue");
            ArrowButton(row, "▶", Complete);
            Rebuild();
        }

        // ---- tap_catch (docs/01 §8) ----

        void BuildTapCatch()
        {
            _taps = 0;
            _speed = 1f;

            // Progress pips (language-free): one per required tap, filled shu as the abu is worn down.
            // Empty pips are white so they read on the dim scrim; filled ones turn shu.
            _pips.Clear();
            var pipRow = TopRow("Pips");
            int need = Mathf.Max(1, _event.Spec != null ? _event.Spec.TapsRequired : 1);
            for (int i = 0; i < need; i++)
                _pips.Add(UiKit.Text(pipRow, "○", DesignTokens.FsTitle, DesignTokens.White,
                    TextAlignmentOptions.Center, "Pip"));

            // The abu: a 2×-scaled invisible hitbox (docs/01 §8 「当たり判定はアブ絵の2倍」) with the glyph.
            float scale = _event.Spec != null && _event.Spec.HitboxScale > 0f ? _event.Spec.HitboxScale : 2f;
            var go = new GameObject("Abu", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_field, false);
            _abu = (RectTransform)go.transform;
            _abu.sizeDelta = new Vector2(AbuEmoji * scale, AbuEmoji * scale);
            var hit = go.GetComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f); // invisible but still a raycast target
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(RegisterTap);
            UiKit.Text(_abu, GlyphFor(_event.Gives, "🪰"), AbuEmoji, DesignTokens.Ink,
                TextAlignmentOptions.Center, "Glyph");

            if (AnimateFx) _flight = StartCoroutine(Flight());
            else _abu.anchoredPosition = Vector2.zero; // static, centred, for capture
        }

        IEnumerator Flight()
        {
            float phase = 0f;
            var fieldRt = (RectTransform)_field;
            while (!_done && _abu != null)
            {
                phase += Time.deltaTime * SlowAngular * Mathf.Max(0.05f, _speed);
                float ampX = fieldRt.rect.width * 0.5f * FieldAmpX;
                float ampY = fieldRt.rect.height * 0.5f * FieldAmpY;
                // Lissajous 8-figure: x = sin(2θ), y = sin(θ).
                _abu.anchoredPosition = new Vector2(Mathf.Sin(phase * 2f) * ampX, Mathf.Sin(phase) * ampY);
                yield return null;
            }
        }

        public void RegisterTap()
        {
            if (_done || _abu == null) return;
            _taps++;
            float slow = _event.Spec != null ? _event.Spec.SlowdownPerTap : 0f;
            _speed *= Mathf.Max(0f, 1f - slow);            // decelerate each tap (docs/01 §8)
            for (int i = 0; i < _pips.Count; i++)
            {
                bool on = i < _taps;
                _pips[i].text = on ? "●" : "○";
                _pips[i].color = on ? DesignTokens.Shu : DesignTokens.White;
            }
            int need = Mathf.Max(1, _event.Spec != null ? _event.Spec.TapsRequired : 1);
            if (_taps >= need) ShowResultThenComplete(_event.LinesOnSuccess ?? Array.Empty<string>());
        }

        // ---- map_choice ----

        void RenderChoices()
        {
            ClearBottom();
            var spec = _event.Spec;
            if (spec != null && !string.IsNullOrEmpty(spec.Prompt))
                ResultPanel(_bottom, new[] { spec.Prompt });

            var row = BottomRow("Choices");
            var choices = spec?.Choices ?? Array.Empty<EventChoice>();
            for (int i = 0; i < choices.Length; i++)
            {
                int idx = i;
                MenuButton(row, choices[i].Label, ButtonVariant.Ai, () => SelectChoice(idx));
            }
            Rebuild();
        }

        public void SelectChoice(int index)
        {
            if (_done) return;
            var choices = _event.Spec?.Choices ?? Array.Empty<EventChoice>();
            if (index < 0 || index >= choices.Length) return;
            var choice = choices[index];

            if (choice.Correct)
            {
                var lines = new List<string>();
                if (!string.IsNullOrEmpty(choice.Result)) lines.Add(choice.Result);
                if (_event.OnComplete?.Lines != null) lines.AddRange(_event.OnComplete.Lines);
                ShowResultThenComplete(lines);
                return;
            }

            // Wrong choice: show its result (carries ぶんの nudge in-line) and retry (docs/03 §7).
            bool retry = _event.Spec != null && _event.Spec.RetryUntilCorrect;
            ClearBottom();
            if (!string.IsNullOrEmpty(choice.Result)) ResultPanel(_bottom, new[] { choice.Result });
            var row = BottomRow("Retry");
            if (retry) ArrowButton(row, "▶", RenderChoices);
            else ArrowButton(row, "▶", Complete);
            Rebuild();
        }

        // ---- cutscene ----

        void BuildCutscene()
        {
            var spec = _event.Spec;

            // Sea band across the middle of the field, with the boat gliding across it.
            var band = UiKit.Panel(_field, DesignTokens.Ai, (int)DesignTokens.Radius, "Sea");
            var bandRt = (RectTransform)band.transform;
            bandRt.anchorMin = new Vector2(0f, 0.5f);
            bandRt.anchorMax = new Vector2(1f, 0.5f);
            bandRt.pivot = new Vector2(0.5f, 0.5f);
            bandRt.sizeDelta = new Vector2(0f, DesignTokens.Sp4 * 3f);
            bandRt.anchoredPosition = Vector2.zero;

            var boat = UiKit.Text(_field, "🛶", BoatEmoji, DesignTokens.White, TextAlignmentOptions.Center, "Boat");
            var boatRt = boat.rectTransform;
            boatRt.anchorMin = boatRt.anchorMax = boatRt.pivot = new Vector2(0.5f, 0.5f);
            boatRt.sizeDelta = new Vector2(BoatEmoji * 1.6f, BoatEmoji * 1.6f);

            if (spec != null && !string.IsNullOrEmpty(spec.Line))
                ResultPanel(_bottom, new[] { spec.Line });

            float travel = ((RectTransform)_field).rect.width * 0.5f;
            if (AnimateFx)
            {
                boatRt.anchoredPosition = new Vector2(-travel, 0f);
                StartCoroutine(Cutscene(boatRt, travel));
            }
            else
            {
                boatRt.anchoredPosition = new Vector2(travel * 0.8f, 0f); // near the far shore, for capture
                ShowSkip();
            }
        }

        IEnumerator Cutscene(RectTransform boat, float travel)
        {
            var spec = _event.Spec;
            float dur = Mathf.Max(0.1f, (spec != null ? spec.DurationMs : 3000) / 1000f);
            float skipAt = (spec != null ? spec.SkippableAfterMs : 0) / 1000f;
            bool skipShown = false;
            for (float t = 0f; t < dur && !_done; t += Time.deltaTime)
            {
                boat.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, t / dur), 0f);
                if (!skipShown && t >= skipAt) { ShowSkip(); skipShown = true; }
                yield return null;
            }
            if (!_done) Complete();
        }

        void ShowSkip()
        {
            var row = BottomRow("Skip");
            ArrowButton(row, "»", Complete);   // skip / advance (docs/03 §7 skippable_after_ms)
            Rebuild();
        }

        // ---- debug drive (headless capture, AnimateFx=false) ----

        public void DebugTap() => RegisterTap();
        public void DebugSelect(int index) => SelectChoice(index);
        public void DebugComplete() => Complete();

        // ---- layer + builders ----

        void BuildLayer(Transform parent)
        {
            var go = new GameObject("MiniEventLayer", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            _layer = go.transform;
            var rt = (RectTransform)_layer;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var scrim = go.GetComponent<Image>();
            var ink = DesignTokens.Ink;
            scrim.color = new Color(ink.r, ink.g, ink.b, ScrimAlpha); // dim + block underlying taps

            _field = NewFullRect("Field", _layer);

            var bottom = new GameObject("Bottom", typeof(RectTransform));
            bottom.transform.SetParent(_layer, false);
            _bottom = bottom.transform;
            var brt = (RectTransform)_bottom;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.offsetMin = new Vector2(DesignTokens.Sp3, DesignTokens.Sp3);
            brt.offsetMax = new Vector2(-DesignTokens.Sp3, DesignTokens.Sp4 * 4f);
            var v = bottom.AddComponent<VerticalLayoutGroup>();
            v.spacing = DesignTokens.Sp2;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.LowerCenter;
        }

        static Transform NewFullRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go.transform;
        }

        Transform TopRow(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_layer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -DesignTokens.Sp4 * 2f);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = DesignTokens.Sp2;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.transform;
        }

        Transform BottomRow(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_bottom, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = DesignTokens.Sp2;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleCenter;
            return go.transform;
        }

        void ClearBottom()
        {
            foreach (Transform c in _bottom) Destroy(c.gameObject);
        }

        // A centred narration panel (no name tag — events are not spoken by a single NPC).
        static GameObject ResultPanel(Transform parent, IEnumerable<string> lines)
        {
            var panel = UiKit.Panel(parent, DesignTokens.White, (int)DesignTokens.Radius, "EventLines").gameObject;
            var v = panel.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset((int)DesignTokens.Sp3, (int)DesignTokens.Sp3, (int)DesignTokens.Sp2, (int)DesignTokens.Sp2);
            v.spacing = DesignTokens.Sp1;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childAlignment = TextAnchor.MiddleCenter;
            panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var body = string.Join("\n", (lines ?? Array.Empty<string>()).Where(s => !string.IsNullOrEmpty(s)));
            var t = UiKit.Text(panel.transform, body, DesignTokens.FsBody, DesignTokens.Ink,
                TextAlignmentOptions.Center, "Lines");
            t.lineSpacing = (DesignTokens.LineHeight - 1f) * 100f;
            return panel;
        }

        void MenuButton(Transform parent, string label, ButtonVariant variant, Action onClick)
        {
            var go = UiKit.ManjuButton(parent, label, variant);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
        }

        void ArrowButton(Transform parent, string glyph, Action onClick) =>
            MenuButton(parent, glyph, ButtonVariant.Shu, onClick);

        string GlyphFor(string itemId, string fallback) =>
            !string.IsNullOrEmpty(itemId) && _session != null
            && _session.Content.Items.TryGetValue(itemId, out var it) && !string.IsNullOrEmpty(it.Emoji)
                ? it.Emoji : fallback;

        void Rebuild() => LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_bottom);
    }
}
