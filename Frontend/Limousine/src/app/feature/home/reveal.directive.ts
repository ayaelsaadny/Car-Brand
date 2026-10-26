import {
  Directive,
  ElementRef,
  Input,
  OnDestroy,
  OnInit,
  PLATFORM_ID,
  booleanAttribute,
  inject
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Reusable scroll-reveal animations for the Oudy landing page.
 *
 * Usage:
 *   <div reveal>                         default fade + slide up
 *   <section reveal="fade">             soft fade only
 *   <div reveal="left">                 enter from the left
 *   <div reveal="right">                enter from the right
 *   <div reveal="zoom">                 elegant zoom-in
 *   <div reveal="blur">                 clear from blur + rise
 *   <h2 reveal="mask">                  unveil from the bottom
 *   <div reveal [revealDelay]="160">    stagger animation
 *   <article reveal="zoom" revealTilt>  desktop 3D tilt + highlight
 *
 * CSS animations live in home.component.css.
 */
@Directive({
  selector: '[reveal]',
  standalone: true
})
export class RevealDirective implements OnInit, OnDestroy {
  @Input() reveal = '';
  @Input() revealDelay = 0;
  @Input({ transform: booleanAttribute }) revealTilt = false;

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly platformId = inject(PLATFORM_ID);
  private observer: IntersectionObserver | null = null;
  private tiltEnabled = false;
  private readonly motionQuery: MediaQueryList | null = null;

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const node = this.el.nativeElement;
    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

    // Keep content visible if motion is reduced or IntersectionObserver is unavailable.
    if (reduceMotion || !('IntersectionObserver' in window)) {
      node.classList.add('is-visible');
      return;
    }

    this.applyRevealVariant(node);
    node.style.setProperty('--reveal-delay', `${Math.max(0, this.revealDelay)}ms`);

    if (this.revealTilt && (window.matchMedia?.('(hover: hover)').matches ?? false)) {
      this.tiltEnabled = true;
      node.addEventListener('mousemove', this.onMove, { passive: true });
      node.addEventListener('mouseleave', this.onLeave, { passive: true });
    }

    this.observer = new IntersectionObserver(
      entries => {
        if (entries.some(entry => entry.isIntersecting)) {
          node.classList.add('is-visible');
          this.observer?.disconnect();
          this.observer = null;
        }
      },
      {
        threshold: this.reveal.trim() === 'fade' ? 0.05 : 0.12,
        rootMargin: '0px 0px -40px 0px'
      }
    );

    this.observer.observe(node);
  }

  private applyRevealVariant(node: HTMLElement): void {
    const variant = (this.reveal || '').trim().toLowerCase();

    if (variant === 'fade') {
      node.classList.add('reveal-fade');
      return;
    }

    node.classList.add('reveal');
    const supported = ['left', 'right', 'zoom', 'blur', 'mask'];
    if (supported.includes(variant)) {
      node.classList.add(`reveal-${variant}`);
    }
  }

  private readonly onMove = (event: MouseEvent): void => {
    const node = this.el.nativeElement;
    const rect = node.getBoundingClientRect();
    if (!rect.width || !rect.height) return;

    const x = (event.clientX - rect.left) / rect.width - 0.5;
    const y = (event.clientY - rect.top) / rect.height - 0.5;

    node.style.setProperty('--ry', `${(x * 9).toFixed(2)}deg`);
    node.style.setProperty('--rx', `${(-y * 9).toFixed(2)}deg`);
    node.style.setProperty('--mx', `${((x + 0.5) * 100).toFixed(1)}%`);
    node.style.setProperty('--my', `${((y + 0.5) * 100).toFixed(1)}%`);
  };

  private readonly onLeave = (): void => {
    const node = this.el.nativeElement;
    node.style.setProperty('--ry', '0deg');
    node.style.setProperty('--rx', '0deg');
  };

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.observer = null;

    if (this.tiltEnabled) {
      const node = this.el.nativeElement;
      node.removeEventListener('mousemove', this.onMove);
      node.removeEventListener('mouseleave', this.onLeave);
    }
  }
}
