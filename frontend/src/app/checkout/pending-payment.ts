import { Injectable } from '@angular/core';
import { CartLine } from '../shop/cart';
const key = 'myshop.pending-payment.v1';
const signature = (lines: readonly CartLine[]) =>
  JSON.stringify(
    lines
      .map(({ productId, variantId, quantity }) => ({ productId, variantId, quantity }))
      .sort((a, b) => (a.productId + a.variantId).localeCompare(b.productId + b.variantId)),
  );

@Injectable({ providedIn: 'root' })
export class PendingPayment {
  save(token: string, lines: readonly CartLine[]) {
    try {
      localStorage.setItem(key, JSON.stringify({ token, signature: signature(lines) }));
    } catch {
      /* Status retrieval still works without browser storage. */
    }
  }
  token(): string | null {
    try {
      const stored = JSON.parse(localStorage.getItem(key) ?? 'null');
      return typeof stored?.token === 'string' && typeof stored.signature === 'string'
        && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(stored.token)
        && stored.token !== '00000000-0000-0000-0000-000000000000' ? stored.token : null;
    } catch { return null; }
  }
  matches(token: string, lines: readonly CartLine[]) {
    try {
      const stored = JSON.parse(localStorage.getItem(key) ?? 'null');
      return stored?.token === token && stored?.signature === signature(lines);
    } catch {
      return false;
    }
  }
  overlaps(token: string, lines: readonly CartLine[]): boolean {
    try {
      const stored = JSON.parse(localStorage.getItem(key) ?? 'null');
      if (stored?.token !== token) return false;
      const original: unknown = JSON.parse(stored.signature);
      if (!Array.isArray(original)) return true;
      return original.some(item => lines.some(line => item?.productId === line.productId && item?.variantId === line.variantId));
    } catch { return true; }
  }
  clear(token: string) {
    try {
      if (JSON.parse(localStorage.getItem(key) ?? 'null')?.token === token)
        localStorage.removeItem(key);
    } catch {}
  }
}
