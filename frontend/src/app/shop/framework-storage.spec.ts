import { migrateFrameworkStorage } from './framework-storage';

describe('Framework storage rename', () => {
  afterEach(() => localStorage.clear());
  it.each(['cart.v1', 'pending-payment.v1'])('preserves existing %s under the new name and removes the old entry', suffix => {
    localStorage.setItem('myshop.' + suffix, 'saved-value');
    const key = migrateFrameworkStorage(suffix);
    expect(key).toBe('HDWebFramework.' + suffix);
    expect(localStorage.getItem(key)).toBe('saved-value');
    expect(localStorage.getItem('myshop.' + suffix)).toBeNull();
  });
  it('keeps the new value when both entries exist', () => {
    localStorage.setItem('myshop.cart.v1', 'old-cart');
    localStorage.setItem('HDWebFramework.cart.v1', 'new-cart');
    expect(localStorage.getItem(migrateFrameworkStorage('cart.v1'))).toBe('new-cart');
    expect(localStorage.getItem('myshop.cart.v1')).toBeNull();
  });
});
