using UnityEngine;

namespace Warashibe.Game
{
    /// <summary>
    /// Design tokens transcribed verbatim from docs/04_UI_SPEC.md (§1 tokens, §4 animation table).
    /// Raw color/size/duration literals must NOT be hardcoded elsewhere — reference these
    /// constants instead (docs/13 §3: "生値のハードコード禁止").
    /// </summary>
    public static class DesignTokens
    {
        // --- Colors (§1) ---
        public static readonly Color Bg       = Hex("#F5F0E4"); // --c-bg (kinari): base tone
        public static readonly Color Ink      = Hex("#3A3226"); // --c-ink (sumi): body text
        public static readonly Color Ai       = Hex("#2C4A6E"); // --c-ai: primary UI / buttons
        public static readonly Color Shu      = Hex("#C0392B"); // --c-shu: success FX / emphasis
        public static readonly Color Wakakusa = Hex("#6B8E4E"); // --c-wakakusa: progress / stairs
        public static readonly Color Gold     = Hex("#C9A227"); // --c-gold: choja rank
        public static readonly Color White    = Hex("#FFFDF7"); // --c-white
        public static readonly Color Shadow   = new Color(58f / 255f, 50f / 255f, 38f / 255f, 0.18f); // --c-shadow rgba(58,50,38,.18)
        public static readonly Color Disabled = Hex("#B8B0A0"); // --c-disabled

        // --- Type scale (§1), px ---
        public const float FsBody      = 18f; // --fs-body
        public const float FsSerifName = 20f; // --fs-serif-name
        public const float FsBtn       = 20f; // --fs-btn
        public const float FsTitle     = 32f; // --fs-title
        public const float FsSmall     = 14f; // --fs-small
        public const float LineHeight  = 1.8f; // --lh

        // --- Spacing / shape (§1), px ---
        public const float Sp1       = 8f;   // --sp-1
        public const float Sp2       = 16f;  // --sp-2
        public const float Sp3       = 24f;  // --sp-3
        public const float Sp4       = 40f;  // --sp-4
        public const float Radius    = 16f;  // --radius
        public const float RadiusBtn = 999f; // --radius-btn (manju / pill)
        public const float TapMin    = 56f;  // --tap-min (min tap target)

        // --- Animation durations (§4), seconds ---
        public const float DurOfferFloat   = 0.500f; // offer item float
        public const float DurSuccessFlash = 0.300f; // success flash
        public const float DurPop          = 0.400f; // "!" pop
        public const float DurStarAppear   = 0.220f; // star appear (1)
        public const float DurStairRise    = 0.600f; // stair rise (1 step)
        public const float DurRefuseShake  = 0.350f; // refuse head-shake
        public const float DurBunBubble    = 0.250f; // bun bubble
        public const float DurScreenTrans  = 0.320f; // screen transition
        public const float DurCombine      = 1.200f; // combine FX
        public const float DurChainItem    = 0.260f; // chain final anim, per item

        // --- Easing control points (§1: cubic-bezier x1,y1,x2,y2) ---
        public static readonly Vector4 EaseOut    = new Vector4(0.22f, 1f, 0.36f, 1f);    // --ease-out
        public static readonly Vector4 EaseSpring = new Vector4(0.34f, 1.56f, 0.64f, 1f); // --ease-spring

        static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out var c);
            return c;
        }
    }
}
