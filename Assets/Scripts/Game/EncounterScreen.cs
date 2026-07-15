using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Warashibe.Core;

namespace Warashibe.Game
{
    /// <summary>
    /// T-U07 — a single-NPC encounter wiring together 会話 / 質問 / あわせ技 / 提案 / ヒント梯子
    /// (docs/01 §1 state flow, §2-3 rules). All rules run through Core via <see cref="GameSession"/>;
    /// this class is view + input only. The success 演出 (ValueMeter/StairUp) is deferred to T-U08.
    ///
    /// Drive methods are public so the flow can be exercised from the Editor (buttons call the same).
    /// </summary>
    public sealed class EncounterScreen : MonoBehaviour
    {
        enum Mode { Idle, Questioning, Offering }

        GameSession _session;
        string _npcId;
        Npc Npc => _session.Content.Npcs[_npcId];

        Mode _mode = Mode.Idle;
        List<string> _dialogue = new List<string>();
        string _bun;
        readonly HashSet<int> _usedQuestions = new HashSet<int>();

        Transform _column;   // rebuilt each Render
        Transform _bunSlot;  // bottom-right overlay
        Transform _overlay;  // centre overlay for the value-meter 演出
        bool _cleared;       // this stop has been traded (docs/01 §1 TradeAccept)
        GameObject _meter;   // the built value meter (animated by AcceptFx)

        MiniEventPlayer _miniEvent;                              // ambient mini-event player (T-U09)
        readonly List<Coroutine> _nudges = new List<Coroutine>(); // active tutorial blinks (docs/01 §7)
        GameObject _offerBtn;                                    // captured for the こうかん nudge
        readonly List<GameObject> _combineSlots = new List<GameObject>(); // captured for the あわせ技 nudge

        /// <summary>Set false to render the value meter in its final state without the frame-timed
        /// flash/reveal (headless/deterministic capture). True in normal play.</summary>
        public static bool AnimateFx = true;

        void Start()
        {
            var content = StreamingContentLoader.LoadRoute();

            // Seed a demo state at loc_kibitsu holding only straw — the horsefly is caught in the
            // ambient tap_catch mini-event (T-U09), which then unlocks combine → offer → 演出.
            var save = new SaveData
            {
                RouteId = content.Route.Id,
                StopIndex = 1, // loc_kibitsu
                Inventory = new List<string> { "item_wara" },
            };
            _session = new GameSession(new GameState(save, content));
            _npcId = "npc_child";

            MiniEventPlayer.AnimateFx = AnimateFx; // keep capture mode in sync (docs/13 headless)
            BuildCanvas();
            PlayAmbientOrTalk();
        }

        /// <summary>Play the current stop's ambient mini-event if its reward is not yet held
        /// (docs/03 §7 ambientEvent, docs/01 §8 アブ捕獲), otherwise start the conversation.</summary>
        void PlayAmbientOrTalk()
        {
            var ev = _session.CurrentAmbientEvent;
            bool pending = ev != null && ev.Kind == EventKind.TapCatch
                           && !string.IsNullOrEmpty(ev.Gives) && !_session.Has(ev.Gives);
            if (pending)
            {
                _miniEvent = gameObject.AddComponent<MiniEventPlayer>();
                _miniEvent.Play(_session, ev, _overlay.parent, OnAmbientDone);
            }
            else
            {
                Talk();
            }
        }

        void OnAmbientDone()
        {
            if (_miniEvent != null) { Destroy(_miniEvent); _miniEvent = null; }
            Talk(); // abu now in にもつ — Render's あわせ技 nudge blinks the two combinable slots
        }

        // ---- drive methods (buttons + Editor) ----

        public void Talk()
        {
            _dialogue = Npc.Intro?.ToList() ?? new List<string>();
            _bun = AskNudge();   // しつもん誘導: 初回セリフ後にぶんの一言（docs/01 §7）
            _mode = Mode.Idle;
            Render();
        }

        // Bun's しつもん nudge string while a question is still available and unused (docs/01 §7).
        string AskNudge()
        {
            bool canAsk = Npc.Questions != null && _usedQuestions.Count == 0 && _session.QuestionsUsed < 2;
            return canAsk && !_cleared ? Str("tut_ask_bun") : null;
        }

        public void Ask()
        {
            _mode = Mode.Questioning;
            Render();
        }

        public void SelectQuestion(int index)
        {
            var q = Npc.Questions[index];
            _dialogue = new List<string> { q.Q, q.A };
            _usedQuestions.Add(index);
            _session.UseQuestion();
            _mode = Mode.Idle;
            Render();
        }

