namespace Warashibe.Core
{
    // Ported from docs/02 §3 recipe.ts / combine. Rules: docs/01 §2.2.
    // Phase 0 has exactly one recipe: item_wara + item_abu = item_abumushi_toy.
    public static class Recipes
    {
        /// <summary>
        /// Returns the output ItemId if (<paramref name="a"/>, <paramref name="b"/>) matches a
        /// recipe's inputs (order-independent), otherwise null. Two identical items or an
        /// unknown id never match, because no recipe has identical inputs.
        /// </summary>
        public static string Combine(string a, string b, Recipe[] recipes)
        {
            if (recipes == null) return null;
            foreach (var r in recipes)
            {
                if (r == null || r.Inputs == null || r.Inputs.Length != 2) continue;
                var x = r.Inputs[0];
                var y = r.Inputs[1];
                if ((x == a && y == b) || (x == b && y == a)) return r.Output;
            }
            return null;
        }
    }
}
