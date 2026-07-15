using System.Collections.Generic;
using NUnit.Framework;
using Warashibe.Core;

namespace Warashibe.Core.Tests
{
    // Covers docs/06 §2 "progress.ts" (applyOffer / advanceStop / immutability).
    public class ProgressTests
    {
        [Test]
        public void ApplyOffer_Accept_SwapsInventory_ClearsStop_RecordsZukan()
        {
            var state = TestContent.State(inventory: new List<string> { TestContent.Toy });

            var next = Progress.ApplyOffer(state, TestContent.NpcChild, TestContent.Toy);

            // inventory swap: offered item out, gained item in
            CollectionAssert.DoesNotContain(next.Save.Inventory, TestContent.Toy);
            CollectionAssert.Contains(next.Save.Inventory, TestContent.Kibidango);
            // stop cleared (= next stop unlocked in a linear route)
            Assert.IsTrue(next.Save.Progress[TestContent.LocA].Cleared);
            // zukan recorded
            CollectionAssert.Contains(next.Save.ZukanItems, TestContent.Kibidango);
            CollectionAssert.Contains(next.Save.ZukanNpcs, TestContent.NpcChild);
        }

        [Test]
        public void ApplyOffer_Decline_IncrementsRejections_AdvancesHint_RecordsOffered()
        {
            var state = TestContent.State(inventory: new List<string> { TestContent.Wara });

            var next = Progress.ApplyOffer(state, TestContent.NpcChild, TestContent.Wara);

            var prog = next.Save.Progress[TestContent.LocA];
            Assert.AreEqual(1, prog.Rejections);
            Assert.AreEqual(1, prog.DeepestHint);
            CollectionAssert.Contains(prog.OfferedItems, TestContent.Wara);
            Assert.IsFalse(prog.Cleared);
        }

        [Test]
        public void ApplyOffer_Duplicate_NoStateChange()
        {
            var seeded = TestContent.Prog(rejections: 1, deepestHint: 1,
                offered: new List<string> { TestContent.Wara });
            var state = TestContent.State(inventory: new List<string> { TestContent.Wara }, currentProg: seeded);

            var next = Progress.ApplyOffer(state, TestContent.NpcChild, TestContent.Wara);

            var prog = next.Save.Progress[TestContent.LocA];
            Assert.AreEqual(1, prog.Rejections);        // unchanged
            Assert.AreEqual(1, prog.OfferedItems.Count); // not re-added
        }

        [Test]
        public void ApplyOffer_IsImmutable_OriginalStateUnchanged()
        {
            var seeded = TestContent.Prog(); // rejections 0, not cleared
            var state = TestContent.State(inventory: new List<string> { TestContent.Wara }, currentProg: seeded);

            Progress.ApplyOffer(state, TestContent.NpcChild, TestContent.Wara);

            // original save must be untouched (deep clone)
            Assert.AreEqual(0, state.Save.Progress[TestContent.LocA].Rejections);
            Assert.IsFalse(state.Save.Progress[TestContent.LocA].Cleared);
            CollectionAssert.AreEqual(new[] { TestContent.Wara }, state.Save.Inventory);
        }

        [Test]
        public void AdvanceStop_NonFinal_IncrementsIndex()
        {
            var state = TestContent.State(stopIndex: 0); // stops = [loc_a, loc_b]

            var next = Progress.AdvanceStop(state);

            Assert.AreEqual(1, next.Save.StopIndex);
            Assert.IsFalse(next.IsRouteClear);
        }

        [Test]
        public void AdvanceStop_Final_RecordsRouteClear()
        {
            var state = TestContent.State(stopIndex: 1); // last index

            var next = Progress.AdvanceStop(state);

            CollectionAssert.Contains(next.Save.ClearedRoutes, TestContent.RouteKibi);
            Assert.IsTrue(next.IsRouteClear);
            Assert.AreEqual(1, next.Save.StopIndex); // does not run past the end
        }
    }
}
