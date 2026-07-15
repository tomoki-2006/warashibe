using System.Collections.Generic;
using NUnit.Framework;
using Warashibe.Core;

namespace Warashibe.Core.Tests
{
    // Covers docs/03 §7 mini-event parsing (ContentParser.ParseEvents: spec / choices / lines /
    // on_complete + Kind mapping) and the completion transitions (Progress.ApplyEvent).
    public class EventTests
    {
        // ASCII stand-ins for the three docs/03 §7 event shapes (no shipped content in tests).
        const string EventsJson = @"[
          { ""id"": ""ev_tap"", ""type"": ""tap_catch"", ""trigger"": ""always"",
            ""spec"": { ""taps_required"": 3, ""hitbox_scale"": 2.0, ""path"": ""figure8_slow"", ""slowdown_per_tap"": 0.35 },
            ""lines_on_success"": [""a"", ""b""], ""gives"": ""item_x"" },
          { ""id"": ""ev_choice"", ""type"": ""map_choice"",
            ""spec"": { ""prompt"": ""p"", ""retry_until_correct"": true,
              ""choices"": [ { ""label"": ""L1"", ""correct"": true, ""result"": ""r1"" },
                             { ""label"": ""L2"", ""correct"": false, ""result"": ""r2"" } ] },
            ""on_complete"": { ""replace_item"": [""item_from"", ""item_to""], ""lines"": [""c"", ""d""] } },
          { ""id"": ""ev_cut"", ""type"": ""cutscene"",
            ""spec"": { ""duration_ms"": 3000, ""skippable_after_ms"": 800, ""visual"": ""v"", ""line"": ""l"" },
            ""on_complete"": { ""advance_to"": ""loc_b"" } }
        ]";

        static Dictionary<string, Event> Parsed()
        {
            var map = new Dictionary<string, Event>();
            foreach (var ev in ContentParser.ParseEvents(EventsJson)) map[ev.Id] = ev;
            return map;
        }

        [Test]
        public void ParseEvents_TapCatch_SpecAndLines()
        {
            var ev = Parsed()["ev_tap"];
            Assert.AreEqual(EventKind.TapCatch, ev.Kind);
            Assert.AreEqual(3, ev.Spec.TapsRequired);
            Assert.AreEqual(2.0f, ev.Spec.HitboxScale, 1e-4f);
            Assert.AreEqual("figure8_slow", ev.Spec.Path);
            Assert.AreEqual(0.35f, ev.Spec.SlowdownPerTap, 1e-4f);
            Assert.AreEqual(2, ev.LinesOnSuccess.Length);
            Assert.AreEqual("item_x", ev.Gives);
        }

        [Test]
        public void ParseEvents_MapChoice_ChoicesAndOnComplete()
        {
            var ev = Parsed()["ev_choice"];
            Assert.AreEqual(EventKind.MapChoice, ev.Kind);
            Assert.AreEqual("p", ev.Spec.Prompt);
            Assert.IsTrue(ev.Spec.RetryUntilCorrect);
            Assert.AreEqual(2, ev.Spec.Choices.Length);
            Assert.IsTrue(ev.Spec.Choices[0].Correct);
            Assert.IsFalse(ev.Spec.Choices[1].Correct);
            Assert.AreEqual("r2", ev.Spec.Choices[1].Result);
            CollectionAssert.AreEqual(new[] { "item_from", "item_to" }, ev.OnComplete.ReplaceItem);
            Assert.AreEqual(2, ev.OnComplete.Lines.Length);
        }

        [Test]
        public void ParseEvents_Cutscene_Timing()
        {
            var ev = Parsed()["ev_cut"];
            Assert.AreEqual(EventKind.Cutscene, ev.Kind);
            Assert.AreEqual(3000, ev.Spec.DurationMs);
            Assert.AreEqual(800, ev.Spec.SkippableAfterMs);
            Assert.AreEqual("loc_b", ev.OnComplete.AdvanceTo);
        }

        [Test]
        public void ApplyEvent_Gives_AddsItemAndZukan_Immutable()
        {
            var state = TestContent.State(inventory: new List<string> { TestContent.Wara });
            var ev = new Event { Id = "e", Type = "tap_catch", Gives = TestContent.Abu };

            var next = Progress.ApplyEvent(state, ev);

            CollectionAssert.Contains(next.Save.Inventory, TestContent.Abu);
            CollectionAssert.Contains(next.Save.ZukanItems, TestContent.Abu);
            CollectionAssert.DoesNotContain(state.Save.Inventory, TestContent.Abu); // original untouched
        }

        [Test]
        public void ApplyEvent_Replace_SwapsInPlace_RecordsZukan()
        {
            var state = TestContent.State(inventory: new List<string> { TestContent.Wara, TestContent.Kibidango });
            var ev = new Event
            {
                Id = "e", Type = "map_choice",
                OnComplete = new EventOnComplete { ReplaceItem = new[] { TestContent.Wara, TestContent.Toy } },
            };

            var next = Progress.ApplyEvent(state, ev);

            CollectionAssert.AreEqual(new[] { TestContent.Toy, TestContent.Kibidango }, next.Save.Inventory); // in place
            CollectionAssert.Contains(next.Save.ZukanItems, TestContent.Toy);
        }

        [Test]
        public void ApplyEvent_AdvanceTo_SetsStopIndex()
        {
            var state = TestContent.State(stopIndex: 0); // stops = [loc_a, loc_b]
            var ev = new Event
            {
                Id = "e", Type = "cutscene",
                OnComplete = new EventOnComplete { AdvanceTo = TestContent.LocB },
            };

            var next = Progress.ApplyEvent(state, ev);

            Assert.AreEqual(1, next.Save.StopIndex);
        }

        [Test]
        public void ApplyEvent_AdvanceTo_UnknownLoc_NoChange()
        {
            var state = TestContent.State(stopIndex: 0);
            var ev = new Event
            {
                Id = "e", Type = "cutscene",
                OnComplete = new EventOnComplete { AdvanceTo = "loc_unknown" },
            };

            var next = Progress.ApplyEvent(state, ev);

            Assert.AreEqual(0, next.Save.StopIndex);
        }

        [Test]
        public void UnknownType_MapsToUnknownKind()
        {
            Assert.AreEqual(EventKind.Unknown, new Event { Type = "mystery" }.Kind);
        }
    }
}
