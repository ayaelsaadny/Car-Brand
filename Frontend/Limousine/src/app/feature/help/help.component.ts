import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

export type HelpCategory = 'booking' | 'airport' | 'pricing' | 'services' | 'business';

interface FaqDef { id: string; cat: HelpCategory; }

/**
 * The questions. Their TEXTS live in en.json / fr.json under  help.faq.<id>.q  and  help.faq.<id>.a
 * To add a question: add a line here + its q / a in both json files.
 */
const FAQS: FaqDef[] = [
  { id: 'b1', cat: 'booking' },
  { id: 'b2', cat: 'booking' },
  { id: 'b3', cat: 'booking' },
  { id: 'a1', cat: 'airport' },
  { id: 'a2', cat: 'airport' },
  { id: 'a3', cat: 'airport' },
  { id: 'p1', cat: 'pricing' },
  { id: 'p2', cat: 'pricing' },
  { id: 's1', cat: 'services' },
  { id: 's2', cat: 'services' },
  { id: 's3', cat: 'services' },
  { id: 'z1', cat: 'business' },
  { id: 'z2', cat: 'business' }
];

export const HELP_CATEGORIES: HelpCategory[] = ['booking', 'airport', 'pricing', 'services', 'business'];

/** lower case + no accents, so "Montreal" finds "Montréal" */
const norm = (s: string): string =>
  s.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

@Component({
  selector: 'app-help',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './help.component.html',
  styleUrl: './help.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HelpComponent {
  private readonly transloco = inject(TranslocoService);

  /** Passed from the home component: contact.whatsappUrl / contact.email */
  readonly whatsappUrl = input<string>('');
  readonly email = input<string>('');

  readonly categories = HELP_CATEGORIES;
  readonly tab = signal<HelpCategory>('booking');
  readonly query = signal('');
  readonly openId = signal<string | null>(null);

  /** All the "help" texts of the current language (emits again when the language changes) */
  private readonly texts = toSignal(this.transloco.selectTranslateObject('help'), { initialValue: null });

  readonly searching = computed(() => this.query().trim().length > 0);

  /** The questions to show: the current tab, or every tab while the person is searching */
  readonly items = computed(() => {
    const t = this.texts() as any;
    if (!t?.faq) return [];
    const q = norm(this.query().trim());
    return FAQS
      .filter(f => (q ? true : f.cat === this.tab()))
      .map(f => ({
        id: f.id,
        cat: f.cat,
        q: String(t.faq[f.id]?.q ?? ''),
        a: String(t.faq[f.id]?.a ?? '')
      }))
      .filter(i => i.q && (!q || norm(i.q + ' ' + i.a).includes(q)));
  });

  onSearch(value: string): void {
    this.query.set(value);
    this.openId.set(null);
  }

  clearSearch(): void {
    this.onSearch('');
  }

  selectTab(cat: HelpCategory): void {
    this.query.set('');
    this.tab.set(cat);
    this.openId.set(null);
  }

  /** Arrow keys / Home / End move between the tabs (keyboard accessibility) */
  onTabKey(ev: KeyboardEvent, index: number): void {
    const last = this.categories.length - 1;
    let next = -1;
    if (ev.key === 'ArrowRight') next = index === last ? 0 : index + 1;
    else if (ev.key === 'ArrowLeft') next = index === 0 ? last : index - 1;
    else if (ev.key === 'Home') next = 0;
    else if (ev.key === 'End') next = last;
    if (next < 0) return;
    ev.preventDefault();
    this.selectTab(this.categories[next]);
    (ev.currentTarget as HTMLElement).parentElement
      ?.querySelectorAll<HTMLElement>('[role="tab"]')[next]?.focus();
  }

  toggle(id: string): void {
    this.openId.update(cur => (cur === id ? null : id));
  }
}