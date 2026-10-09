export const environment = {
  production: true,
  get baseUrl(): string {
    if (typeof window !== 'undefined' && (window as any).__env?.baseUrl) {
      return (window as any).__env.baseUrl;
    }
    return '';
  },
  get apiUrl(): string {
    return `${this.baseUrl}/api`;
  }
};
