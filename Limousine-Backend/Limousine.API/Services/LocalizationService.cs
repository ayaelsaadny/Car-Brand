using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace Limousine.API.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly ICurrentLanguageService _languageService;
        private readonly IWebHostEnvironment _env;
        private static readonly ConcurrentDictionary<string, JsonElement> Cache = new();

        public LocalizationService(ICurrentLanguageService languageService, IWebHostEnvironment env)
        {
            _languageService = languageService;
            _env = env;
        }

        public string this[string key] => GetMessage(key);

        public string GetMessage(string key, params object[] args)
        {
            var lang = _languageService.CurrentLanguage;
            if (lang != "fr") lang = "en";

            var root = GetOrLoadDictionary(lang);
            var value = FindKey(root, key) ?? FindKey(GetOrLoadDictionary("en"), key) ?? key;

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(value, args);
                }
                catch
                {
                    return value;
                }
            }

            return value;
        }

        private JsonElement GetOrLoadDictionary(string lang)
        {
            return Cache.GetOrAdd(lang, l =>
            {
                var filePath = Path.Combine(_env.ContentRootPath, "Resources", $"messages.{l}.json");
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    using var doc = JsonDocument.Parse(json);
                    return doc.RootElement.Clone();
                }
                using var emptyDoc = JsonDocument.Parse("{}");
                return emptyDoc.RootElement.Clone();
            });
        }

        private static string? FindKey(JsonElement element, string keyPath)
        {
            var parts = keyPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var current = element;

            foreach (var part in parts)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var next))
                {
                    current = next;
                }
                else
                {
                    return null;
                }
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }
    }
}
