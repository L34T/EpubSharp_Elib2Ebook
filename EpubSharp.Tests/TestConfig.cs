#nullable enable
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EpubSharp.Tests;

public static class TestFiles
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();
    private static JsonNode? _config;

    private static JsonNode Config
    {
        get
        {
            if (_config != null) return _config;

            var cfgPath = Path.Combine(AppContext.BaseDirectory, "test-settings.json");
            if (File.Exists(cfgPath))
            {
                try
                {
                    _config = JsonNode.Parse(File.ReadAllText(cfgPath)) ?? throw new JsonException("Empty JSON");
                }
                catch
                {
                    Console.WriteLine($"Failed to parse {cfgPath}, using fallbacks");
                }
            }

            _config ??= new JsonObject
            {
                ["TestData"] = new JsonObject()
            };
            return _config;
        }
    }

    private static string GetPathOrFallback(string key, params string[] fallbackPath)
    {
        if (Cache.TryGetValue(key, out var cached) && File.Exists(cached))
            return cached;

        // Config -> TestData -> key
        var path = Config["TestData"]?[key]?.GetValue<string>();
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            Cache[key] = path;
            return path;
        }

        // Fallback: TestRoot + path
        var fallback = Path.Combine(Path.GetDirectoryName(typeof(TestFiles).Assembly.Location)!,
            Path.Combine(fallbackPath));
        Cache[key] = fallback;
        return fallback;
    }

    // Публичные свойства — простой синтаксис
    public static string TestRootDirectory => GetPathOrFallback("testRootDirectory");

    public static string SampleEpubPath => GetPathOrFallback("sampleEpubPath",
        "Samples", "epub-assorted", "accessibility-test-1.epub");
}
