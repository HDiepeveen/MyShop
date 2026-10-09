import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { Cart } from './cart';
import { ShopCart } from './shop-cart';
import { ShopApi } from './shop.api';
import { Auth } from '../auth/auth';
import { PaymentOptionsApi } from '../checkout/payment-options.api';
import { DeliveryMethodsApi } from '../checkout/delivery-methods.api';
const productId = '10000000-0000-0000-0000-000000000001',
  variantId = '20000000-0000-0000-0000-000000000001',
  secondId = '20000000-0000-0000-0000-000000000002';
const first = { productId, variantId, quantity: 2 },
  second = { productId, variantId: secondId, quantity: 3 };
beforeEach(() => localStorage.removeItem('myshop.cart.v1'));
afterEach(() => {
  localStorage.removeItem('myshop.cart.v1');
  vi.restoreAllMocks();
});
describe('Undo cart removal', () => {
  function setup(checkoutEnabled = true) {
    const quote = vi.fn(() => of({ at: '', lines: [], totals: [] }));
    TestBed.configureTestingModule({
      imports: [ShopCart],
      providers: [
        provideRouter([]),
        { provide: Auth, useValue: { session: signal(null) } },
        { provide: ShopApi, useValue: { quote } },
        { provide: PaymentOptionsApi, useValue: { publicOptions: vi.fn(() => of({ checkoutEnabled, items: [] })) } },
        { provide: DeliveryMethodsApi, useValue: { publicMethods: vi.fn(() => of([])) } },
      ],
    });
    const cart = TestBed.inject(Cart);
    cart.addLines([first, second]);
    const fixture = TestBed.createComponent(ShopCart);
    fixture.detectChanges();
    TestBed.tick();
    return { fixture, page: fixture.componentInstance, cart, quote };
  }
  it('hides an existing cart and checkout form in catalogue-only mode without deleting its contents', () => {
    const { fixture, cart } = setup(false);
    expect(fixture.nativeElement.textContent).toContain('Bestellen is momenteel uitgeschakeld.');
    expect(fixture.nativeElement.querySelector('app-shop-checkout')).toBeNull();
    expect(fixture.nativeElement.querySelector('input[name=quantity]')).toBeNull();
    expect(cart.lines()).toEqual([first, second]);
  });
  it('restores a removed line with the original quantity and rechecks it', () => {
    const { fixture, page, cart, quote } = setup();
    page.remove(first);
    fixture.detectChanges();
    TestBed.tick();
    expect(cart.lines()).toEqual([second]);
    expect(page.undoItems()).toEqual([first]);
    page.undoRemoval();
    fixture.detectChanges();
    TestBed.tick();
    expect(cart.count()).toBe(5);
    expect(cart.lines()).toContainEqual(first);
    expect(page.undoItems()).toEqual([]);
    expect(quote).toHaveBeenLastCalledWith(cart.lines());
    page.undoRemoval();
    expect(cart.count()).toBe(5);
  });
  it('asks before clearing and restores the complete cart', () => {
    const { page, cart } = setup();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);
    page.clearCart();
    expect(cart.count()).toBe(5);
    expect(page.undoItems()).toEqual([]);
    confirm.mockReturnValue(true);
    page.clearCart();
    expect(cart.lines()).toEqual([]);
    expect(page.undoItems()).toEqual([first, second]);
    page.undoRemoval();
    expect(cart.lines()).toEqual([first, second]);
  });
  it('offers undo on the empty cart screen and retains only the last removal', () => {
    const { fixture, page } = setup();
    page.remove(first);
    page.remove(second);
    fixture.detectChanges();
    TestBed.tick();
    expect(page.undoItems()).toEqual([second]);
    expect(fixture.nativeElement.textContent).toContain('Je winkelmand is leeg');
    expect(fixture.nativeElement.textContent).toContain('Verwijdering ongedaan maken');
  });
  it('retains the current cart and undo option when restoring would exceed a limit', () => {
    const { page, cart } = setup();
    page.remove(first);
    cart.addLines([{ ...first, quantity: 99 }]);
    const before = cart.lines();
    page.undoRemoval();
    expect(cart.lines()).toBe(before);
    expect(page.error()).toContain('99');
    expect(page.undoItems()).toEqual([first]);
    cart.setQuantity(first, 97);
    page.undoRemoval();
    expect(cart.lines().find((line) => line.variantId === variantId)?.quantity).toBe(99);
  });
  it('blocks clearing, undo, quantity edits, removal and refresh while checkout is pending', () => {
    const { page, cart, quote } = setup();
    page.remove(first);
    const before = cart.lines();
    const undo = page.undoItems();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    page.checkoutBusy.set(true);
    page.clearCart();
    page.undoRemoval();
    page.remove(second);
    page.update(second, '4');
    page.refresh();
    expect(cart.lines()).toBe(before);
    expect(page.undoItems()).toBe(undo);
    expect(confirm).not.toHaveBeenCalled();
    expect(quote).toHaveBeenCalledTimes(1);
  });
  it('does not retain an undo action after a placed order', () => {
    const { page, cart } = setup();
    page.remove(first);
    page.orderPlaced({
      id: 'o',
      number: 'MS-1',
      placedAt: '',
      paymentInstructions: null,
      totals: [],
      deliveryMethod: null,
    });
    page.undoRemoval();
    expect(page.undoItems()).toEqual([]);
    expect(cart.lines()).toEqual([]);
    expect(page.checkoutBusy()).toBe(false);
  });
  it('does not overwrite the undo action for a missing cart line', () => {
    const { page } = setup();
    page.remove(first);
    page.remove(first);
    expect(page.undoItems()).toEqual([first]);
  });
});
