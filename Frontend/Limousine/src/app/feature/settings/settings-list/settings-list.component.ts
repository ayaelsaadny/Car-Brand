import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { SettingsService } from '../../../core/services/settings.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { WebsiteSetting } from '../../../core/interfaces/api.models';
import { SettingFormComponent } from '../setting-form/setting-form.component';

@Component({
  selector: 'app-settings-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslocoPipe,
    SettingFormComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './settings-list.component.html',
  styleUrls: ['./settings-list.component.css']
})
export class SettingsListComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  readonly authService = inject(AuthService);
  private readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);

  readonly settings = signal<WebsiteSetting[]>([]);
  readonly loading = signal<boolean>(true);
  readonly saving = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Search & Filter
  readonly searchQuery = signal<string>('');
  readonly selectedGroup = signal<string>('all');

  // Edit Modal State
  readonly isFormOpen = signal<boolean>(false);
  readonly selectedSetting = signal<WebsiteSetting | null>(null);

  // Role permissions
  readonly isAdmin = computed(() => this.authService.isAdmin());

  get currentLang(): string {
    return this.langService.currentLang();
  }

  readonly availableGroups = computed<string[]>(() => {
    const list = this.settings();
    const groups = new Set<string>();
    list.forEach(s => {
      if (s.group) groups.add(s.group);
    });
    return Array.from(groups);
  });

  readonly filteredSettings = computed<WebsiteSetting[]>(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const grp = this.selectedGroup();

    return this.settings().filter(s => {
      const matchesQuery = !query ||
        s.key.toLowerCase().includes(query) ||
        s.value.toLowerCase().includes(query) ||
        (s.description && s.description.toLowerCase().includes(query)) ||
        (s.group && s.group.toLowerCase().includes(query));

      const matchesGroup = grp === 'all' || (s.group && s.group.toLowerCase() === grp.toLowerCase());

      return matchesQuery && matchesGroup;
    });
  });

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.settingsService.getAllSettings().subscribe({
      next: (res) => {
        this.loading.set(false);
        this.settings.set(res.data || []);
      },
      error: (err: any) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('settingsPage.errorLoading');
        this.errorMessage.set(msg);
      }
    });
  }

  openEditModal(setting: WebsiteSetting): void {
    if (!this.isAdmin()) return;
    this.selectedSetting.set(setting);
    this.isFormOpen.set(true);
  }

  closeFormModal(): void {
    this.isFormOpen.set(false);
    this.selectedSetting.set(null);
  }

  onSettingSaved(saved: WebsiteSetting): void {
    this.closeFormModal();
    this.successMessage.set(this.transloco.translate('settingsPage.savedSuccess'));
    setTimeout(() => this.successMessage.set(null), 4000);
    this.settings.update(list => list.map(s => s.key === saved.key ? saved : s));
  }
}
