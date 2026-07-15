using NUnit.Framework;
using Warashibe.Core;

namespace Warashibe.Core.Tests
{
    /// <summary>
    /// Skeleton smoke test proving the Tests -> Core assembly wiring compiles and runs green.
    /// Real domain coverage (06 §2, Core 100%) lands in T-U04.
    /// </summary>
    public class CoreSmokeTests
    {
        [Test]
        public void CoreInfo_SchemaVersion_IsOne()
        {
            Assert.AreEqual("1", CoreInfo.SchemaVersion);
        }
    }
}