        public void OpenOffer()
        {
            _mode = Mode.Offering;
            _bun = null;
            Render();
        }

        public void Offer(string itemId)
        {
            var result = _session.Offer(_npcId, itemId);
            switch (result.Outcome)
            {
                case OfferOutcome.Accept:
                    _dialogue = result.Lines;
                    _bun = null;
                    _mode = Mode.Idle;
                    _cleared = true;
                    Render();
                    BuildMeter(itemId);               // final state built synchronously (docs/04 §S6)
                    if (AnimateFx) StartCoroutine(AcceptFx()); // flash + star reveal (docs/04 §4)
                    return;
                case OfferOutcome.Decline:
                    _dialogue = result.Lines;                    // L{n} decline line
                    _bun = result.HintLevelShown >= 3 ? Npc.HintL3
                         : result.HintLevelShown >= 2 ? Npc.HintL2
                         : null;                                 // L1 hint is in the decline line itself
                    _mode = Mode.Offering;                       // stay so the ladder is easy to walk
                    break;
                case OfferOutcome.Duplicate:
                    _bun = Str("offer_dup_bun");
                    _mode = Mode.Offering;
                    break;
            }
            Render();
        }

        public void TryCombine()
        {
            var inv = _session.Save.Inventory;
            for (int i = 0; i < inv.Count; i++)
                for (int j = i + 1; j < inv.Count; j++)
                    if (Recipes.Combine(inv[i], inv[j], _session.Content.Recipes) != null)
                    {
                        _session.Combine(inv[i], inv[j]);
                        _bun = null;
                        _mode = Mode.Idle;
                        Render();
                        return;
                    }
            _bun = Str("combine_fail_bun");
            Render();
        }

        // ---- rendering ----

        void Render()
        {
            StopNudges();
            _offerBtn = null;
            _combineSlots.Clear();
            foreach (Transform c in _column) Destroy(c.gameObject);
            foreach (Transform c in _bunSlot) Destroy(c.gameObject);

            UiKit.DialogueBox(_column, Npc.Name, _dialogue);

            if (_cleared)
            {
                RenderStairs(); // value stairs — the visible score proxy (docs/04 §S2)
            }
            else
            {
                if (_mode == Mode.Questioning) RenderQuestions();
                else RenderActions();
                RenderInventory();
            }

            if (!string.IsNullOrEmpty(_bun)) UiKit.BunBubble(_bunSlot, _bun);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_column);
            ApplyNudges();
        }

        // ---- tutorial nudges (docs/01 §7: 状況＋明滅＋ぶんの一言, no text panels) ----

        void StopNudges()
        {
            foreach (var c in _nudges) if (c != null) StopCoroutine(c);
            _nudges.Clear();
        }

        void ApplyNudges()
        {
            if (_cleared || _mode != Mode.Idle) return;
            if (CanCombine())
            {
                // あわせ技: にもつの2枠が明滅（アブ捕獲後）
                foreach (var slot in _combineSlots)
                    _nudges.Add(StartCoroutine(TutorialNudge.Blink(slot)));
            }
            else if (_offerBtn != null && OfferReady())
            {
                // こうかん: おもちゃ完成後、こうかんボタンが明滅
                _nudges.Add(StartCoroutine(TutorialNudge.Blink(_offerBtn)));
            }
        }

        bool OfferReady() => Npc.Accepts != null && Npc.Accepts.Any(a => _session.Has(a.Item));

        // Ids of held items that form an available recipe (blink targets for the あわせ技 nudge).
        HashSet<string> CombinablePair()
        {
            var set = new HashSet<string>();
            var inv = _session.Save.Inventory;
            for (int i = 0; i < inv.Count; i++)
                for (int j = i + 1; j < inv.Count; j++)
                    if (Recipes.Combine(inv[i], inv[j], _session.Content.Recipes) != null)
                    {
                        set.Add(inv[i]);
                        set.Add(inv[j]);
                    }
            return set;
        }

        void RenderStairs()
        {
            var owned = new List<string>();
            foreach (var id in new[] { "item_wara", "item_abumushi_toy", "item_kibidango" })
                if (_session.Content.Items.TryGetValue(id, out var it)) owned.Add(it.Emoji);
            string goal = _session.Content.Items.TryGetValue(_session.Content.Route.GoalItem, out var g) ? g.Emoji : "🏠";
            UiKit.StairProgress(_column, owned, 7, goal);
        }

