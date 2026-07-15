using System.Collections.Generic;
using Newtonsoft.Json;

namespace Warashibe.Core
{
    // Pure JSON -> typed content parsing (docs/03 schema). File I/O stays in the Game layer
    // (StreamingContentLoader); this remains UnityEngine-free and unit-testable with strings.
    public static class ContentParser
    {
        public static Item[] ParseItems(string json) => JsonConvert.DeserializeObject<Item[]>(json);
        public static Recipe[] ParseRecipes(string json) => JsonConvert.DeserializeObject<Recipe[]>(json);
        public static Route ParseRoute(string json) => JsonConvert.DeserializeObject<Route>(json);
        public static Stop[] ParseStops(string json) => JsonConvert.DeserializeObject<Stop[]>(json);
        public static Npc[] ParseNpcs(string json) => JsonConvert.DeserializeObject<Npc[]>(json);
        public static Event[] ParseEvents(string json) => JsonConvert.DeserializeObject<Event[]>(json);

        public static Dictionary<string, string> ParseStrings(string json) =>
            JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();

        /// <summary>Assemble a LoadedContent from the raw JSON of each content file.</summary>
        public static LoadedContent Assemble(string itemsJson, string recipesJson, string routeJson,
            string stopsJson, string npcsJson, string eventsJson, string stringsJson)
        {
            var content = new LoadedContent
            {
                Route = ParseRoute(routeJson),
                Recipes = ParseRecipes(recipesJson) ?? new Recipe[0],
                Strings = ParseStrings(stringsJson),
            };
            foreach (var it in ParseItems(itemsJson) ?? new Item[0]) content.Items[it.Id] = it;
            foreach (var st in ParseStops(stopsJson) ?? new Stop[0]) content.Stops[st.Id] = st;
            foreach (var np in ParseNpcs(npcsJson) ?? new Npc[0]) content.Npcs[np.Id] = np;
            foreach (var ev in ParseEvents(eventsJson) ?? new Event[0]) content.Events[ev.Id] = ev;
            return content;
        }
    }
}
