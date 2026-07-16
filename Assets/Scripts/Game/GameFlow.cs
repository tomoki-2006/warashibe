using System;
using System.Collections.Generic;
using UnityEngine;
using Warashibe.Core;

namespace Warashibe.Game
{
    /// <summary>
    /// T-U12 — top-level app flow (docs/01 §1): Title → Map → Encounter×N → Result, inside the single
    /// Main scene (docs/13 §3: UI-state switching, no extra scenes). Loads + validates content once
    /// (T-U10 async), owns the <see cref="GameSession"/>, and swaps one self-contained screen at a
    /// time. Progression is linear: a stop unlocks the next when it's cleared (a completed trade, or a
    /// non-trading stop after its intro). Autosave / つづきから (docs/01 §9) is a follow-up — with no
    /// save yet, Title hides つづきから (spec-compliant).
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        public static bool AnimateFx = true; // propagates to every screen for headless capture

        LoadedContent _content;
        GameSession _session;
        GameObject _screen;                                 // the active screen (destroyed on transition)
        readonly HashSet<int> _cleared = new HashSet<int>(); // stop indices the player has finished
        int _current;                                        // stop the player is at

        void Start()
        {
            PropagateAnimateFx();
            StartCoroutine(StreamingContentLoader.LoadRouteRoutine(
                StreamingContentLoader.DefaultRouteFolder, OnLoaded, OnError));
        }

        void OnLoaded(LoadedContent content) { _content = content; ShowTitle(); }
        static void OnError(Exception e) => Debug.LogError("[GameFlow] content load failed: " + e.Message);

        void PropagateAnimateFx()
        {
            EncounterScreen.AnimateFx = AnimateFx;
            MiniEventPlayer.AnimateFx = AnimateFx;
            MapScreen.AnimateFx = AnimateFx;
            ResultScreen.AnimateFx = AnimateFx;
        }

        void NewGame()
        {
            var save = new SaveData
            {
                RouteId = _content.Route.Id,
                StopIndex = 0,
                Inventory = new List<string> { _content.Route.StartItem },
            };
            _session = new GameSession(new GameState(save, _content));
            _cleared.Clear();
            _current = 0;
            AudioManager.Instance.Unlock(); // first tap = はじめる → satisfy the WebGL autoplay policy (docs/04 §5)
            ShowMap();
        }

        // ---- screen transitions (each screen owns its own canvas) ----

        void Swap(GameObject next) { if (_screen != null) Destroy(_screen); _screen = next; }

        void ShowTitle()
        {
            var go = new GameObject("TitleScreen");
            Swap(go);
            go.AddComponent<TitleScreen>().Begin(_content, hasSave: false, onStart: NewGame);
        }

        void ShowMap()
        {
            AudioManager.Instance.PlayBgm("bgm_michi"); // 道中 (docs/04 §5)
            var go = new GameObject("MapScreen");
            Swap(go);
            go.AddComponent<MapScreen>().Begin(_session, ReachableMax(), _current, EnterStop);
        }

        void EnterStop(int index)
        {
            _current = index;
            _session.Save.StopIndex = index; // the encounter reads CurrentLocationId / progress from this
            AudioManager.Instance.PlayBgm("bgm_ichi"); // 出会い/市 (docs/04 §5)
            var stop = _content.Stops[_content.Route.Stops[index]];
            var npcId = _session.PrimaryNpcId(stop);
            var go = new GameObject("EncounterScreen");
            Swap(go);
            go.AddComponent<EncounterScreen>().Begin(_session, npcId, () => OnStopCleared(index));
        }

        void OnStopCleared(int index)
        {
            // A postEvent's advance_to (舟) may have moved StopIndex; GameFlow owns travel, so re-assert.
            _session.Save.StopIndex = index;
            _cleared.Add(index);

            bool isLast = index >= _content.Route.Stops.Length - 1;
            if (isLast && _session.IsStopCleared(_content.Route.Stops[index])) ShowResult(); // RouteClear
            else ShowMap();
        }

        void ShowResult()
        {
            var go = new GameObject("ResultScreen");
            Swap(go);
            go.AddComponent<ResultScreen>().Begin(_session, onReplay: NewGame, onTitle: ShowTitle);
        }

        // Reachable stops = cleared stops + the next one (linear route, docs/01 §6).
        int ReachableMax()
        {
            int max = 0;
            foreach (var i in _cleared) max = Mathf.Max(max, i + 1);
            return Mathf.Min(max, _content.Route.Stops.Length - 1);
        }
    }
}
