import { Component, Input, OnDestroy, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';

/**
 * Image slider.
 *  - One image  (only `image`)            => a normal photo, no arrows, no dots, no movement.
 *  - Many images (`images` has 2 or more) => slider with arrows, dots and auto-play.
 * Put it inside an element that has position: relative / absolute and a size.
 */
@Component({
  selector: 'app-image-slider',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="sl" (mouseenter)="stop()" (mouseleave)="start()">
      <div class="sl-slide" *ngFor="let img of list; let i = index"
           [class.active]="i === current()"
           [style.background-image]="'url(' + img + '), linear-gradient(135deg,#071634,#143d7d)'"></div>

      <ng-container *ngIf="list.length > 1">
        <button type="button" class="sl-arrow sl-prev" (click)="prev($event)" aria-label="Previous">‹</button>
        <button type="button" class="sl-arrow sl-next" (click)="next($event)" aria-label="Next">›</button>
        <div class="sl-dots">
          <button type="button" class="sl-dot" *ngFor="let img of list; let i = index"
                  [class.active]="i === current()" (click)="go(i, $event)"
                  [attr.aria-label]="'Image ' + (i + 1)"></button>
        </div>
      </ng-container>
    </div>
  `,
  styles: [`
    :host { position: absolute; inset: 0; display: block; }
    .sl { position: relative; width: 100%; height: 100%; overflow: hidden; }
    .sl-slide { position: absolute; inset: 0; background-size: cover; background-position: center;
                opacity: 0; transition: opacity .7s ease; }
    .sl-slide.active { opacity: 1; }
    .sl-arrow { position: absolute; top: 50%; transform: translateY(-50%); z-index: 3;
                width: 38px; height: 38px; border: 0; border-radius: 50%; cursor: pointer;
                background: rgba(0,0,0,.45); color: #fff; font-size: 24px; line-height: 1;
                opacity: 0; transition: opacity .25s; }
    .sl:hover .sl-arrow { opacity: 1; }
    .sl-prev { left: 10px; }
    .sl-next { right: 10px; }
    .sl-dots { position: absolute; bottom: 12px; left: 0; right: 0; z-index: 3;
               display: flex; justify-content: center; gap: 6px; }
    .sl-dot { width: 8px; height: 8px; padding: 0; border: 0; border-radius: 50%;
              background: rgba(255,255,255,.5); cursor: pointer; }
    .sl-dot.active { background: #fff; transform: scale(1.3); }
    @media (hover: none) { .sl-arrow { opacity: 1; } }
  `]
})
export class ImageSliderComponent implements OnInit, OnDestroy {
  @Input() image?: string;        // one image (default)
  @Input() images?: string[];     // many images (slider appears when 2 or more)
  @Input() interval = 5000;       // ms between images

  private platformId = inject(PLATFORM_ID);
  current = signal(0);
  private timer: ReturnType<typeof setInterval> | null = null;

  get list(): string[] {
    return this.images?.length ? this.images : (this.image ? [this.image] : []);
  }

  ngOnInit(): void { this.start(); }
  ngOnDestroy(): void { this.stop(); }

  start(): void {
    if (!isPlatformBrowser(this.platformId) || this.list.length < 2) return;
    this.stop();
    this.timer = setInterval(() => this.current.update(i => (i + 1) % this.list.length), this.interval);
  }

  stop(): void {
    if (this.timer) { clearInterval(this.timer); this.timer = null; }
  }

  // preventDefault: the slider can sit inside a card that is a link (<a>), so arrows must not open the page
  next(e: Event): void {
    e.preventDefault(); e.stopPropagation();
    this.current.update(i => (i + 1) % this.list.length);
    this.start();
  }

  prev(e: Event): void {
    e.preventDefault(); e.stopPropagation();
    this.current.update(i => (i - 1 + this.list.length) % this.list.length);
    this.start();
  }

  go(i: number, e: Event): void {
    e.preventDefault(); e.stopPropagation();
    this.current.set(i);
    this.start();
  }
}