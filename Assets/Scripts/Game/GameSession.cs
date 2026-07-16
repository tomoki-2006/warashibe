using System.Collections.Generic;
using Warashibe.Core;

namespace Warashibe.Game
{
    /// <summary>
    /// Runtime wrapper around the immutable Core <see cref="GameState"/>. Offers/advances go through
    /// the pure Core functions (Exchange/Progress/Recipes); questions and combines touch the working
    /// SaveData directly. This is the seam the UI (T-U07) drives — all rules stay in Core.
    /// </summary>
    public sealed class GameSession
    {
        public GameState State { get; private set; }
        public LoadedContent Content => State.Content;
        public SaveData Save => State.Save;

        public GameSession(GameState state) { State = state; }

        public string CurrentLocationId => Content.Route.Stops[Save.StopIndex];
        public Stop CurrentStop => Content.Stops[CurrentLocationId];

        public StopProgress ProgressFor(string locId) =>
            Save.Progress.TryGetValue(locId, out var p) ? p : new StopProgress();

        StopProgress MutableProgress(string locId)
        {
            if (!Save.Progress.TryGetValue(locId, out var p))
            {
                p = new StopProgress();
                Save.Progress[locId] = p;
            }
            return p;
        }

        /// <summary>Evaluate + apply an offer (docs/01 §2, §3). Returns the result for display.</summary>
        public OfferResult Offer(string npcId, string itemId)
        {
            var npc = Content.Npcs[npcId];
            var result = Exchange.EvaluateOffer(npc, itemId, ProgressFor(CurrentLocationId));
            State = Progress.ApplyOffer(State, npcId, itemId); // immutable update
            return result;
        }

        /// <summary>Combine two held items via a recipe (docs/01 §2.2). Returns output id, or null.</summary>
        public string Combine(string a, string b)
        {
            var output = Recipes.Combine(a, b, Content.Recipes);
            if (output == null) return null;
            var inv = Save.Inventory;
            inv.Remove(a);
            inv.Remove(b);
            if (!inv.Contains(output)) inv.Add(output);
            return output;
        }

        /// <summary>Questions used at the current stop (docs/01 §2.3: max 2, no score effect).</summary>
        public int QuestionsUsed => ProgressFor(CurrentLocationId).QuestionsUsed;
        public void UseQuestion() => MutableProgress(CurrentLocationId).QuestionsUsed++;

        // ---- mini-events (docs/03 §7 / docs/01 §7-8) ----

        public bool Has(string itemId) => Save.Inventory.Contains(itemId);

        /// <summary>The current stop's ambient mini-event, or null (docs/03 §7 stops.ambientEvent).</summary>
        public Event CurrentAmbientEvent =>
            !string.IsNullOrEmpty(CurrentStop.AmbientEvent)
            && Content.Events.TryGetValue(CurrentStop.AmbientEvent, out var ev) ? ev : null;

        /// <summary>Apply a mini-event's completion effects through Core (docs/03 §7).</summary>
        public void ApplyEvent(Event ev) => State = Progress.ApplyEvent(State, ev);

        // ---- route walking / value stairs (T-U12, docs/01 §6 / §5, docs/04 §S2) ----

        /// <summary>The stop's trading NPC (first with a non-empty accepts), or null.</summary>
        public Npc TradingNpc(Stop stop)
        {
            if (stop?.NpcIds == null) return null;
            foreach (var id in stop.NpcIds)
                if (Content.Npcs.TryGetValue(id, out var npc) && npc.Accepts != null && npc.Accepts.Length > 0)
                    return npc;
            return null;
        }

        /// <summary>The NPC to converse with at a stop: its trading NPC if any, else the first one.</summary>
        public string PrimaryNpcId(Stop stop)
        {
            var trading = TradingNpc(stop);
            if (trading != null) return trading.Id;
            return stop?.NpcIds != null && stop.NpcIds.Length > 0 ? stop.NpcIds[0] : null;
        }

        public bool IsStopCleared(string locId) =>
            Save.Progress.TryGetValue(locId, out var p) && p.Cleared;

        /// <summary>Value-stairs chain (docs/04 §S2): start item + each <b>cleared</b> trading stop's
        /// reward, in route order. The goal item is shown separately (see <see cref="ChainTotalSteps"/>).</summary>
        public List<string> ChainEmojisOwned()
        {
            var owned = new List<string>();
            if (Content.Items.TryGetValue(Content.Route.StartItem, out var start)) owned.Add(start.Emoji);
            foreach (var locId in Content.Route.Stops)
            {
                if (!IsStopCleared(locId)) continue;
                var npc = TradingNpc(Content.Stops[locId]);
                if (npc == null) continue;
                var gives = npc.Accepts[0].Gives;
                if (gives == Content.Route.GoalItem) continue; // goal has its own slot
                if (Content.Items.TryGetValue(gives, out var g)) owned.Add(g.Emoji);
            }
            return owned;
        }

        /// <summary>Total non-goal stair steps: start item + every trading stop whose reward isn't the goal.</summary>
        public int ChainTotalSteps()
        {
            int steps = 1; // start item
            foreach (var locId in Content.Route.Stops)
            {
                var npc = TradingNpc(Content.Stops[locId]);
                if (npc != null && npc.Accepts[0].Gives != Content.Route.GoalItem) steps++;
            }
            return steps;
        }

        /// <summary>Trip total over cleared stops (docs/01 §4). Observation bonus is 0 until observed
        /// tracking lands.</summary>
        public int TripScore()
        {
            var stopScores = new List<int>();
            foreach (var locId in Content.Route.Stops)
                if (IsStopCleared(locId)) stopScores.Add(Score.StopScore(ProgressFor(locId)));
            return Score.TripScore(stopScores, 0);
        }

        public RouteRank Rank() => Score.RankFor(TripScore());
    }
}
