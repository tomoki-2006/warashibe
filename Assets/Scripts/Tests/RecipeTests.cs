using NUnit.Framework;
using Warashibe.Core;

namespace Warashibe.Core.Tests
{
    // Covers docs/06 §2 "recipe.ts / combine".
    public class RecipeTests
    {
        [Test]
        public void WaraPlusAbu_MakesToy()
        {
            Assert.AreEqual(TestContent.Toy,
                Recipes.Combine(TestContent.Wara, TestContent.Abu, TestContent.Recipes()));
        }

        [Test]
        public void AbuPlusWara_MakesToy_OrderIndependent()
        {
            Assert.AreEqual(TestContent.Toy,
                Recipes.Combine(TestContent.Abu, TestContent.Wara, TestContent.Recipes()));
        }

        [Test]
        public void MismatchedPair_ReturnsNull()
        {
            Assert.IsNull(Recipes.Combine(TestContent.Wara, TestContent.Kibidango, TestContent.Recipes()));
        }

        [Test]
        public void SameItemTwice_ReturnsNull()
        {
            Assert.IsNull(Recipes.Combine(TestContent.Wara, TestContent.Wara, TestContent.Recipes()));
        }

        [Test]
        public void NonexistentId_ReturnsNull()
        {
            Assert.IsNull(Recipes.Combine("item_does_not_exist", TestContent.Abu, TestContent.Recipes()));
        }
    }
}
