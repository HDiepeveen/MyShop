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
  matches(token: string, lines: readonly CartLine[]) {
    try {
      const stored = JSON.parse(localStorage.getItem(key) ?? 'null');
      return stored?.token === token && stored?.signature === signature(lines);
    } catch {
      return false;
    }
  }
  clear(token: string) {
    try {
      if (JSON.parse(localStorage.getItem(key) ?? 'null')?.token === token)
        localStorage.removeItem(key);
    } catch {}
  }
}
