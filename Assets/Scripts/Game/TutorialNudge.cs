using System.Collections;
using UnityEngine;

namespace Warashibe.Game
{
    /// <summary>
    /// Tutorial guidance primitives (docs/01 §7): guidance is <b>only</b> 状況 + 明滅 (motion) +
    /// ぶんの一言 — full-screen text instruction panels are forbidden (§7). This class supplies the
    /// motion half (blink / bounce); the "ぶんの一言" half is an ordinary <see cref="UiKit.BunBubble"/>
    /// fed a string from content, and the "状況" half is whatever screen the caller already shows.
    ///
    /// Each method is a coroutine the caller StartCoroutine's on a target; Blink loops until the
    /// coroutine is stopped or the object is destroyed. No Japanese literals live here (motion only).
    /// </summary>
    public static class TutorialNudge
    {
        // Attention-pulse tuning. Period reuses the doc-sourced screen-transition token so timing
        // stays token-driven (docs/13 §3); the alpha/scale amplitudes below are motion ratios, not
        // color/size design tokens, so they live here with an explanatory comment.
        const float BlinkMinAlpha = 0.30f;  // dimmest point of a blink cycle
        const float BounceAmount = 0.22f;   // peak extra scale of a single bounce

        static CanvasGroup EnsureGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>Blink a target's alpha to draw the eye to it (docs/01 §7 「明滅」). Loops until
        /// stopped. Pulses a CanvasGroup so composite targets (button = frame + label) blink whole.</summary>
        public static IEnumerator Blink(GameObject target)
        {
            if (target == null) yield break;
            var cg = EnsureGroup(target);
            float half = DesignTokens.DurScreenTrans;
            while (target != null)
            {
                for (float t = 0f; t < half; t += Time.deltaTime)
                {
                    cg.alpha = Mathf.Lerp(1f, BlinkMinAlpha, t / half);
                    yield return null;
                }
                for (float t = 0f; t < half; t += Time.deltaTime)
                {
                    cg.alpha = Mathf.Lerp(BlinkMinAlpha, 1f, t / half);
                    yield return null;
                }
                cg.alpha = 1f;
            }
        }

        /// <summary>One scale bounce to flag a target once (docs/01 §7 にもつ「1回だけバウンス」).</summary>
        public static IEnumerator BounceOnce(RectTransform rt)
        {
            if (rt == null) yield break;
            float d = DesignTokens.DurPop;
            for (float t = 0f; t < d; t += Time.deltaTime)
            {
                float s = 1f + BounceAmount * Mathf.Sin((t / d) * Mathf.PI); // 1 → 1+amt → 1
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}
