import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { UsersService } from '../../../core/services/users.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { UserItem } from '../../../core/interfaces/api.models';
import { UserDetailModalComponent } from '../user-detail-modal/user-detail-modal.component';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslocoPipe,
    UserDetailModalComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.css']
})
export class UsersListComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  readonly authService = inject(AuthService);
  private readonly langService = inject(LanguageService);
  private readonly transloco = inject(TranslocoService);

  readonly users = signal<UserItem[]>([]);
  readonly loading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly togglingId = signal<number | null>(null);

  // Search & Role filters
  readonly searchQuery = signal<string>('');
  readonly selectedRole = signal<string>('all');

  // Modal State
  readonly selectedUserForModal = signal<UserItem | null>(null);
  readonly isModalOpen = signal<boolean>(false);

  // Current logged in admin
  readonly currentUser = computed(() => this.authService.currentUser());
  readonly isAdmin = computed(() => this.authService.isAdmin());

  get currentLang(): string {
    return this.langService.currentLang();
  }

  getRoleName(u: UserItem): string {
    if (u.roleName) return u.roleName;
    const r = (u as any).role;
    if (r === 1 || r === '1' || r === 'Admin') return 'Admin';
    if (r === 3 || r === '3' || r === 'Employee') return 'Employee';
    if (r === 2 || r === '2' || r === 'Customer') return 'Customer';
    return String(r || 'Customer');
  }

  readonly filteredUsers = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const roleFilter = this.selectedRole().toLowerCase();

    return this.users().filter(u => {
      const roleStr = this.getRoleName(u).toLowerCase();
      const matchesQuery = !query ||
        u.fullName?.toLowerCase().includes(query) ||
        u.email?.toLowerCase().includes(query) ||
        u.phoneNumber?.toLowerCase().includes(query);

      const matchesRole = roleFilter === 'all' || roleStr === roleFilter;

      return matchesQuery && matchesRole;
    });
  });

  readonly stats = computed(() => {
    const all = this.users();
    return {
      total: all.length,
      admins: all.filter(u => this.getRoleName(u).toLowerCase() === 'admin').length,
      employees: all.filter(u => this.getRoleName(u).toLowerCase() === 'employee').length,
      customers: all.filter(u => this.getRoleName(u).toLowerCase() === 'customer').length,
      active: all.filter(u => u.isActive).length
    };
  });

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.usersService.getUsers().subscribe({
      next: (res) => {
        this.loading.set(false);
        this.users.set(res.data || []);
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.message || this.transloco.translate('users.errorLoading');
        this.errorMessage.set(msg);
      }
    });
  }

  openUserModal(user: UserItem): void {
    this.selectedUserForModal.set(user);
    this.isModalOpen.set(true);
  }

  closeUserModal(): void {
    this.isModalOpen.set(false);
    this.selectedUserForModal.set(null);
  }

  isCurrentUser(user: UserItem | null): boolean {
    if (!user) return false;
    const cur = this.currentUser();
    return !!cur && cur.id === user.id;
  }

  toggleUserStatus(user: UserItem): void {
    if (this.isCurrentUser(user)) {
      this.errorMessage.set(this.transloco.translate('users.cannotDeactivateSelf'));
      return;
    }

    const nextStatus = !user.isActive;
    this.togglingId.set(user.id);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.usersService.toggleStatus(user.id, nextStatus).subscribe({
      next: () => {
        this.togglingId.set(null);
        this.users.update(list =>
          list.map(u => u.id === user.id ? { ...u, isActive: nextStatus } : u)
        );

        // Also update modal selected user if open
        const curModal = this.selectedUserForModal();
        if (curModal && curModal.id === user.id) {
          this.selectedUserForModal.set({ ...curModal, isActive: nextStatus });
        }

        this.successMessage.set(this.transloco.translate('users.statusUpdatedSuccess'));
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: (err) => {
        this.togglingId.set(null);
        const msg = err?.error?.message || this.transloco.translate('users.statusUpdateError');
        this.errorMessage.set(msg);
      }
    });
  }

  getRoleClass(u: UserItem): string {
    return this.getRoleName(u).toLowerCase();
  }
}
