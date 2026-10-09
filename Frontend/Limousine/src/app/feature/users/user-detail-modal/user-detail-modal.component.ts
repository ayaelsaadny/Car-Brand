import { Component, ChangeDetectionStrategy, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { UserItem } from '../../../core/interfaces/api.models';

@Component({
  selector: 'app-user-detail-modal',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './user-detail-modal.component.html',
  styleUrls: ['./user-detail-modal.component.css']
})
export class UserDetailModalComponent {
  user = input<UserItem | null>(null);
  isOpen = input<boolean>(false);
  isCurrentUser = input<boolean>(false);
  isToggling = input<boolean>(false);

  toggleStatus = output<UserItem>();
  closed = output<void>();

  confirming = signal<boolean>(false);

  onClose(): void {
    this.confirming.set(false);
    this.closed.emit();
  }

  startConfirm(): void {
    this.confirming.set(true);
  }

  cancelConfirm(): void {
    this.confirming.set(false);
  }

  onConfirmToggle(): void {
    const u = this.user();
    if (u && !this.isCurrentUser()) {
      this.confirming.set(false);
      this.toggleStatus.emit(u);
    }
  }

  getRoleName(u: UserItem | null): string {
    if (!u) return '';
    if (u.roleName) return u.roleName;
    const r = (u as any).role;
    if (r === 1 || r === '1' || r === 'Admin') return 'Admin';
    if (r === 3 || r === '3' || r === 'Employee') return 'Employee';
    if (r === 2 || r === '2' || r === 'Customer') return 'Customer';
    return String(r || 'Customer');
  }

  getRoleDescription(role: string | undefined): string {
    const r = (role || '').toLowerCase();
    if (r === 'admin') return 'Full administrative control, settings, user accounts and system configuration.';
    if (r === 'employee') return 'Fleet operations, chauffeur dispatching, booking management and categories.';
    return 'VIP client reservation, private booking management and executive account access.';
  }
}
