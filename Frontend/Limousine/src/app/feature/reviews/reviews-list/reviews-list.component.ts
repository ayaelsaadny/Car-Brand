import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AdminReviewsService } from '../../../core/services/admin-reviews.service';
import { Review } from '../../../core/interfaces/api.models';

type FilterTab = 'all' | 'pending' | 'approved' | 'rejected';

@Component({
  selector: 'app-reviews-list',
  standalone: true,
  imports: [CommonModule, TranslocoPipe, DatePipe],
  templateUrl: './reviews-list.component.html',
  styleUrls: ['./reviews-list.component.css']
})
export class ReviewsListComponent implements OnInit {
  private readonly adminReviewsService = inject(AdminReviewsService);
  private readonly transloco = inject(TranslocoService);

  readonly reviews = signal<Review[]>([]);
  readonly loading = signal(true);
  readonly currentTab = signal<FilterTab>('all');
  readonly actionLoadingId = signal<number | null>(null);

  readonly filteredReviews = computed(() => {
    const list = this.reviews();
    const tab = this.currentTab();
    if (tab === 'pending') {
      return list.filter(r => !r.isApproved && !r.reviewedAt);
    }
    if (tab === 'approved') {
      return list.filter(r => r.isApproved);
    }
    if (tab === 'rejected') {
      return list.filter(r => !r.isApproved && !!r.reviewedAt);
    }
    return list;
  });

  readonly counts = computed(() => {
    const list = this.reviews();
    return {
      all: list.length,
      pending: list.filter(r => !r.isApproved && !r.reviewedAt).length,
      approved: list.filter(r => r.isApproved).length,
      rejected: list.filter(r => !r.isApproved && !!r.reviewedAt).length
    };
  });

  ngOnInit(): void {
    this.loadReviews();
  }

  loadReviews(): void {
    this.loading.set(true);
    this.adminReviewsService.getAllReviews().subscribe({
      next: (res) => {
        this.reviews.set(res.data || []);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error fetching admin reviews:', err);
        this.loading.set(false);
      }
    });
  }

  setTab(tab: FilterTab): void {
    this.currentTab.set(tab);
  }

  approve(review: Review): void {
    this.actionLoadingId.set(review.id);
    this.adminReviewsService.approveReview(review.id).subscribe({
      next: () => {
        this.reviews.update(list =>
          list.map(r => r.id === review.id ? { ...r, isApproved: true, reviewedAt: new Date().toISOString() } : r)
        );
        this.actionLoadingId.set(null);
      },
      error: (err) => {
        console.error('Error approving review:', err);
        this.actionLoadingId.set(null);
      }
    });
  }

  reject(review: Review): void {
    this.actionLoadingId.set(review.id);
    this.adminReviewsService.rejectReview(review.id).subscribe({
      next: () => {
        this.reviews.update(list =>
          list.map(r => r.id === review.id ? { ...r, isApproved: false, reviewedAt: new Date().toISOString() } : r)
        );
        this.actionLoadingId.set(null);
      },
      error: (err) => {
        console.error('Error rejecting review:', err);
        this.actionLoadingId.set(null);
      }
    });
  }

  deleteReview(review: Review): void {
    if (!confirm('Are you sure you want to permanently delete this review?')) return;

    this.actionLoadingId.set(review.id);
    this.adminReviewsService.deleteReview(review.id).subscribe({
      next: () => {
        this.reviews.update(list => list.filter(r => r.id !== review.id));
        this.actionLoadingId.set(null);
      },
      error: (err) => {
        console.error('Error deleting review:', err);
        this.actionLoadingId.set(null);
      }
    });
  }
}
