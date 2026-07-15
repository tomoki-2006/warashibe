using System.Collections.Generic;
using NUnit.Framework;
using Warashibe.Core;

namespace Warashibe.Core.Tests
{
    // Exercises argument guards and defensive/alternate branches so the four Core modules are
    // fully covered (docs/06 §2 AC: カバレッジ100%). Behavioural cases live in the per-module files.
    public class CoreGuardTests
    {
        // ---- Exchange ----

        [Test]
        public void EvaluateOffer_NullNpc_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => Exchange.EvaluateOffer(null, TestContent.Toy, TestContent.Prog()));
        }

        [Test]
        public void EvaluateOffer_NullProg_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => Exchange.EvaluateOffer(TestContent.Child(), TestContent.Toy, null));
        }

        [Test]
        public void EvaluateOffer_NpcWithNullAccepts_Declines()
        {
            var npc = new Npc { Id = "npc_x", DeclineLines = new[] { "d1", "d2", "d3" } };

            var result = Exchange.EvaluateOffer(npc, TestContent.Toy, TestContent.Prog());

            Assert.AreEqual(OfferOutcome.Decline, result.Outcome);
            Assert.AreEqual(1, result.HintLevelShown);
        }

        [Test]
        public void EvaluateOffer_AcceptWithNullAcceptLines_ReturnsEmptyLines()
        {
            var npc = new Npc
            {
                Id = "npc_x",
                Accepts = new[] { new AcceptRule { Item = TestContent.Toy, Gives = TestContent.Kibidango } },
            };

            var result = Exchange.EvaluateOffer(npc, TestContent.Toy, TestContent.Prog());

            Assert.AreEqual(OfferOutcome.Accept, result.Outcome);
            Assert.IsNotNull(result.Lines);
            Assert.IsEmpty(result.Lines);
        }

        [Test]
        public void EvaluateOffer_DeclineWithNullDeclineLines_ReturnsEmptyLines()
        {
            var npc = new Npc { Id = "npc_x" }; // no accepts, no decline lines

            var result = Exchange.EvaluateOffer(npc, TestContent.Wara, TestContent.Prog());

            Assert.AreEqual(OfferOutcome.Decline, result.Outcome);
            Assert.AreEqual(1, result.HintLevelShown);
            Assert.IsEmpty(result.Lines);
        }

        [Test]
        public void EvaluateOffer_AcceptsContainingNullRule_SkipsItAndMatchesValid()
        {
            var npc = new Npc
            {
                Id = "npc_x",
                Accepts = new[]
                {
                    null,
                    new AcceptRule { Item = TestContent.Toy, Gives = TestContent.Kibidango },
                },
            };

            var result = Exchange.EvaluateOffer(npc, TestContent.Toy, TestContent.Prog());

            Assert.AreEqual(OfferOutcome.Accept, result.Outcome);
            Assert.AreEqual(TestContent.Kibidango, result.Gained);
        }

        [Test]
        public void EvaluateOffer_DeclineLinesShorterThanLevel_ReturnsEmptyLines()
        {
            var npc = new Npc { Id = "npc_x", DeclineLines = new[] { "only_one" } };

            // rejections 2 -> level 3, but only one decline line exists -> no line, no out-of-range.
            var result = Exchange.EvaluateOffer(npc, TestContent.Wara, TestContent.Prog(rejections: 2));

            Assert.AreEqual(OfferOutcome.Decline, result.Outcome);
            Assert.AreEqual(3, result.HintLevelShown);
            Assert.IsEmpty(result.Lines);
        }

        // ---- Recipes ----

        [Test]
        public void Combine_NullRecipes_ReturnsNull()
        {
            Assert.IsNull(Recipes.Combine(TestContent.Wara, TestContent.Abu, null));
        }

        [Test]
        public void Combine_SkipsNullAndMalformedRecipes_StillMatchesValidOne()
        {
            var recipes = new[]
            {
                null,
                new Recipe { Inputs = null, Output = "x" },
                new Recipe { Inputs = new[] { TestContent.Wara }, Output = "x" }, // wrong length
                new Recipe { Inputs = new[] { TestContent.Wara, TestContent.Abu }, Output = TestContent.Toy },
            };

            Assert.AreEqual(TestContent.Toy, Recipes.Combine(TestContent.Wara, TestContent.Abu, recipes));
        }

        // ---- Score ----

        [Test]
        public void StopScore_NullProg_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => Score.StopScore(null));
        }

        [Test]
        public void TripScore_NullStopScores_ReturnsBonusOnly()
        {
            Assert.AreEqual(20, Score.TripScore(null, 2)); // 0 + 10 x2
        }

        // ---- Progress ----

        [Test]
        public void ApplyOffer_NullState_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => Progress.ApplyOffer(null, TestContent.NpcChild, TestContent.Toy));
        }

        [Test]
        public void AdvanceStop_NullState_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => Progress.AdvanceStop(null));
        }

        [Test]
        public void ApplyOffer_ReAccept_IsIdempotent_NoDuplicates()
        {
            var state = TestContent.State(inventory: new List<string> { TestContent.Toy });

            var once = Progress.ApplyOffer(state, TestContent.NpcChild, TestContent.Toy);
            var twice = Progress.ApplyOffer(once, TestContent.NpcChild, TestContent.Toy);

            Assert.AreEqual(1, twice.Save.Inventory.Count);
            CollectionAssert.Contains(twice.Save.Inventory, TestContent.Kibidango);
            Assert.AreEqual(1, twice.Save.ZukanItems.Count);
            Assert.AreEqual(1, twice.Save.ZukanNpcs.Count);
            Assert.AreEqual(1, twice.Save.Progress[TestContent.LocA].OfferedItems.Count);
        }

        [Test]
        public void ApplyOffer_AcceptWithEmptyGives_NoInventoryOrZukanAdd()
        {
            var npc = new Npc
            {
                Id = "npc_x",
                Accepts = new[] { new AcceptRule { Item = TestContent.Wara, Gives = "" } },
            };
            var content = new LoadedContent
            {
                Route = new Route { Id = "route_x", Stops = new[] { "loc_x" } },
                Npcs = new Dictionary<string, Npc> { ["npc_x"] = npc },
            };
            var save = new SaveData
            {
                RouteId = "route_x",
                StopIndex = 0,
                Inventory = new List<string> { TestContent.Wara },
            };
            var state = new GameState(save, content);

            var next = Progress.ApplyOffer(state, "npc_x", TestContent.Wara);

            Assert.IsEmpty(next.Save.Inventory);      // offered item removed, nothing gained
            Assert.IsEmpty(next.Save.ZukanItems);     // empty gives -> not recorded
            Assert.IsTrue(next.Save.Progress["loc_x"].Cleared);
        }

        [Test]
        public void ApplyOffer_DeclineAtCappedL3_DoesNotLowerDeepestHint()
        {
            var seeded = TestContent.Prog(rejections: 3, deepestHint: 3,
                offered: new List<string> { TestContent.Kibidango });
            var state = TestContent.State(inventory: new List<string> { TestContent.Abu }, currentProg: seeded);

            // Offering a NEW wrong item declines at level 3 (capped); deepest hint must stay 3.
            var next = Progress.ApplyOffer(state, TestContent.NpcChild, TestContent.Abu);

            var prog = next.Save.Progress[TestContent.LocA];
            Assert.AreEqual(4, prog.Rejections);
            Assert.AreEqual(3, prog.DeepestHint);
        }

        [Test]
        public void AdvanceStop_FinalTwice_IsIdempotent()
        {
            var state = TestContent.State(stopIndex: 1); // last index

            var once = Progress.AdvanceStop(state);
            var twice = Progress.AdvanceStop(once);

            Assert.AreEqual(1, twice.Save.ClearedRoutes.Count); // routeId not duplicated
            Assert.IsTrue(twice.IsRouteClear);
        }
    }
}
