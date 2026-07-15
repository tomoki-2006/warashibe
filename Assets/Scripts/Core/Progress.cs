using System;

namespace Warashibe.Core
{
    // Ported from docs/02 §3 progress.ts. Immutable state transitions: the input GameState is
    // never mutated; a new GameState (with a deep-cloned SaveData) is returned. Test matrix: docs/06 §2.
    public static class Progress
    {
        /// <summary>
        /// Apply an offer of <paramref name="item"/> to <paramref name="npcId"/> at the current stop.
        /// Accept  -> inventory swap (offered item out, gained item in), stop cleared, zukan recorded.
        /// Decline -> rejections++, deepest hint advanced, item recorded as offered.
        /// Duplicate -> no state change.
        /// "Next stop unlocked" (docs/06 §2) is represented by the stop's Cleared flag, which gates
        /// movement in a linear route; the index itself advances via <see cref="AdvanceStop"/>.
        /// </summary>
        public static GameState ApplyOffer(GameState state, string npcId, string item)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var content = state.Content;
            var save = state.Save.Clone();

            var locId = content.Route.Stops[save.StopIndex];
            if (!save.Progress.TryGetValue(locId, out var prog))
            {
                prog = new StopProgress();
                save.Progress[locId] = prog;
            }

            var npc = content.Npcs[npcId];
            var result = Exchange.EvaluateOffer(npc, item, prog);

            switch (result.Outcome)
            {
                case OfferOutcome.Accept:
                    save.Inventory.Remove(item);
                    var gained = result.Gained;
                    if (!string.IsNullOrEmpty(gained) && !save.Inventory.Contains(gained))
                        save.Inventory.Add(gained);
                    prog.Cleared = true;
                    if (!prog.OfferedItems.Contains(item)) prog.OfferedItems.Add(item);
                    if (!string.IsNullOrEmpty(gained) && !save.ZukanItems.Contains(gained))
                        save.ZukanItems.Add(gained);
                    if (!save.ZukanNpcs.Contains(npcId)) save.ZukanNpcs.Add(npcId);
                    break;

                case OfferOutcome.Decline:
                    // EvaluateOffer only returns Decline when the item was not already offered,
                    // so no duplicate guard is needed here.
                    prog.Rejections += 1;
                    if (result.HintLevelShown > prog.DeepestHint) prog.DeepestHint = result.HintLevelShown;
                    prog.OfferedItems.Add(item);
                    break;

                case OfferOutcome.Duplicate:
                    // no-op
                    break;
            }

            return new GameState(save, content);
        }

        /// <summary>
        /// Advance to the next stop. At the final stop this records route completion
        /// (routeId added to clearedRoutes = RouteClear) instead of moving past the end.
        /// </summary>
        public static GameState AdvanceStop(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var save = state.Save.Clone();
            var stops = state.Content.Route.Stops;

            if (save.StopIndex >= stops.Length - 1)
            {
                if (!save.ClearedRoutes.Contains(save.RouteId)) save.ClearedRoutes.Add(save.RouteId);
            }
            else
            {
                save.StopIndex += 1;
            }

            return new GameState(save, state.Content);
        }
    }
}
