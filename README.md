# Limousine Executive & VIP Fleet — Multilingual System (EN / FR)

This solution implements bilingual support (**English** `en` and **French** `fr` only, both LTR `dir="ltr"`).

---

## Architecture Overview

### 1. Backend (.NET 10 / ASP.NET Core & EF Core SQL Server)
- **Language Resolution Middleware** (`LanguageResolutionMiddleware`):
  - Resolves language from `?lang=` query parameter (first priority) or `Accept-Language` HTTP header.
  - Strictly allows only `en` and `fr`; falls back to `en` for all other values.
  - Attaches `Content-Language` header to all HTTP responses and exposes it via CORS.
  - Sets request culture and stores language code in request context.
- **Translatable Entity Design**:
  - Main entities: `Services` and `Categories`.
  - Translations tables: `ServiceTranslations` and `CategoryTranslations` with unique composite indexes on `(ServiceId, LanguageCode)` and `(CategoryId, LanguageCode)`.
  - Read endpoints resolve text in the requested language, with automatic fallback to English if the French record is absent.
  - Admin write endpoints accept multi-language payloads (`translations: { en: {...}, fr: {...} }`), enforcing required English records.
- **Server Message Localization**:
  - Server-side messages (validation, notifications) loaded from `Resources/messages.en.json` and `Resources/messages.fr.json`.

### 2. Frontend (Angular 22 Standalone Components, SSR & Transloco)
- **Translation Engine**: Configured with `@jsverse/transloco` (`public/assets/i18n/en.json` and `public/assets/i18n/fr.json`).
- **LanguageService**: Signal-based state management (`currentLang`), synchronizing DOM (`<html lang="..." dir="ltr">`), localStorage (`preferred_lang`), Transloco active language, and URL prefix.
- **Route Prefixing & LangGuard**:
  - All routes are prefixed with `/:lang` (e.g. `/en`, `/fr`, `/en/dashboard`, `/fr/dashboard`).
  - `langGuard` validates that `:lang` is strictly `en` or `fr`. Any invalid or missing prefix automatically redirects to `/en`.
- **HTTP Interceptor** (`languageInterceptor`):
  - Automatically appends `Accept-Language: <currentLang>` header to every outgoing HTTP request.
- **Language Switcher Component** (`LanguageSwitcherComponent`):
  - Segmented `EN | FR` luxury toggle integrated in the public navbar and in the `MainLayoutComponent` header.
- **SEO & SSR Prerendering**:
  - `SeoService` dynamically manages `<title>`, `<meta name="description">`, OpenGraph tags, canonical URLs, and alternate `hreflang` tags (`en`, `fr`, and `x-default`).
  - Pre-renders both `/en` and `/fr` static routes on the server.

---

## Developer Guide: How to Add New Translations

### 1. How to Add a New Translated String (Frontend)
1. **Add keys to translation dictionaries**:
   Open `public/assets/i18n/en.json` and `public/assets/i18n/fr.json`:
   ```json
   // en.json
   {
     "branches": {
       "title": "Branches & Locations",
       "searchPlaceholder": "Search city or airport..."
     }
   }
   ```
   ```json
   // fr.json
   {
     "branches": {
       "title": "Agences & Emplacements",
       "searchPlaceholder": "Rechercher une ville ou un aéroport..."
     }
   }
   ```
2. **Use in HTML templates**:
   ```html
   <h2>{{ 'branches.title' | transloco }}</h2>
   <input [placeholder]="'branches.searchPlaceholder' | transloco" />
   ```
3. **Use in TypeScript code**:
   ```typescript
   private transloco = inject(TranslocoService);
   const label = this.transloco.translate('branches.title');
   ```

---

### 2. How to Add a New Translatable Entity Field (Backend)
1. **Update the Translation Entity**:
   Add the new property to the corresponding translation class in `Limousine.Core/Entities/`:
   ```csharp
   // ServiceTranslation.cs
   public class ServiceTranslation : BaseEntity
   {
       // ... existing fields
       public string? Subtitle { get; set; }
   }
   ```
2. **Generate and Apply Migration**:
   ```powershell
   cd "Limousine-Backend"
   dotnet ef migrations add AddSubtitleToServiceTranslation --project Limousine.Infrastracture --startup-project Limousine.API
   dotnet ef database update --project Limousine.Infrastracture --startup-project Limousine.API
   ```
3. **Update DTOs**:
   - In `ServiceTranslationDto.cs`, add `public string? Subtitle { get; set; }`.
   - In `ServiceResponseDto.cs`, add `public string? Subtitle { get; set; }`.
4. **Update Service Resolution**:
   In `ServiceManagementService.cs`, populate the field from the resolved translation:
   ```csharp
   var trans = service.Translations.FirstOrDefault(t => t.LanguageCode == lang)
            ?? service.Translations.FirstOrDefault(t => t.LanguageCode == "en");

   dto.Subtitle = trans?.Subtitle;
   ```
5. **Handle Multi-language Updates/Creation**:
   When saving via Admin endpoints, persist the field to `ServiceTranslation` entities for both `en` and `fr`.
