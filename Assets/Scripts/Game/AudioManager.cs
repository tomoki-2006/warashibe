using System.Collections.Generic;
using UnityEngine;
using Warashibe.Core;

namespace Warashibe.Game
{
    /// <summary>
    /// Plays SE / BGM by id (docs/04 §5) through procedurally-synthesised clips (<see cref="SynthAudio"/>).
    /// Respects mute and defers BGM until the first user tap (WebGL autoplay policy, docs/04 §5).
    /// Lazy singleton that survives screen swaps. There is no settings screen in Phase 0 (docs/01 §11),
    /// so mute defaults to on-sound; <see cref="ApplySettings"/> lets a future settings UI flip it.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AudioManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AudioManager>();
                    _instance.Init();
                }
                return _instance;
            }
        }

        public static bool SeEnabled = true;
        public static bool BgmEnabled = true;

        AudioSource _bgm;
        AudioSource _se;
        readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        bool _unlocked;
        string _pendingBgm;

        void Init()
        {
            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.loop = true;
            _bgm.playOnAwake = false;
            _bgm.volume = 0.5f;
            _se = gameObject.AddComponent<AudioSource>();
            _se.playOnAwake = false;
        }

        /// <summary>Apply persisted sound settings (docs/01 §11). No-op-friendly; call when a settings
        /// system exists. Passing null leaves the defaults (sound on).</summary>
        public void ApplySettings(Settings s)
        {
            if (s == null) return;
            SeEnabled = s.Se;
            BgmEnabled = s.Bgm;
            if (!BgmEnabled) _bgm.Stop();
        }

        AudioClip Cached(string id, System.Func<AudioClip> make)
        {
            if (!_cache.TryGetValue(id, out var c) || c == null) { c = make(); _cache[id] = c; }
            return c;
        }

        public void PlaySe(string id)
        {
            if (!SeEnabled) return;
            var c = Cached(id, () => SynthAudio.Se(id));
            if (c != null) _se.PlayOneShot(c);
        }

        /// <summary>se_stair rises one note per stop cleared (docs/04 §5). Not cached (varies by step).</summary>
        public void PlayStair(int step)
        {
            if (!SeEnabled) return;
            var c = SynthAudio.StairNote(step);
            if (c != null) _se.PlayOneShot(c);
        }

        /// <summary>Call on the first user interaction to satisfy the browser autoplay policy
        /// (docs/04 §5: BGMはユーザー初タップ後に再生開始). Starts any BGM requested before unlock.</summary>
        public void Unlock()
        {
            if (_unlocked) return;
            _unlocked = true;
            if (!string.IsNullOrEmpty(_pendingBgm))
            {
                var id = _pendingBgm;
                _pendingBgm = null;
                PlayBgm(id);
            }
        }

        public void PlayBgm(string id)
        {
            if (!BgmEnabled) { _bgm.Stop(); return; }
            if (!_unlocked) { _pendingBgm = id; return; } // deferred until first tap
            var c = Cached(id, () => SynthAudio.Bgm(id));
            if (c == null) return;
            if (_bgm.clip == c && _bgm.isPlaying) return;
            _bgm.clip = c;
            _bgm.Play();
        }

        public void StopBgm() => _bgm.Stop();
    }
}
