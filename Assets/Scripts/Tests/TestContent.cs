using System.Collections.Generic;
using Warashibe.Core;

namespace Warashibe.Core.Tests
{
    // Shared in-memory fixtures for the Core engine tests. Uses the real docs/03 ids so the
    // behaviour mirrors the kibi-01 route. All strings here are ASCII test data (no shipped content).
    internal static class TestContent
    {
        public const string Wara = "item_wara";
        public const string Abu = "item_abu";
        public const string Toy = "item_abumushi_toy";
        public const string Kibidango = "item_kibidango";

        public const string NpcChild = "npc_child";
        public const string LocA = "loc_a";
        public const string LocB = "loc_b";
        public const string RouteKibi = "route_kibi";

        public static Recipe[] Recipes() => new[]
        {
            new Recipe { Inputs = new[] { Wara, Abu }, Output = Toy },
        };

        public static Npc Child() => new Npc
        {
            Id = NpcChild,
            Name = "child",
            DeclineLines = new[] { "decline_L1", "decline_L2", "decline_L3" },
            HintL2 = "bun_L2",
            HintL3 = "bun_L3",
            Accepts = new[]
            {
                new AcceptRule
                {
                    Item = Toy,
                    ValueForNpc = 5,
                    ReasonLine = "reason",
                    Gives = Kibidango,
                    AcceptLines = new[] { "accept_1" },
                },
            },
        };

        public static StopProgress Prog(int rejections = 0, int deepestHint = 0,
            int questionsUsed = 0, bool cleared = false, List<string> offered = null)
            => new StopProgress
            {
                Rejections = rejections,
                DeepestHint = deepestHint,
                QuestionsUsed = questionsUsed,
                Cleared = cleared,
                OfferedItems = offered ?? new List<string>(),
            };

        public static LoadedContent Content() => new LoadedContent
        {
            Route = new Route
            {
                Id = RouteKibi,
                StartItem = Wara,
                GoalItem = Kibidango,
                Stops = new[] { LocA, LocB },
            },
            Stops = new Dictionary<string, Stop>
            {
                [LocA] = new Stop { Id = LocA, NpcIds = new[] { NpcChild } },
                [LocB] = new Stop { Id = LocB, NpcIds = new string[0] },
            },
            Npcs = new Dictionary<string, Npc> { [NpcChild] = Child() },
            Recipes = Recipes(),
        };

        // Builds a GameState at the given stop with the given inventory. Optionally pre-seeds the
        // current stop's progress (used by immutability tests).
        public static GameState State(int stopIndex = 0, List<string> inventory = null,
            StopProgress currentProg = null)
        {
            var save = new SaveData
            {
                RouteId = RouteKibi,
                StopIndex = stopIndex,
                Inventory = inventory ?? new List<string>(),
            };
            if (currentProg != null)
            {
                var content0 = Content();
                var locId = content0.Route.Stops[stopIndex];
                save.Progress[locId] = currentProg;
            }
            return new GameState(save, Content());
        }
    }
}
