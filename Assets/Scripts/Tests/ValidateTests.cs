using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Warashibe.Core;

namespace Warashibe.Core.Tests
{
    // Covers docs/06 §2 "validate.ts": kibi-01 loads clean, and each of the docs/03 §9-10 checks
    // detects its corruption. Loads the real StreamingAssets content (Editor path), parses it with
    // Core.ContentParser, then runs Core.Validate — no UnityEngine logic beyond the file path.
    public class ValidateTests
    {
        static LoadedContent LoadRaw()
        {
            string sa = Application.streamingAssetsPath;
            string root = Path.Combine(sa, "routes", "kibi-01");
            string R(string f) => File.ReadAllText(Path.Combine(root, f));
            return ContentParser.Assemble(
                R("items.json"), R("recipes.json"), R("route.json"),
                R("stops.json"), R("npcs.json"), R("events.json"),
                File.ReadAllText(Path.Combine(sa, "strings.ja.json")));
        }

        static bool Has(List<ValidationError> errors, ValidationCode code) => errors.Any(e => e.Code == code);

        [Test]
        public void Kibi01_LoadsAndValidates_WithZeroErrors()
        {
            var errors = Validate.ValidateRoute(LoadRaw());
            Assert.IsEmpty(errors, "unexpected errors: " + string.Join("; ", errors.Select(e => e.ToString())));
        }

        [Test]
        public void Detects_1_StopMissing()
        {
            var c = LoadRaw();
            c.Route.Stops[0] = "loc_ghost";
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.StopMissing));
        }

        [Test]
        public void Detects_2_GoalUnreachable()
        {
            var c = LoadRaw();
            c.Npcs["npc_master"].Accepts[0].Gives = "item_uma"; // yashiki no longer produced
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.GoalUnreachable));
        }

        [Test]
        public void Detects_3_ValueNotAboveBase()
        {
            var c = LoadRaw();
            c.Npcs["npc_child"].Accepts[0].ValueForNpc = 1; // offered toy baseValue is 1
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.ValueNotAboveBase));
        }

        [Test]
        public void Detects_4_NpcShape_DeclineLineCount()
        {
            var c = LoadRaw();
            c.Npcs["npc_child"].DeclineLines = new[] { "a", "b" }; // must be exactly 3
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.NpcShape));
        }

        [Test]
        public void Detects_5_RefMissing()
        {
            var c = LoadRaw();
            c.Npcs["npc_child"].Accepts[0].Gives = "item_ghost";
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.RefMissing));
        }

        [Test]
        public void Detects_6_TextTooLong()
        {
            var c = LoadRaw();
            c.Npcs["npc_grandma"].IdleLine = new string('あ', 41); // > 40 kana
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.TextTooLong));
        }

        [Test]
        public void Detects_6_RubyMissing()
        {
            var c = LoadRaw();
            c.Npcs["npc_grandma"].IdleLine = "これは龍です"; // 龍 outside the ruby-free set
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.RubyMissing));
        }

        [Test]
        public void Detects_6_KatakanaNotAllowed()
        {
            var c = LoadRaw();
            c.Npcs["npc_grandma"].IdleLine = "テレビを みる"; // テレビ not allow-listed
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.KatakanaNotAllowed));
        }

        [Test]
        public void Detects_6_FuriganaMissing()
        {
            var c = LoadRaw();
            c.Items["item_yashiki"].NameRuby = ""; // 屋敷 has kanji but no reading
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.FuriganaMissing));
        }

        [Test]
        public void Detects_7_HighlightMissing()
        {
            var c = LoadRaw();
            c.Npcs["npc_child"].HighlightTarget = "ghost_target";
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.HighlightMissing));
        }

        [Test]
        public void Detects_8_ChainNotMonotonic()
        {
            var c = LoadRaw();
            c.Npcs["npc_merchant"].Accepts[0].Gives = "item_wara"; // kibidango(2) -> wara(1), no postEvent
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.ChainNotMonotonic));
        }

        [Test]
        public void Detects_9_ObservableMissing()
        {
            var c = LoadRaw();
            c.Npcs["npc_merchant"].Observables[0].Target = "";
            Assert.IsTrue(Has(Validate.ValidateRoute(c), ValidationCode.ObservableMissing));
        }
    }
}
