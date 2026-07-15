using System.Collections.Generic;
using Newtonsoft.Json;

namespace Warashibe.Core
{
    // Ported from docs/02_TDD.md §2 (types.ts). Pure C# — no UnityEngine.
    // Content-type property names are annotated with [JsonProperty] to match the
    // exact JSON keys in docs/03 (the deserialization schema loaded in T-U05).
    //
    // ItemId / NpcId / LocationId are plain strings ("item_wara", "npc_child", "loc_tanbo").

    // ===== Content types (schema of StreamingAssets JSON) =====

    public sealed class Item
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("name_ruby")] public string NameRuby { get; set; }
        [JsonProperty("emoji")] public string Emoji { get; set; }
        [JsonProperty("origin")] public string Origin { get; set; }
        [JsonProperty("trivia")] public string Trivia { get; set; }
        [JsonProperty("baseValue")] public int BaseValue { get; set; } // 1..5 (your-side star)
    }

    public sealed class Recipe
    {
        [JsonProperty("inputs")] public string[] Inputs { get; set; } // exactly 2, matched order-independently
        [JsonProperty("output")] public string Output { get; set; }
    }

    public sealed class NpcQuestion
    {
        [JsonProperty("q")] public string Q { get; set; }
        [JsonProperty("a")] public string A { get; set; }
    }

    public sealed class AcceptRule
    {
        [JsonProperty("item")] public string Item { get; set; }
        [JsonProperty("valueForNpc")] public int ValueForNpc { get; set; } // 1..5 (their-side star, > baseValue)
        [JsonProperty("reasonLine")] public string ReasonLine { get; set; }
        [JsonProperty("gives")] public string Gives { get; set; }
        [JsonProperty("acceptLines")] public string[] AcceptLines { get; set; }
        [JsonProperty("postEvent")] public string PostEvent { get; set; } // optional EventId
    }

    public sealed class Observable
    {
        [JsonProperty("target")] public string Target { get; set; }
        [JsonProperty("bunLine")] public string BunLine { get; set; }
    }

    public sealed class Npc
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("portrait")] public string Portrait { get; set; }
        [JsonProperty("intro")] public string[] Intro { get; set; }
        [JsonProperty("idleLine")] public string IdleLine { get; set; }
        [JsonProperty("questions")] public NpcQuestion[] Questions { get; set; } // max 2
        [JsonProperty("accepts")] public AcceptRule[] Accepts { get; set; }      // Phase 0: 1 per NPC
        [JsonProperty("declineLines")] public string[] DeclineLines { get; set; } // [L1, L2, L3]
        [JsonProperty("hintL2")] public string HintL2 { get; set; }               // bun bubble
        [JsonProperty("hintL3")] public string HintL3 { get; set; }               // bun reveals answer
        [JsonProperty("highlightTarget")] public string HighlightTarget { get; set; } // optional
        [JsonProperty("afterTradeLine")] public string AfterTradeLine { get; set; }
        [JsonProperty("observables")] public Observable[] Observables { get; set; } // optional
    }

    public sealed class Stop
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("region")] public string Region { get; set; }
        [JsonProperty("mapX")] public int MapX { get; set; }
        [JsonProperty("bg")] public string Bg { get; set; }
        [JsonProperty("npcIds")] public string[] NpcIds { get; set; }
        [JsonProperty("ambientEvent")] public string AmbientEvent { get; set; } // optional EventId
    }

    public sealed class Route
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("startItem")] public string StartItem { get; set; }
        [JsonProperty("goalItem")] public string GoalItem { get; set; }
        [JsonProperty("stops")] public string[] Stops { get; set; } // LocationIds in travel order
    }

    // Event content (docs/03 §7). Only the fields the engine/validator needs are mapped;
    // per-type `spec` details stay in JSON (UI reads them in later tickets).
    public sealed class Event
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("gives")] public string Gives { get; set; }              // item produced (optional)
        [JsonProperty("on_complete")] public EventOnComplete OnComplete { get; set; }
    }

    public sealed class EventOnComplete
    {
        [JsonProperty("replace_item")] public string[] ReplaceItem { get; set; } // [from, to]
        [JsonProperty("advance_to")] public string AdvanceTo { get; set; }
    }

    // ===== Runtime types =====

    public enum OfferOutcome { Accept, Decline, Duplicate }

    // routeRank() range: docs/02 §3 returns "choja"|"daishonin"|"gyoshonin"|"minarai".
    public enum RouteRank { Choja, Daishonin, Gyoshonin, Minarai }

    // Validation (docs/03 §9-10 + docs/02 §3 validateRoute). One code per failure kind.
    public enum ValidationCode
    {
        StopMissing,        // 1: route.stops references an unknown location
        GoalUnreachable,    // 2: startItem cannot reach goalItem via recipes/accepts/events
        ValueNotAboveBase,  // 3: AcceptRule.valueForNpc <= offered item's baseValue
        NpcShape,           // 4: declineLines != 3, questions > 2, or empty hintL2/L3 (trading NPC)
        RefMissing,         // 5: referenced item/npc/event id does not exist
        TextTooLong,        // 6: a sentence exceeds the kana/kanji length limit
        RubyMissing,        // 6: dialogue kanji outside the approved (ruby-free) set
        KatakanaNotAllowed, // 6: katakana word outside the allow-list
        FuriganaMissing,    // 6: item name has kanji but name_ruby is empty
        HighlightMissing,   // 7: highlightTarget is neither an item nor an event
        ChainNotMonotonic,  // 8: a non-postEvent trade lowers the value stairs
        ObservableMissing,  // 9: observable target is empty
    }

    public sealed class ValidationError
    {
        public ValidationCode Code { get; }
        public string Where { get; }   // id / context the problem is anchored to
        public string Message { get; }

        public ValidationError(ValidationCode code, string where, string message)
        {
            Code = code;
            Where = where;
            Message = message;
        }

        public override string ToString() => "[" + Code + "] " + Where + ": " + Message;
    }

    public sealed class ValueMeter
    {
        public int Mine { get; set; }   // offered item's baseValue (your side)
        public int Theirs { get; set; } // AcceptRule.valueForNpc (their side)
        public string Reason { get; set; }
    }

    public sealed class OfferResult
    {
        public OfferOutcome Outcome { get; set; }
        public int HintLevelShown { get; set; } // 0..3
        public string Gained { get; set; }      // ItemId on accept, else null
        public List<string> Lines { get; set; } = new List<string>();
        public ValueMeter ValueMeter { get; set; } // null from EvaluateOffer (see Exchange notes)
    }

    public sealed class StopProgress
    {
        [JsonProperty("questionsUsed")] public int QuestionsUsed { get; set; } // 0..2
        [JsonProperty("rejections")] public int Rejections { get; set; }
        [JsonProperty("deepestHint")] public int DeepestHint { get; set; }     // 0..3
        [JsonProperty("offeredItems")] public List<string> OfferedItems { get; set; } = new List<string>();
        [JsonProperty("cleared")] public bool Cleared { get; set; }

        public StopProgress Clone() => new StopProgress
        {
            QuestionsUsed = QuestionsUsed,
            Rejections = Rejections,
            DeepestHint = DeepestHint,
            OfferedItems = new List<string>(OfferedItems),
            Cleared = Cleared,
        };
    }

    public sealed class Settings
    {
        [JsonProperty("tts")] public bool Tts { get; set; }
        [JsonProperty("bgm")] public bool Bgm { get; set; }
        [JsonProperty("se")] public bool Se { get; set; }

        public Settings Clone() => new Settings { Tts = Tts, Bgm = Bgm, Se = Se };
    }

    public sealed class SaveData
    {
        [JsonProperty("version")] public int Version { get; set; } = 1;
        [JsonProperty("playerName")] public string PlayerName { get; set; } = "";
        [JsonProperty("routeId")] public string RouteId { get; set; } = "";
        [JsonProperty("stopIndex")] public int StopIndex { get; set; }
        [JsonProperty("inventory")] public List<string> Inventory { get; set; } = new List<string>();
        [JsonProperty("progress")] public Dictionary<string, StopProgress> Progress { get; set; } = new Dictionary<string, StopProgress>();
        [JsonProperty("bestScore")] public int BestScore { get; set; }
        [JsonProperty("clearedRoutes")] public List<string> ClearedRoutes { get; set; } = new List<string>();
        [JsonProperty("zukanItems")] public List<string> ZukanItems { get; set; } = new List<string>();
        [JsonProperty("zukanNpcs")] public List<string> ZukanNpcs { get; set; } = new List<string>();
        [JsonProperty("settings")] public Settings Settings { get; set; } = new Settings();

        public SaveData Clone()
        {
            var progress = new Dictionary<string, StopProgress>();
            foreach (var kv in Progress) progress[kv.Key] = kv.Value.Clone();
            return new SaveData
            {
                Version = Version,
                PlayerName = PlayerName,
                RouteId = RouteId,
                StopIndex = StopIndex,
                Inventory = new List<string>(Inventory),
                Progress = progress,
                BestScore = BestScore,
                ClearedRoutes = new List<string>(ClearedRoutes),
                ZukanItems = new List<string>(ZukanItems),
                ZukanNpcs = new List<string>(ZukanNpcs),
                Settings = Settings.Clone(),
            };
        }
    }

    // Parsed content held after startup load (T-U05). Treated as immutable by the engine.
    public sealed class LoadedContent
    {
        public Route Route { get; set; }
        public Dictionary<string, Stop> Stops { get; set; } = new Dictionary<string, Stop>();
        public Dictionary<string, Npc> Npcs { get; set; } = new Dictionary<string, Npc>();
        public Dictionary<string, Item> Items { get; set; } = new Dictionary<string, Item>();
        public Dictionary<string, Event> Events { get; set; } = new Dictionary<string, Event>();
        public Dictionary<string, string> Strings { get; set; } = new Dictionary<string, string>();
        public Recipe[] Recipes { get; set; } = new Recipe[0];
    }

    // Immutable game state the engine transforms: persistent Save + loaded Content.
    public sealed class GameState
    {
        public SaveData Save { get; }
        public LoadedContent Content { get; }

        public GameState(SaveData save, LoadedContent content)
        {
            Save = save;
            Content = content;
        }

        // Route is cleared once its routeId is recorded (see Progress.AdvanceStop).
        public bool IsRouteClear => Save != null && Save.ClearedRoutes.Contains(Save.RouteId);
    }
}
