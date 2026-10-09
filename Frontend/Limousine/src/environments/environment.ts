export const environment = {
  production: false,
  get baseUrl(): string {
    if (typeof window !== 'undefined' && (window as any).__env?.baseUrl) {
      return (window as any).__env.baseUrl;
    }
    return 'http://localhost:5205';
  },
  get apiUrl(): string {
    return `${this.baseUrl}/api`;
  }
};
