using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Limousine.API.Middleware
{
    public class LanguageResolutionMiddleware
    {
        public const string LanguageItemKey = "Language";
        public const string DefaultLanguage = "en";
        public static readonly string[] SupportedLanguages = { "en", "fr" };

        private readonly RequestDelegate _next;

        public LanguageResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string resolvedLang = DefaultLanguage;

            // 1. Check ?lang= query parameter (query parameter takes precedence)
            if (context.Request.Query.TryGetValue("lang", out var queryLang) && !string.IsNullOrWhiteSpace(queryLang))
            {
                var val = queryLang.ToString().Trim().ToLowerInvariant();
                if (val.StartsWith("fr")) resolvedLang = "fr";
                else if (val.StartsWith("en")) resolvedLang = "en";
            }
            // 2. Otherwise check Accept-Language header
            else if (context.Request.Headers.TryGetValue("Accept-Language", out var acceptHeader) && !string.IsNullOrWhiteSpace(acceptHeader))
            {
                var headerVal = acceptHeader.ToString().ToLowerInvariant();
                var parts = headerVal.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in parts)
                {
                    var code = part.Split(';')[0].Trim();
                    if (code.StartsWith("fr"))
                    {
                        resolvedLang = "fr";
                        break;
                    }
                    if (code.StartsWith("en"))
                    {
                        resolvedLang = "en";
                        break;
                    }
                }
            }

            // Fallback strictly to "en" if not allowed
            if (resolvedLang != "fr" && resolvedLang != "en")
            {
                resolvedLang = DefaultLanguage;
            }

            // Store in request context
            context.Items[LanguageItemKey] = resolvedLang;

            // Set thread culture for formatting
            var culture = new CultureInfo(resolvedLang);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // Response header Content-Language with resolved language
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Content-Language"] = resolvedLang;
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
