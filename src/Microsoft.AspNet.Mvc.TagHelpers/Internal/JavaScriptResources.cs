// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Methods for loading JavaScript from assembly embedded resources.
    /// </summary>
    public static class JavaScriptResources
    {
        private static readonly Assembly ResourcesAssembly = typeof(JavaScriptResources).GetTypeInfo().Assembly;

        private static readonly ConcurrentDictionary<string, string> Cache =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets an embedded JavaScript file resource and decodes it for use as a .NET format string.
        /// </summary>
        public static string GetEmbeddedJavaScript(string resourceName)
        {
            return GetEmbeddedJavaScript(resourceName, ResourcesAssembly.GetManifestResourceStream, Cache);
        }

        // Internal for testing
        internal static string GetEmbeddedJavaScript(
            string resourceName,
            Func<string, Stream> getManifestResourceStream,
            ConcurrentDictionary<string, string> cache)
        {
            return cache.GetOrAdd(resourceName, key =>
            {
                // Load the JavaScript from embedded resource
                using (var resourceStream = getManifestResourceStream(key))
                {
                    Debug.Assert(resourceStream != null, "Embedded resource missing. Ensure 'prebuild' script has run.");

                    using (var streamReader = new StreamReader(resourceStream))
                    {
                        var script = streamReader.ReadToEnd();

                        return PrepareFormatString(script);
                    }
                }
            });
        }
        
        private static string PrepareFormatString(string input)
        {
            // Replace unescaped/escaped chars with their equivalent
            return input.Replace("{", "{{")
                        .Replace("}", "}}")
                        .Replace("[[[", "{")
                        .Replace("]]]", "}");
        }
    }
}