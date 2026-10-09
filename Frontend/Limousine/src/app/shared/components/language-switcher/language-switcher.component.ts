import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageService, SupportedLanguage } from '../../../core/services/language.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="lang-switcher" role="group" aria-label="Language selection">
      <button
        type="button"
        class="lang-btn"
        [class.active]="langService.currentLang() === 'en'"
        (click)="switchLang('en')"
        [attr.aria-pressed]="langService.currentLang() === 'en'"
      >
        EN
      </button>
      <span class="divider">|</span>
      <button
        type="button"
        class="lang-btn"
        [class.active]="langService.currentLang() === 'fr'"
        (click)="switchLang('fr')"
        [attr.aria-pressed]="langService.currentLang() === 'fr'"
      >
        FR
      </button>
    </div>
  `,
  styles: [`
    .lang-switcher {
      display: inline-flex;
      align-items: center;
      background: var(--c-ink-2, #1a1a1c);
      border: 1px solid var(--c-ink-3, #2a2a2d);
      border-radius: var(--radius-pill, 999px);
      padding: 3px 6px;
      gap: 4px;
      user-select: none;
    }
    .lang-btn {
      background: transparent;
      border: 0;
      color: var(--c-on-dark-muted, rgba(255, 255, 255, 0.7));
      font-size: var(--fs-xs, 0.75rem);
      font-weight: 700;
      letter-spacing: 0.05em;
      padding: 4px 10px;
      border-radius: var(--radius-pill, 999px);
      cursor: pointer;
      transition: all 0.2s cubic-bezier(0.2, 0.7, 0.2, 1);
    }
    .lang-btn:hover {
      color: #fff;
    }
    .lang-btn.active {
      background: var(--c-gold, #c2b283);
      color: var(--c-ink, #0f0f10);
      box-shadow: 0 2px 6px rgba(194, 178, 131, 0.35);
    }
    .divider {
      color: var(--c-ink-3, #2a2a2d);
      font-size: 0.75rem;
      pointer-events: none;
    }
  `]
})
export class LanguageSwitcherComponent {
  readonly langService = inject(LanguageService);

  switchLang(lang: SupportedLanguage): void {
    if (this.langService.currentLang() !== lang) {
      this.langService.setLanguage(lang);
    }
  }
}
