import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // Prerender public landing page for both supported languages ('en' and 'fr')
  {
    path: ':lang',
    renderMode: RenderMode.Prerender,
    getPrerenderParams: async () => [
      { lang: 'en' },
      { lang: 'fr' }
    ]
  },
  // Dynamic admin and portal routes rendered server-side on demand
  {
    path: ':lang/dashboard',
    renderMode: RenderMode.Server
  },
  {
    path: ':lang/vehicles',
    renderMode: RenderMode.Server
  },
  {
    path: ':lang/bookings',
    renderMode: RenderMode.Server
  },
  {
    path: ':lang/services',
    renderMode: RenderMode.Server
  },
  {
    path: ':lang/categories',
    renderMode: RenderMode.Server
  },
  {
    path: ':lang/settings',
    renderMode: RenderMode.Server
  },
  // Fallback for all other routes rendered server-side
  {
    path: '**',
    renderMode: RenderMode.Server
  }
];
