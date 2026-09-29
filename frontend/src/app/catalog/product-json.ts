import { Product } from './catalog.models';
// JSON.parse's source context preserves numeric tokens before IEEE-754 conversion.
export function parseProduct(json: string): Product {
  return JSON.parse(json, (key: string, value: unknown, context?: { source: string }) => {
    if (key === 'value' && typeof value === 'number') {
      if (!context?.source) throw new Error('Exact numeric JSON parsing is unavailable.');
      return context.source;
    }
    return value;
  });
}
