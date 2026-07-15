using System;
using System.Collections.Generic;

namespace Warashibe.Core
{
    // Ported from docs/02 §3 exchange.ts / evaluateOffer. Pure function, no mutation.
    // Rules: docs/01 §2 (exchange) + §3 (hint ladder). Test matrix: docs/06 §2.
    public static class Exchange
    {
        /// <summary>
        /// Evaluate offering <paramref name="item"/> to <paramref name="npc"/> given the
        /// current per-stop <paramref name="prog"/>. Does NOT mutate prog — state is applied
        /// immutably by <see cref="Progress.ApplyOffer"/>.
        /// </summary>
        public static OfferResult EvaluateOffer(Npc npc, string item, StopProgress prog)
        {
            if (npc == null) throw new ArgumentNullException(nameof(npc));
            if (prog == null) throw new ArgumentNullException(nameof(prog));

            // 1) Accept: item is in the NPC's accept rules. Accept never advances the hint ladder,
            //    so hintLevelShown stays at the current deepest hint ("現状維持").
            if (npc.Accepts != null)
            {
                foreach (var rule in npc.Accepts)
                {
                    if (rule != null && rule.Item == item)
                    {
                        return new OfferResult
                        {
                            Outcome = OfferOutcome.Accept,
                            HintLevelShown = prog.DeepestHint,
                            Gained = rule.Gives,
                            Lines = ToList(rule.AcceptLines),
                            // ValueMeter.Mine needs the offered Item.baseValue (not in this signature);
                            // the store assembles the full meter from LoadedContent.Items (T-U07).
                            ValueMeter = null,
                        };
                    }
                }
            }

            // 2) Duplicate: already-offered (and previously declined) item. No penalty, no hint advance.
            //    The "already shown" bun line is a UI string (strings.ja.json), not NPC content.
            if (prog.OfferedItems.Contains(item))
            {
                return new OfferResult
                {
                    Outcome = OfferOutcome.Duplicate,
                    HintLevelShown = prog.DeepestHint,
                    Gained = null,
                    Lines = new List<string>(),
                    ValueMeter = null,
                };
            }

            // 3) Decline: advance the hint ladder by one, capped at L3 (no 4th tier — docs/01 §3).
            int level = Math.Min(prog.Rejections + 1, 3);
            var lines = new List<string>();
            if (npc.DeclineLines != null && npc.DeclineLines.Length >= level)
            {
                lines.Add(npc.DeclineLines[level - 1]);
            }
            return new OfferResult
            {
                Outcome = OfferOutcome.Decline,
                HintLevelShown = level,
                Gained = null,
                Lines = lines,
                ValueMeter = null,
            };
        }

        static List<string> ToList(string[] source) =>
            source == null ? new List<string>() : new List<string>(source);
    }
}