        void RenderActions()
        {
            var row = Row("Actions");
            Button(row, Str("btn_talk"), ButtonVariant.Ai, Talk);
            bool canAsk = Npc.Questions != null && _usedQuestions.Count < Npc.Questions.Length
                          && _session.QuestionsUsed < 2;
            if (canAsk) Button(row, Str("btn_ask"), ButtonVariant.Ghost, Ask); // hide when spent (docs/01 §2.3)
            _offerBtn = Button(row, Str("btn_offer"), ButtonVariant.Shu, OpenOffer);
            if (CanCombine()) Button(row, Str("btn_combine"), ButtonVariant.Ghost, TryCombine);
        }

        void RenderQuestions()
        {
            var col = Row("Questions");
            for (int i = 0; i < Npc.Questions.Length; i++)
            {
                if (_usedQuestions.Contains(i)) continue;
                int captured = i;
                Button(col, Npc.Questions[i].Q, ButtonVariant.Ghost, () => SelectQuestion(captured));
            }
            Button(col, "←", ButtonVariant.Ai, Talk); // back arrow
        }

        void RenderInventory()
        {
            var prog = _session.ProgressFor(_session.CurrentLocationId);
            UiKit.Text(_column, Str("btn_bag"), DesignTokens.FsSmall, DesignTokens.Disabled,
                TextAlignmentOptions.Left, "InvLabel");

            var combinable = CombinablePair();
            var row = Row("Inventory");
            foreach (var id in _session.Save.Inventory)
            {
                var item = _session.Content.Items[id];
                string label = item.Emoji + " " + item.NameRuby;
                bool offered = prog.OfferedItems.Contains(id);
                GameObject slot;
                if (_mode == Mode.Offering)
                {
                    string capture = id;
                    slot = Button(row, label, offered ? ButtonVariant.Ghost : ButtonVariant.Ai,
                        () => Offer(capture), disabled: offered);
                }
                else
                {
                    slot = Button(row, label, ButtonVariant.Ghost, null, disabled: true);
                    if (combinable.Contains(id)) _combineSlots.Add(slot); // あわせ技 blink target
                }
            }
        }

        bool CanCombine()
        {
            var inv = _session.Save.Inventory;
            for (int i = 0; i < inv.Count; i++)
                for (int j = i + 1; j < inv.Count; j++)
                    if (Recipes.Combine(inv[i], inv[j], _session.Content.Recipes) != null) return true;
            return false;
        }

        // ---- ui helpers ----

        string Str(string key) => _session.Content.Strings.TryGetValue(key, out var v) ? v : key;

        Transform Row(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_column, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = DesignTokens.Sp2;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            return go.transform;
        }

        GameObject Button(Transform parent, string label, ButtonVariant v, Action onClick, bool disabled = false)
        {
            var go = UiKit.ManjuButton(parent, label, v, disabled);
            if (!disabled && onClick != null)
                go.GetComponent<Button>().onClick.AddListener(() => onClick());
            return go;
        }

