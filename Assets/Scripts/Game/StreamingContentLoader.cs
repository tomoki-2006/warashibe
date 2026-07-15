using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Warashibe.Core;

namespace Warashibe.Game
{
    // Reads a route's content from StreamingAssets, parses it (Warashibe.Core.ContentParser) and
    // validates it (Warashibe.Core.Validate). File I/O is the only Game-layer concern here; all
    // parsing/validation logic stays in engine-free Core.
    //
    // Two paths, same parse/validate:
    //  • LoadRoute / LoadRouteUnvalidated — synchronous System.IO. Editor, desktop/standalone and
    //    EditMode tests only. WebGL CANNOT read StreamingAssets synchronously.
    //  • LoadRouteRoutine — asynchronous UnityWebRequest (T-U10). Works everywhere, and is the ONLY
    //    path that works on WebGL (there StreamingAssets is served over HTTP, not a local file).
    public static class StreamingContentLoader
    {
        public const string DefaultRouteFolder = "kibi-01";

        // The seven content files a route is assembled from (route folder + the shared strings file).
        static readonly string[] RouteFiles =
            { "items.json", "recipes.json", "route.json", "stops.json", "npcs.json", "events.json" };
        const string StringsFile = "strings.ja.json";

        /// <summary>
        /// Load, parse and validate a route. Per docs/02 §9, content validation errors throw in
        /// development (fail fast) rather than shipping broken data.
        /// </summary>
        public static LoadedContent LoadRoute(string routeFolder = DefaultRouteFolder)
        {
            var content = LoadRouteUnvalidated(routeFolder);
            ThrowIfInvalid(content);
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
                ReadText(Path.Combine(Application.streamingAssetsPath, StringsFile)));
        }

        /// <summary>
        /// WebGL-safe async load: fetch every content file with UnityWebRequest, then parse + validate
        /// with the same Core code. Invokes <paramref name="onLoaded"/> with validated content, or
        /// <paramref name="onError"/> with the first failure (fetch, parse or validation).
        /// Also works in the Editor and on desktop (file:// scheme is added for local paths).
        /// </summary>
        public static IEnumerator LoadRouteRoutine(string routeFolder,
            Action<LoadedContent> onLoaded, Action<Exception> onError = null)
        {
            routeFolder = string.IsNullOrEmpty(routeFolder) ? DefaultRouteFolder : routeFolder;
            string root = CombineUrl(Application.streamingAssetsPath, "routes/" + routeFolder);

            var texts = new Dictionary<string, string>();
            foreach (var file in RouteFiles)
            {
                string text = null;
                Exception err = null;
                yield return FetchText(CombineUrl(root, file), t => text = t, e => err = e);
                if (err != null) { onError?.Invoke(err); yield break; }
                texts[file] = text;
            }

            string stringsText = null;
            Exception stringsErr = null;
            yield return FetchText(CombineUrl(Application.streamingAssetsPath, StringsFile),
                t => stringsText = t, e => stringsErr = e);
            if (stringsErr != null) { onError?.Invoke(stringsErr); yield break; }

            LoadedContent content;
            try
            {
                content = ContentParser.Assemble(texts["items.json"], texts["recipes.json"],
                    texts["route.json"], texts["stops.json"], texts["npcs.json"], texts["events.json"],
                    stringsText);
                ThrowIfInvalid(content);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                yield break;
            }

            onLoaded?.Invoke(content);
        }

        // ---- helpers ----

        static void ThrowIfInvalid(LoadedContent content)
        {
            var errors = Validate.ValidateRoute(content);
            if (errors.Count == 0) return;
            var sb = new StringBuilder();
            sb.Append("Content validation failed (").Append(errors.Count).Append(" issue(s)):");
            foreach (var e in errors) sb.Append("\n  ").Append(e);
            throw new Exception(sb.ToString());
        }

        static IEnumerator FetchText(string uri, Action<string> onOk, Action<Exception> onErr)
        {
            using (var req = UnityWebRequest.Get(ToUri(uri)))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                    onErr(new Exception("failed to load " + uri + ": " + req.error));
                else
                    onOk(req.downloadHandler.text);
            }
        }

        // StreamingAssets is a URL on WebGL/Android (has a scheme already); on desktop/editor it is a
        // bare filesystem path, which UnityWebRequest needs as a file:// URI.
        static string ToUri(string path) => path.Contains("://") ? path : "file://" + path;

        static string CombineUrl(string a, string b) => a.TrimEnd('/') + "/" + b.TrimStart('/');

        static string ReadText(string path) => File.ReadAllText(path, Encoding.UTF8);
    }
}
