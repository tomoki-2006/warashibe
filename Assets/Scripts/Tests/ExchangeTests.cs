using System.Collections.Generic;
using NUnit.Framework;
using Warashibe.Core;

namespace Warashibe.Core.Tests
{
    // Covers docs/06 §2 "exchange.ts / evaluateOffer".
    public class ExchangeTests
    {
        [Test]
        public void CorrectItem_Accepts_WithGained()
        {
            var result = Exchange.EvaluateOffer(TestContent.Child(), TestContent.Toy, TestContent.Prog());

            Assert.AreEqual(OfferOutcome.Accept, result.Outcome);
            Assert.AreEqual(TestContent.Kibidango, result.Gained);
        }

        [Test]
        public void Accept_KeepsHintLevelUnchanged()
        {
            // deepestHint already at 2 -> accept must not advance it ("現状維持").
            var result = Exchange.EvaluateOffer(TestContent.Child(), TestContent.Toy, TestContent.Prog(deepestHint: 2));

            Assert.AreEqual(OfferOutcome.Accept, result.Outcome);
            Assert.AreEqual(2, result.HintLevelShown);
        }

        [Test]
        public void FirstWrongOffer_Declines_L1()
        {
            var result = Exchange.EvaluateOffer(TestContent.Child(), TestContent.Wara, TestContent.Prog(rejections: 0));

            Assert.AreEqual(OfferOutcome.Decline, result.Outcome);
            Assert.AreEqual(1, result.HintLevelShown);
            CollectionAssert.Contains(result.Lines, "decline_L1");
        }

        [Test]
        public void SecondWrongOffer_Declines_L2()
        {
            var result = Exchange.EvaluateOffer(TestContent.Child(), TestContent.Wara, TestContent.Prog(rejections: 1));

            Assert.AreEqual(2, result.HintLevelShown);
            CollectionAssert.Contains(result.Lines, "decline_L2");
        }

        [Test]
        public void ThirdWrongOffer_Declines_L3()
        {
            var result = Exchange.EvaluateOffer(TestContent.Child(), TestContent.Wara, TestContent.Prog(rejections: 2));

            Assert.AreEqual(3, result.HintLevelShown);
            CollectionAssert.Contains(result.Lines, "decline_L3");
        }

        [Test]
        public void FourthWrongOffer_StaysL3_NoFourthTier()
        {
            var result = Exchange.EvaluateOffer(TestContent.Child(), TestContent.Wara, TestContent.Prog(rejections: 3));

            Assert.AreEqual(3, result.HintLevelShown);
            CollectionAssert.Contains(result.Lines, "decline_L3");
        }

        [Test]
        public void AlreadyOfferedItem_IsDuplicate_NoHintAdvance()
        {
            var prog = TestContent.Prog(rejections: 1, deepestHint: 1,
                offered: new List<string> { TestContent.Wara });

            var result = Exchange.EvaluateOffer(TestContent.Child(), TestContent.Wara, prog);

            Assert.AreEqual(OfferOutcome.Duplicate, result.Outcome);
            Assert.AreEqual(1, result.HintLevelShown); // unchanged
        }

        [Test]
        public void QuestionsUsed_DoesNotAffectOffer()
        {
            var accepted = Exchange.EvaluateOffer(TestContent.Child(), TestContent.Toy, TestContent.Prog(questionsUsed: 2));
            var declined = Exchange.EvaluateOffer(TestContent.Child(), TestContent.Wara, TestContent.Prog(questionsUsed: 2));

            Assert.AreEqual(OfferOutcome.Accept, accepted.Outcome);
            Assert.AreEqual(OfferOutcome.Decline, declined.Outcome);
            Assert.AreEqual(1, declined.HintLevelShown); // driven by rejections, not questions
        }
    }
}
