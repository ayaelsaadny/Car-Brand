import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { BookingRequestItem } from '../../../core/interfaces/api.models';

@Component({
  selector: 'app-booking-detail-modal',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './booking-detail-modal.component.html',
  styleUrls: ['./booking-detail-modal.component.css']
})
export class BookingDetailModalComponent {
  booking = input<BookingRequestItem | null>(null);
  isOpen = input<boolean>(false);

  closed = output<void>();

  onClose(): void {
    this.closed.emit();
  }

  getStatusClass(status: number | string | undefined): string {
    const s = String(status || '').toLowerCase();
    if (s === '1' || s === 'pending') return 'status-pending';
    if (s === '2' || s === 'confirmed' || s === 'accepted') return 'status-confirmed';
    if (s === '3' || s === 'inprogress') return 'status-inprogress';
    if (s === '4' || s === 'completed') return 'status-completed';
    if (s === '5' || s === 'cancelled') return 'status-cancelled';
    if (s === '6' || s === 'rejected') return 'status-rejected';
    return 'status-pending';
  }
}
