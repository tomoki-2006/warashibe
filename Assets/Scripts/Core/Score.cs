using System;
using System.Collections.Generic;

namespace Warashibe.Core
{
    // Ported from docs/02 §3 score.ts. Formula: docs/01 §4. Test matrix: docs/06 §2.
    public static class Score
    {
        /// <summary>Observation bonus per stop (observable tapped before first accept). docs/01 §4.</summary>
        public const int ObservationBonus = 10;

        /// <summary>
        /// Per-stop score. base 100 − question penalty (−15/question, cap −30)
        /// − deepest-hint penalty (L1 −30 / L2 −50 / L3 −80, deepest only), floored at 10.
        /// Callers sum this over cleared stops only.
        /// </summary>
        public static int StopScore(StopProgress prog)
        {
            if (prog == null) throw new ArgumentNullException(nameof(prog));
            int questionPenalty = Math.Min(prog.QuestionsUsed * 15, 30);
            int hintPenalty;
            switch (prog.DeepestHint)
            {
                case 1: hintPenalty = 30; break;
                case 2: hintPenalty = 50; break;
                case 3: hintPenalty = 80; break;
                default: hintPenalty = 0; break;
            }
            return Math.Max(100 - questionPenalty - hintPenalty, 10);
        }

        /// <summary>
        /// Trip total = Σ per-stop scores + observation bonus (+10 per observed stop). docs/01 §4.
        /// </summary>
        public static int TripScore(IEnumerable<int> stopScores, int observedStopCount)
        {
            int sum = 0;
            if (stopScores != null)
            {
                foreach (var s in stopScores) sum += s;
            }
            return sum + observedStopCount * ObservationBonus;
        }

        /// <summary>Maps a trip total to a rank (docs/01 §4 thresholds, v3.2 values).</summary>
        public static RouteRank RankFor(int total)
        {
            if (total >= 540) return RouteRank.Choja;      // 長者
            if (total >= 420) return RouteRank.Daishonin;  // 大商人
            if (total >= 250) return RouteRank.Gyoshonin;  // 行商人
            return RouteRank.Minarai;                      // 見習い
        }
    }
}
