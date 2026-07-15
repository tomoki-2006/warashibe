using System;
using System.IO;
using System.Text;
using UnityEngine;
using Warashibe.Core;

namespace Warashibe.Game
{
    // Reads a route's content from StreamingAssets, parses it (Warashibe.Core.ContentParser) and
    // validates it (Warashibe.Core.Validate). File I/O is the only Game-layer concern here; all
    // parsing/validation logic stays in engine-free Core.
    //
    // NOTE: uses System.IO, which works in the Editor and on desktop/standalone. WebGL cannot read
    // StreamingAssets synchronously — an async UnityWebRequest path is added in T-U10 (WebGL build).
    public static class StreamingContentLoader
    {
        public const string DefaultRouteFolder = "kibi-01";

        /// <summary>
        /// Load, parse and validate a route. Per docs/02 §9, content validation errors throw in
        /// development (fail fast) rather than shipping broken data.
        /// </summary>
        public static LoadedContent LoadRoute(string routeFolder = DefaultRouteFolder)
        {
            var content = LoadRouteUnvalidated(routeFolder);
            var errors = Validate.ValidateRoute(content);
            if (errors.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("Content validation failed (").Append(errors.Count).Append(" issue(s)):");
                foreach (var e in errors) sb.Append("\n  ").Append(e);
                throw new Exception(sb.ToString());
            }
            return content;
        }

        /// <summary>Load and parse without validating (used by tests that inject corrupt data).</summary>
        public static LoadedContent LoadRouteUnvalidated(string routeFolder = DefaultRouteFolder)
        {
            string root = Path.Combine(Application.streamingAssetsPath, "routes", routeFolder);
            return ContentParser.Assemble(
                ReadText(Path.Combine(root, "items.json")),
                ReadText(Path.Combine(root, "recipes.json")),
                ReadText(Path.Combine(root, "route.json")),
                ReadText(Path.Combine(root, "stops.json")),
                ReadText(Path.Combine(root, "npcs.json")),
                ReadText(Path.Combine(root, "events.json")),
                ReadText(Path.Combine(Application.streamingAssetsPath, "strings.ja.json")));
        }

        static string ReadText(string path) => File.ReadAllText(path, Encoding.UTF8);
    }
}
