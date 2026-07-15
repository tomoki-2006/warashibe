using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Warashibe.Core
{
    // Ported from docs/02 §3 validate.ts. Runs the docs/03 §9-10 integrity checks against a
    // LoadedContent and returns every problem found (empty list = valid). Pure, engine-free.
    //
    // Deviations from the docs/02 signature (documented in the T-U05 PR):
    //  - Operates on a single LoadedContent instead of loose (route, stops, npcs, items, recipes);
    //    events are needed for checks 2/5/7 so they are included.
    //  - portrait/bg asset existence (part of §9 check 5) is an asset concern, verified in the
    //    Game layer, not here — TODO(T-U06+).
    //  - The ruby / katakana allow-lists are seeded with the grade-appropriate characters used in
    //    kibi-01; extend from the full 教育漢字 / katakana lists as content grows — TODO(content).
    public static class Validate
    {
        public const int MaxSentenceLen = 40; // docs/03 §9-6: one sentence <= 40 kana/kanji

        // Grade 1-3 kanji that appear (ruby-free) in kibi-01 dialogue. TODO(content): full list.
        static readonly HashSet<char> RubyFreeKanji = new HashSet<char>("子空見");
        // Katakana words allowed without flagging. TODO(content): full allow-list.
        static readonly HashSet<string> KatakanaAllow = new HashSet<string> { "アブ" };
        // Katakana letters excluding ー(U+30FC), ・(U+30FB), ゠(U+30A0) — those aren't "words".
        static readonly Regex KatakanaRun = new Regex(@"[ァ-ヺヽヾ]+");

        public static List<ValidationError> ValidateRoute(LoadedContent c)
        {
            var errors = new List<ValidationError>();
            if (c == null)
            {
                errors.Add(new ValidationError(ValidationCode.RefMissing, "content", "content is null"));
                return errors;
            }
            var route = c.Route;

            void ReqItem(string id, string where)
            {
                if (!string.IsNullOrEmpty(id) && !c.Items.ContainsKey(id))
                    errors.Add(new ValidationError(ValidationCode.RefMissing, where, "unknown item id: " + id));
            }
            void ReqEvent(string id, string where)
            {
                if (!string.IsNullOrEmpty(id) && !c.Events.ContainsKey(id))
                    errors.Add(new ValidationError(ValidationCode.RefMissing, where, "unknown event id: " + id));
            }
            void ReqNpc(string id, string where)
            {
                if (!string.IsNullOrEmpty(id) && !c.Npcs.ContainsKey(id))
                    errors.Add(new ValidationError(ValidationCode.RefMissing, where, "unknown npc id: " + id));
            }

            // Check 1: route.stops all exist.
            if (route?.Stops != null)
                foreach (var loc in route.Stops)
                    if (!c.Stops.ContainsKey(loc))
                        errors.Add(new ValidationError(ValidationCode.StopMissing, loc, "route.stops references an unknown stop"));

            // Check 5: referenced ids exist (route/stops/recipes).
            if (route != null)
            {
                ReqItem(route.StartItem, "route.startItem");
                ReqItem(route.GoalItem, "route.goalItem");
            }
            foreach (var st in c.Stops.Values)
            {
                if (st.NpcIds != null)
                    foreach (var n in st.NpcIds) ReqNpc(n, st.Id + ".npcIds");
                ReqEvent(st.AmbientEvent, st.Id + ".ambientEvent");
            }
            foreach (var r in c.Recipes ?? new Recipe[0])
            {
                if (r?.Inputs != null)
                    foreach (var inp in r.Inputs) ReqItem(inp, "recipe.inputs");
                ReqItem(r?.Output, "recipe.output");
            }

            // Per-NPC checks (4 shape, 3 value, 5 refs, 7 highlight, 8 monotonic, 9 observable).
            foreach (var npc in c.Npcs.Values)
            {
                bool trades = npc.Accepts != null && npc.Accepts.Length > 0;

                // Check 4: NPC shape.
                if (npc.Questions != null && npc.Questions.Length > 2)
                    errors.Add(new ValidationError(ValidationCode.NpcShape, npc.Id, "more than 2 questions"));
                if (trades)
                {
                    if (npc.DeclineLines == null || npc.DeclineLines.Length != 3)
                        errors.Add(new ValidationError(ValidationCode.NpcShape, npc.Id, "declineLines must have exactly 3 entries"));
                    if (string.IsNullOrEmpty(npc.HintL2) || string.IsNullOrEmpty(npc.HintL3))
                        errors.Add(new ValidationError(ValidationCode.NpcShape, npc.Id, "hintL2/hintL3 must be non-empty for a trading NPC"));
                }

                if (npc.Accepts != null)
                    foreach (var rule in npc.Accepts)
                    {
                        if (rule == null) continue;
                        ReqItem(rule.Item, npc.Id + ".accept.item");
                        ReqItem(rule.Gives, npc.Id + ".accept.gives");
                        ReqEvent(rule.PostEvent, npc.Id + ".accept.postEvent");

                        // Check 3: valueForNpc > offered item's baseValue.
                        if (c.Items.TryGetValue(rule.Item, out var offered) && rule.ValueForNpc <= offered.BaseValue)
                            errors.Add(new ValidationError(ValidationCode.ValueNotAboveBase, npc.Id,
                                "valueForNpc must exceed offered baseValue"));

                        // Check 8: value stairs must not drop, unless a postEvent upgrades the item (half-step).
                        if (string.IsNullOrEmpty(rule.PostEvent)
                            && c.Items.TryGetValue(rule.Item, out var from)
                            && c.Items.TryGetValue(rule.Gives, out var to)
                            && to.BaseValue < from.BaseValue)
                            errors.Add(new ValidationError(ValidationCode.ChainNotMonotonic, npc.Id,
                                "trade lowers the value stairs without a postEvent"));
                    }

                // Check 7: highlightTarget resolves to an item or an event.
                if (!string.IsNullOrEmpty(npc.HighlightTarget)
                    && !c.Items.ContainsKey(npc.HighlightTarget) && !c.Events.ContainsKey(npc.HighlightTarget))
                    errors.Add(new ValidationError(ValidationCode.HighlightMissing, npc.Id,
                        "highlightTarget is neither item nor event: " + npc.HighlightTarget));

                // Check 9: observable target is present.
                if (npc.Observables != null)
                    foreach (var obs in npc.Observables)
                        if (obs == null || string.IsNullOrEmpty(obs.Target))
                            errors.Add(new ValidationError(ValidationCode.ObservableMissing, npc.Id, "observable target is empty"));
            }

            // Check 2: goalItem reachable from startItem via recipes / accepts / event outputs.
            if (route != null && !string.IsNullOrEmpty(route.GoalItem))
            {
                var reachable = new HashSet<string>();
                if (!string.IsNullOrEmpty(route.StartItem)) reachable.Add(route.StartItem);
                foreach (var ev in c.Events.Values)
                    if (!string.IsNullOrEmpty(ev.Gives)) reachable.Add(ev.Gives);

                bool changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (var r in c.Recipes ?? new Recipe[0])
                    {
                        if (r?.Inputs == null || string.IsNullOrEmpty(r.Output) || reachable.Contains(r.Output)) continue;
                        bool all = true;
                        foreach (var inp in r.Inputs) if (!reachable.Contains(inp)) { all = false; break; }
                        if (all) { reachable.Add(r.Output); changed = true; }
                    }
                    foreach (var npc in c.Npcs.Values)
                        if (npc.Accepts != null)
                            foreach (var rule in npc.Accepts)
                                if (rule != null && !string.IsNullOrEmpty(rule.Gives)
                                    && reachable.Contains(rule.Item) && !reachable.Contains(rule.Gives))
                                { reachable.Add(rule.Gives); changed = true; }
                    foreach (var ev in c.Events.Values)
                    {
                        var rep = ev.OnComplete?.ReplaceItem;
                        if (rep != null && rep.Length == 2 && reachable.Contains(rep[0]) && !reachable.Contains(rep[1]))
                        { reachable.Add(rep[1]); changed = true; }
                    }
                }
                if (!reachable.Contains(route.GoalItem))
                    errors.Add(new ValidationError(ValidationCode.GoalUnreachable, route.GoalItem,
                        "goalItem is not reachable from startItem"));
            }

            // Check 6: text — furigana on item names, then length / ruby / katakana on dialogue.
            foreach (var it in c.Items.Values)
                if (HasKanji(it.Name) && string.IsNullOrEmpty(it.NameRuby))
                    errors.Add(new ValidationError(ValidationCode.FuriganaMissing, it.Id,
                        "item name has kanji but name_ruby is empty"));

            foreach (var pair in DialogueStrings(c))
            {
                var where = pair.Key;
                var text = pair.Value;
                if (string.IsNullOrEmpty(text)) continue;

                foreach (var sentence in text.Split('。', '！', '？', '\n'))
                    if (KanaKanjiLength(sentence) > MaxSentenceLen)
                    {
                        errors.Add(new ValidationError(ValidationCode.TextTooLong, where,
                            "sentence exceeds " + MaxSentenceLen + " chars"));
                        break;
                    }

                foreach (var ch in text)
                    if (IsKanji(ch) && !RubyFreeKanji.Contains(ch))
                    {
                        errors.Add(new ValidationError(ValidationCode.RubyMissing, where, "kanji needs ruby: " + ch));
                        break;
                    }

                foreach (Match m in KatakanaRun.Matches(text))
                    if (!KatakanaAllow.Contains(m.Value))
                    {
                        errors.Add(new ValidationError(ValidationCode.KatakanaNotAllowed, where, "katakana not allowed: " + m.Value));
                        break;
                    }
            }

            return errors;
        }

        // Player-facing dialogue fields (name/label fields are excluded from ruby/length scanning).
        static IEnumerable<KeyValuePair<string, string>> DialogueStrings(LoadedContent c)
        {
            foreach (var npc in c.Npcs.Values)
            {
                var w = npc.Id;
                if (npc.Intro != null) foreach (var s in npc.Intro) yield return Pair(w, s);
                yield return Pair(w, npc.IdleLine);
                if (npc.Questions != null)
                    foreach (var q in npc.Questions)
                    {
                        if (q == null) continue;
                        yield return Pair(w, q.Q);
                        yield return Pair(w, q.A);
                    }
                if (npc.Accepts != null)
                    foreach (var a in npc.Accepts)
                    {
                        if (a == null) continue;
                        yield return Pair(w, a.ReasonLine);
                        if (a.AcceptLines != null) foreach (var s in a.AcceptLines) yield return Pair(w, s);
                    }
                if (npc.DeclineLines != null) foreach (var s in npc.DeclineLines) yield return Pair(w, s);
                yield return Pair(w, npc.HintL2);
                yield return Pair(w, npc.HintL3);
                yield return Pair(w, npc.AfterTradeLine);
                if (npc.Observables != null)
                    foreach (var o in npc.Observables)
                        if (o != null) yield return Pair(w, o.BunLine);
            }
            if (c.Strings != null)
                foreach (var kv in c.Strings) yield return Pair("strings." + kv.Key, kv.Value);
        }

        static KeyValuePair<string, string> Pair(string where, string text) =>
            new KeyValuePair<string, string>(where, text);

        static bool IsKanji(char ch) => ch >= '一' && ch <= '鿿';
        static bool IsHiragana(char ch) => ch >= 'ぁ' && ch <= 'ゟ';
        static bool IsKatakana(char ch) => ch >= '゠' && ch <= 'ヿ';

        static bool HasKanji(string s)
        {
            if (s == null) return false;
            foreach (var ch in s) if (IsKanji(ch)) return true;
            return false;
        }

        static int KanaKanjiLength(string s)
        {
            int n = 0;
            foreach (var ch in s) if (IsHiragana(ch) || IsKatakana(ch) || IsKanji(ch)) n++;
            return n;
        }
    }
}
