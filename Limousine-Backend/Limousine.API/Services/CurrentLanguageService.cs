using Limousine.API.Middleware;
using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Limousine.API.Services
{
    public class CurrentLanguageService : ICurrentLanguageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentLanguageService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string CurrentLanguage
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context?.Items.TryGetValue(LanguageResolutionMiddleware.LanguageItemKey, out var lang) == true && lang is string langStr)
                {
                    return langStr;
                }
                return LanguageResolutionMiddleware.DefaultLanguage;
            }
        }
    }
}
