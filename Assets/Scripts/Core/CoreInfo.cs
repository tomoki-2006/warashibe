namespace Warashibe.Core
{
    /// <summary>
    /// Assembly marker for Warashibe.Core.
    /// Pure C# only: no UnityEngine reference (enforced structurally by
    /// Warashibe.Core.asmdef "noEngineReferences": true).
    /// Domain types (Types/Exchange/Recipe/Score/Progress/Validate) are ported in T-U04.
    /// </summary>
    public static class CoreInfo
    {
        /// <summary>SaveData / content-schema generation this Core targets (see docs/02 SaveData v1).</summary>
        public const string SchemaVersion = "1";
    }
}
