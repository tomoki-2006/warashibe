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
    }
}
