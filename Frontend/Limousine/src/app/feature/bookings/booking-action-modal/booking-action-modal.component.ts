import { Component, ChangeDetectionStrategy, inject, input, output, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { BookingRequestItem } from '../../../core/interfaces/api.models';

export type BookingActionType = 'accept' | 'reject' | 'complete' | 'cancel';

@Component({
  selector: 'app-booking-action-modal',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslocoPipe
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './booking-action-modal.component.html',
  styleUrls: ['./booking-action-modal.component.css']
})
export class BookingActionModalComponent {
  private readonly fb = inject(FormBuilder);

  booking = input<BookingRequestItem | null>(null);
  actionType = input<BookingActionType | null>(null);
  isOpen = input<boolean>(false);
  isLoading = input<boolean>(false);

  confirmed = output<{ type: BookingActionType; reason?: string; notes?: string }>();
  closed = output<void>();

  readonly form: FormGroup = this.fb.group({
    reason: ['', [Validators.maxLength(500)]],
    notes: ['', [Validators.maxLength(1000)]]
  });

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        this.form.reset({ reason: '', notes: '' });
      }
    });
  }

  onSubmit(): void {
    const type = this.actionType();
    if (!type) return;

    const val = this.form.value;
    this.confirmed.emit({
      type,
      reason: val.reason?.trim() || undefined,
      notes: val.notes?.trim() || undefined
    });
  }

  onClose(): void {
    this.closed.emit();
  }
}
