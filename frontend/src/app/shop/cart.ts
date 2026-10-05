import { Injectable, computed, signal } from '@angular/core';

export interface CartLine {
  productId: string;
  variantId: string;
  quantity: number;
}
const key = 'myshop.cart.v1';
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const validId = (id: unknown): id is string =>
  typeof id === 'string' && guid.test(id) && id !== '00000000-0000-0000-0000-000000000000';

@Injectable({ providedIn: 'root' })
export class Cart {
  private readonly stored = signal<readonly CartLine[]>([]);
  readonly lines = this.stored.asReadonly();
  readonly count = computed(() => this.lines().reduce((sum, line) => sum + line.quantity, 0));
  readonly warning = signal('');
  constructor() {
    try {
      const raw = localStorage.getItem(key);
      if (!raw) return;
      const parsed: unknown = JSON.parse(raw);
      if (!Array.isArray(parsed) || parsed.length > 20) throw new Error();
      const unique = new Set<string>();
      const lines = parsed.map((line): CartLine => {
        if (
          !line ||
          !validId(line.productId) ||
          !validId(line.variantId) ||
          !Number.isInteger(line.quantity) ||
          line.quantity < 1 ||
          line.quantity > 99
        )
          throw new Error();
        const productId = line.productId.toLowerCase(),
          variantId = line.variantId.toLowerCase();
        const id = productId + variantId;
        if (unique.has(id)) throw new Error();
        unique.add(id);
        return { productId, variantId, quantity: line.quantity };
      });
      this.stored.set(lines);
    } catch {
      this.warning.set(
        'De bewaarde winkelmand kon niet worden geladen. Je kunt een nieuwe winkelmand samenstellen.',
      );
    }
  }
  add(productId: string, variantId: string): string {
    if (!validId(productId) || !validId(variantId))
      return 'Deze variant kan niet worden toegevoegd.';
    productId = productId.toLowerCase();
    variantId = variantId.toLowerCase();
    const existing = this.lines().find(
      (line) => line.productId === productId && line.variantId === variantId,
    );
    if (existing) return this.setQuantity(existing, existing.quantity + 1);
    if (this.lines().length >= 20) return 'Je kunt maximaal 20 verschillende varianten toevoegen.';
    this.save([...this.lines(), { productId, variantId, quantity: 1 }]);
    return '';
  }
  setQuantity(line: CartLine, quantity: number): string {
    if (!this.lines().some((item) => this.same(item, line)))
      return 'Dit artikel staat niet meer in je winkelmand.';
    if (!Number.isInteger(quantity) || quantity < 1 || quantity > 99)
      return 'Kies een heel aantal van 1 tot en met 99.';
    this.save(this.lines().map((item) => (this.same(item, line) ? { ...item, quantity } : item)));
    return '';
  }
  remove(line: CartLine) {
    if (!this.lines().some((item) => this.same(item, line))) return;
    this.save(this.lines().filter((item) => !this.same(item, line)));
  }
  clear() {
    this.save([]);
  }
  private same(a: CartLine, b: CartLine) {
    return a.productId === b.productId && a.variantId === b.variantId;
  }
  private save(lines: readonly CartLine[]) {
    this.stored.set(lines);
    try {
      localStorage.setItem(key, JSON.stringify(lines));
      this.warning.set('');
    } catch {
      this.warning.set('Je winkelmand werkt, maar kan niet in deze browser worden bewaard.');
    }
  }
}
