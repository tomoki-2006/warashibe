using NUnit.Framework;
using Warashibe.Core;

namespace Warashibe.Core.Tests
{
    // Covers docs/06 §2 "score.ts" (stop scores + rank boundaries + observation bonus).
    public class ScoreTests
    {
        [Test]
        public void NoQuestionsNoHint_Is100()
        {
            Assert.AreEqual(100, Score.StopScore(TestContent.Prog()));
        }

        [Test]
        public void TwoQuestions_Is70()
        {
            Assert.AreEqual(70, Score.StopScore(TestContent.Prog(questionsUsed: 2)));
        }

        [Test]
        public void L1Only_Is70()
        {
            Assert.AreEqual(70, Score.StopScore(TestContent.Prog(deepestHint: 1)));
        }

        [Test]
        public void L2_Is50()
        {
            Assert.AreEqual(50, Score.StopScore(TestContent.Prog(deepestHint: 2)));
        }

        [Test]
        public void L3_Is20()
        {
            Assert.AreEqual(20, Score.StopScore(TestContent.Prog(deepestHint: 3)));
        }

        [Test]
        public void TwoQuestionsPlusL3_FloorsAt10()
        {
            // 100 - 30 - 80 = -10 -> floored to 10 (never negative).
            Assert.AreEqual(10, Score.StopScore(TestContent.Prog(questionsUsed: 2, deepestHint: 3)));
        }

        [Test]
        public void QuestionPenalty_CapsAt30()
        {
            // 3 questions would be -45 but caps at -30 -> 70.
            Assert.AreEqual(70, Score.StopScore(TestContent.Prog(questionsUsed: 3)));
        }

        [Test]
        public void RankFor_ChojaBoundary_539And540()
        {
            Assert.AreEqual(RouteRank.Daishonin, Score.RankFor(539));
            Assert.AreEqual(RouteRank.Choja, Score.RankFor(540));
        }

        [Test]
        public void RankFor_DaishoninBoundary_419And420()
        {
            Assert.AreEqual(RouteRank.Gyoshonin, Score.RankFor(419));
            Assert.AreEqual(RouteRank.Daishonin, Score.RankFor(420));
        }

        [Test]
        public void RankFor_GyoshoninBoundary_249And250()
        {
            Assert.AreEqual(RouteRank.Minarai, Score.RankFor(249));
            Assert.AreEqual(RouteRank.Gyoshonin, Score.RankFor(250));
        }

        [Test]
        public void TripScore_AddsObservationBonus_PerObservedStop()
        {
            var stops = new[] { 100, 100, 100, 100, 100 }; // 500
            Assert.AreEqual(500, Score.TripScore(stops, 0));
            Assert.AreEqual(520, Score.TripScore(stops, 2)); // +10 x2
        }

        [Test]
        public void ObservationBonus_CanCrossRankBoundary()
        {
            // 530 (大商人) + one observed stop (+10) = 540 -> 長者.
            Assert.AreEqual(RouteRank.Daishonin, Score.RankFor(530));
            Assert.AreEqual(RouteRank.Choja, Score.RankFor(Score.TripScore(new[] { 530 }, 1)));
        }
    }
}
