using System;
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

        void Start()
        {
            var content = StreamingContentLoader.LoadRoute();

            // Seed a demo state at loc_kibitsu with straw + horsefly so the player can combine,
            // offer the toy (accept), or offer a wrong item (decline → hint ladder).
            var save = new SaveData
            {
                RouteId = content.Route.Id,
                StopIndex = 1, // loc_kibitsu
                Inventory = new List<string> { "item_wara", "item_abu" },
            };
            _session = new GameSession(new GameState(save, content));
            _npcId = "npc_child";

            BuildCanvas();
            Talk();
        }

        // ---- drive methods (buttons + Editor) ----

        public void Talk()
        {
            _dialogue = Npc.Intro?.ToList() ?? new List<string>();
            _bun = null;
            _mode = Mode.Idle;
            Render();
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
                    break;
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
            foreach (Transform c in _column) Destroy(c.gameObject);
            foreach (Transform c in _bunSlot) Destroy(c.gameObject);

            UiKit.DialogueBox(_column, Npc.Name, _dialogue);

            if (_mode == Mode.Questioning) RenderQuestions();
            else RenderActions();

            RenderInventory();

            if (!string.IsNullOrEmpty(_bun)) UiKit.BunBubble(_bunSlot, _bun);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_column);
        }

        void RenderActions()
        {
            var row = Row("Actions");
            Button(row, Str("btn_talk"), ButtonVariant.Ai, Talk);
            bool canAsk = Npc.Questions != null && _usedQuestions.Count < Npc.Questions.Length
                          && _session.QuestionsUsed < 2;
            if (canAsk) Button(row, Str("btn_ask"), ButtonVariant.Ghost, Ask); // hide when spent (docs/01 §2.3)
            Button(row, Str("btn_offer"), ButtonVariant.Shu, OpenOffer);
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

            var row = Row("Inventory");
            foreach (var id in _session.Save.Inventory)
            {
                var item = _session.Content.Items[id];
                string label = item.Emoji + " " + item.NameRuby;
                bool offered = prog.OfferedItems.Contains(id);
                if (_mode == Mode.Offering)
                {
                    string capture = id;
                    Button(row, label, offered ? ButtonVariant.Ghost : ButtonVariant.Ai,
                        () => Offer(capture), disabled: offered);
                }
                else
                {
                    Button(row, label, ButtonVariant.Ghost, null, disabled: true);
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

        void Button(Transform parent, string label, ButtonVariant v, Action onClick, bool disabled = false)
        {
            var go = UiKit.ManjuButton(parent, label, v, disabled);
            if (!disabled && onClick != null)
                go.GetComponent<Button>().onClick.AddListener(() => onClick());
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
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
