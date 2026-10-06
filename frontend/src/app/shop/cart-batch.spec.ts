import { Cart } from './cart';
const productId = '10000000-0000-0000-0000-000000000001';
const variantId = '20000000-0000-0000-0000-000000000001';
const second = '20000000-0000-0000-0000-000000000002';
const line = { productId, variantId, quantity: 2 };
beforeEach(() => localStorage.removeItem('myshop.cart.v1'));
afterEach(() => {
  localStorage.removeItem('myshop.cart.v1');
  vi.restoreAllMocks();
});
describe('Atomic cart additions', () => {
  it('merges quantities and persists identifiers only in one write', () => {
    const cart = new Cart();
    cart.add(productId, variantId);
    const persist = vi.spyOn(Storage.prototype, 'setItem');
    const input = [
      { ...line, productName: 'Historical name', unitAmount: '1.00' },
      { ...line, variantId: second, quantity: 3 },
    ];
    expect(cart.addLines(input)).toBe('');
    expect(cart.lines()).toEqual([
      { ...line, quantity: 3 },
      { ...line, variantId: second, quantity: 3 },
    ]);
    expect(persist).toHaveBeenCalledOnce();
    expect(new Cart().lines()).toEqual(cart.lines());
    expect(input[0].quantity).toBe(2);
  });
  it.each([0, -1, 100, 1.5, NaN])(
    'rejects invalid quantity %s without a partial change',
    (quantity) => {
      const cart = new Cart();
      cart.add(productId, variantId);
      const before = cart.lines();
      const persist = vi.spyOn(Storage.prototype, 'setItem');
      expect(
        cart.addLines([
          { ...line, variantId: second },
          { ...line, quantity },
        ]),
      ).not.toBe('');
      expect(cart.lines()).toBe(before);
      expect(cart.count()).toBe(1);
      expect(persist).not.toHaveBeenCalled();
    },
  );
  it.each(['invalid', '00000000-0000-0000-0000-000000000000'])(
    'rejects invalid identifiers %s without partial addition',
    (id) => {
      const cart = new Cart();
      expect(cart.addLines([line, { ...line, variantId: id }])).not.toBe('');
      expect(cart.lines()).toEqual([]);
      expect(cart.addLines([{ ...line, productId: id }])).not.toBe('');
    },
  );
  it('merges repeated identifiers and normalizes case', () => {
    const cart = new Cart();
    const productId = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA',
      variantId = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB';
    expect(
      cart.addLines([
        { productId, variantId, quantity: 1 },
        { productId: productId.toLowerCase(), variantId: variantId.toLowerCase(), quantity: 2 },
      ]),
    ).toBe('');
    expect(cart.lines()).toEqual([
      { productId: productId.toLowerCase(), variantId: variantId.toLowerCase(), quantity: 3 },
    ]);
  });
  it('refuses merged quantity above 99 while retaining existing values', () => {
    const cart = new Cart();
    cart.addLines([{ ...line, quantity: 98 }]);
    const before = cart.lines();
    expect(
      cart.addLines([
        { ...line, variantId: second },
        { ...line, quantity: 2 },
      ]),
    ).toContain('99');
    expect(cart.lines()).toBe(before);
    expect(cart.addLines([{ ...line, quantity: 1 }])).toBe('');
    expect(cart.count()).toBe(99);
  });
  it('enforces capacity on the merged cart without saving a prefix', () => {
    const cart = new Cart();
    const lines = Array.from({ length: 19 }, (_, i) => ({
      productId,
      variantId: '20000000-0000-0000-0000-' + String(i + 1).padStart(12, '0'),
      quantity: 1,
    }));
    cart.addLines(lines);
    const before = cart.lines();
    cart.warning.set('Existing warning');
    expect(
      cart.addLines([
        { ...line, variantId: '20000000-0000-0000-0000-000000000020' },
        { ...line, variantId: '20000000-0000-0000-0000-000000000021' },
      ]),
    ).toContain('20');
    expect(cart.lines()).toBe(before);
    expect(cart.warning()).toBe('Existing warning');
    expect(cart.addLines([{ ...line, variantId: '20000000-0000-0000-0000-000000000020' }])).toBe(
      '',
    );
    expect(cart.lines()).toHaveLength(20);
  });
  it('rejects an empty or oversized source order', () => {
    const cart = new Cart();
    expect(cart.addLines([])).not.toBe('');
    expect(cart.addLines(Array.from({ length: 21 }, () => line))).not.toBe('');
    expect(cart.count()).toBe(0);
  });
  it('keeps the additions in memory if browser storage is unavailable', () => {
    const cart = new Cart();
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('Offline storage');
    });
    expect(cart.addLines([line])).toBe('');
    expect(cart.lines()).toEqual([line]);
    expect(cart.warning()).toContain('niet in deze browser');
  });
});