        void BuildCanvas()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)bg.transform);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = DesignTokens.Bg;
            bgImg.raycastTarget = false;

            var col = new GameObject("Column", typeof(RectTransform));
            col.transform.SetParent(canvas.transform, false);
            var colRt = (RectTransform)col.transform;
            colRt.anchorMin = new Vector2(0f, 0f);
            colRt.anchorMax = new Vector2(1f, 1f);
            colRt.offsetMin = new Vector2(DesignTokens.Sp3, DesignTokens.Sp3);
            colRt.offsetMax = new Vector2(-DesignTokens.Sp3, -DesignTokens.Sp3);
            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = DesignTokens.Sp3;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.UpperLeft;
            _column = col.transform;

            var bun = new GameObject("BunSlot", typeof(RectTransform));
            bun.transform.SetParent(canvas.transform, false);
            var bunRt = (RectTransform)bun.transform;
            bunRt.anchorMin = bunRt.anchorMax = bunRt.pivot = new Vector2(1f, 0f);
            bunRt.sizeDelta = new Vector2(320f, 0f);
            bunRt.anchoredPosition = new Vector2(-DesignTokens.Sp3, DesignTokens.Sp3);
            var bunV = bun.AddComponent<VerticalLayoutGroup>();
            bunV.childControlWidth = bunV.childControlHeight = true;
            bunV.childForceExpandWidth = false;
            bunV.childAlignment = TextAnchor.LowerRight;
            _bunSlot = bun.transform;

            var ov = new GameObject("Overlay", typeof(RectTransform));
            ov.transform.SetParent(canvas.transform, false);
            var ovRt = (RectTransform)ov.transform;
            ovRt.anchorMin = ovRt.anchorMax = ovRt.pivot = new Vector2(0.5f, 0.5f);
            ovRt.sizeDelta = new Vector2(360f, 0f);
            var ovV = ov.AddComponent<VerticalLayoutGroup>();
            ovV.childControlWidth = ovV.childControlHeight = true;
            ovV.childForceExpandWidth = true;
            ovV.childAlignment = TextAnchor.MiddleCenter;
            _overlay = ov.transform;

            if (Application.isEditor) BuildDebugStrip(canvas.transform);
        }

        // ---- editor-only review harness (T-U09) ----
        // The three mini-events fire from content triggers: tap_catch is the loc_kibitsu ambient
        // (played on start); horse-care fires on gaining item_uma_weak and boat fires on advancing to
        // loc_shukuba — neither is reachable until the route walker exists. This top strip lets a
        // reviewer replay each event now. Editor-only: it never appears in a shipped build.

        void BuildDebugStrip(Transform canvas)
        {
            var go = new GameObject("DebugEvents", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -DesignTokens.Sp1);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = DesignTokens.Sp1;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleCenter;
            go.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button(go.transform, "🪰", ButtonVariant.Ghost, () => PlayEvent("ev_abu_catch"));
            Button(go.transform, "🐴", ButtonVariant.Ghost, () => PlayEvent("ev_horse_care"));
            Button(go.transform, "🛶", ButtonVariant.Ghost, () => PlayEvent("ev_boat_ride"));
        }

        /// <summary>Play a content event by id (ambient wiring + editor review). Public so the
        /// Editor / MCP can drive each mini-event deterministically for capture.</summary>
        public MiniEventPlayer PlayEvent(string eventId)
        {
            if (_miniEvent != null) return _miniEvent; // one at a time
            if (!_session.Content.Events.TryGetValue(eventId, out var ev)) return null;
            _miniEvent = gameObject.AddComponent<MiniEventPlayer>();
            _miniEvent.Play(_session, ev, _overlay.parent, OnAmbientDone);
            return _miniEvent;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ---- success 演出 (docs/01 §1 TradeAccept → ValueMeter; docs/04 §4 numbers) ----

        void BuildMeter(string offeredId)
        {
            var item = _session.Content.Items[offeredId];
            var rule = Npc.Accepts.First(a => a.Item == offeredId);
            _meter = UiKit.ValueMeter(_overlay, item.Emoji, item.NameRuby,
                item.BaseValue, rule.ValueForNpc, rule.ReasonLine, Str("meter_mine"), Str("meter_theirs"));

            // 5th "their" star flashes shu (docs/04 §4). Set synchronously so it shows in the final state.
            var stars = TheirStars();
            if (rule.ValueForNpc >= 5 && stars.Count == 5) stars[4].color = DesignTokens.Shu;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_overlay);

            // Score wired here; per docs/01 §4 per-stop deductions stay off-screen (stairs are the
            // visible proxy, final rank shows at RouteClear). Logged for development.
            var prog = _session.ProgressFor(_session.CurrentLocationId);
            int stopScore = Score.StopScore(prog);
            Debug.Log($"[Score] {_session.CurrentLocationId}: stopScore={stopScore} rank={Score.RankFor(stopScore)}");
        }

        List<TextMeshProUGUI> TheirStars() =>
            _meter == null ? new List<TextMeshProUGUI>()
            : _meter.GetComponentsInChildren<TextMeshProUGUI>(true).Where(x => x.name == "TheirStar").ToList();

        IEnumerator AcceptFx()
        {
            // Success flash (docs/04 §4: 300ms white overlay .6 → 0).
            var flash = UiKit.FullscreenFlash(_overlay.parent, new Color(1f, 1f, 1f, 0.6f));
            for (float t = 0f; t < DesignTokens.DurSuccessFlash; t += Time.deltaTime)
            {
                flash.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.6f, 0f, t / DesignTokens.DurSuccessFlash));
                yield return null;
            }
            Destroy(flash.gameObject);

            // Reveal "their" stars left→right (150ms, spring pop + 180° spin — docs/04 §4).
            var stars = TheirStars();
            foreach (var s in stars) s.rectTransform.localScale = Vector3.zero;
            for (int i = 0; i < stars.Count; i++)
            {
                yield return new WaitForSeconds(0.15f);
                yield return PopIn(stars[i].rectTransform);
            }
        }

        static IEnumerator PopIn(RectTransform rt)
        {
            float d = DesignTokens.DurStarAppear;
            for (float t = 0f; t < d; t += Time.deltaTime)
            {
                float p = t / d;
                float s = BackOut(p);
                rt.localScale = new Vector3(s, s, 1f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(180f, 0f, p)); // 回転180°
                yield return null;
            }
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        static float BackOut(float p)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = p - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
