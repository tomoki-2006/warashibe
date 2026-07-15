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
        /// Apply a mini-event's completion effects (docs/03 §7): produce <c>gives</c> (tap_catch),
        /// swap <c>on_complete.replace_item</c> [from,to] (map_choice), and jump to
        /// <c>on_complete.advance_to</c> (cutscene). Zukan is recorded for produced/upgraded items,
        /// mirroring <see cref="ApplyOffer"/>. Immutable: a new GameState is returned.
        /// </summary>
        public static GameState ApplyEvent(GameState state, Event ev)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (ev == null) throw new ArgumentNullException(nameof(ev));
            var save = state.Save.Clone();

            if (!string.IsNullOrEmpty(ev.Gives))
            {
                if (!save.Inventory.Contains(ev.Gives)) save.Inventory.Add(ev.Gives);
                if (!save.ZukanItems.Contains(ev.Gives)) save.ZukanItems.Add(ev.Gives);
            }

            var replace = ev.OnComplete?.ReplaceItem;
            if (replace != null && replace.Length == 2)
            {
                var from = replace[0];
                var to = replace[1];
                int idx = save.Inventory.IndexOf(from);
                if (idx >= 0) save.Inventory[idx] = to;               // in-place swap keeps chain order
                else if (!save.Inventory.Contains(to)) save.Inventory.Add(to);
                if (!save.ZukanItems.Contains(to)) save.ZukanItems.Add(to);
            }

            var advanceTo = ev.OnComplete?.AdvanceTo;
            if (!string.IsNullOrEmpty(advanceTo))
            {
                var stops = state.Content.Route.Stops;
                int dest = Array.IndexOf(stops, advanceTo);
                if (dest >= 0) save.StopIndex = dest;                 // unknown loc: ignore (validator guards)
            }

            return new GameState(save, state.Content);
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
