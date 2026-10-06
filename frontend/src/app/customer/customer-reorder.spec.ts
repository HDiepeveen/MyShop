import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { Cart } from '../shop/cart';
import { CustomerOrderApi, CustomerOrderDetail as Detail } from './customer-order.api';
import { CustomerOrderDetail } from './customer-order-detail';
const productId = '10000000-0000-0000-0000-000000000001',
  variantId = '20000000-0000-0000-0000-000000000001';
const order: Detail = {
  id: 'order',
  number: 'MS-1',
  placedAt: '2026-10-06T10:00:00Z',
  paymentMethod: 'payLater',
  status: 'shipped',
  customer: { name: 'Ada', email: 'ada@example.test' },
  deliveryAddress: {
    addressLine: 'Street 1',
    postalCode: '1234 AB',
    city: 'Utrecht',
    countryCode: 'NL',
  },
  paymentInstructions: null,
  paidAt: null,
  shippedAt: null,
  shippingCarrier: null,
  trackingCode: null,
  cancelledAt: null,
  refundedAt: null,
  revision: 'r',
  lines: [
    {
      productId,
      variantId,
      quantity: 2,
      productName: 'Old name',
      variantName: 'Old variant',
      unitAmount: '1.00',
      currency: 'EUR',
      totalAmount: '2.00',
    },
  ],
  totals: [],
};
beforeEach(() => localStorage.removeItem('myshop.cart.v1'));
afterEach(() => {
  localStorage.removeItem('myshop.cart.v1');
  vi.restoreAllMocks();
});
describe('Ordering previous articles again', () => {
  function setup() {
    const params = new BehaviorSubject(convertToParamMap({ id: 'order' }));
    TestBed.configureTestingModule({
      imports: [CustomerOrderDetail],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: params, queryParamMap: of(convertToParamMap({})) },
        },
        { provide: CustomerOrderApi, useValue: { get: vi.fn(() => of(order)) } },
      ],
    });
    const fixture = TestBed.createComponent(CustomerOrderDetail);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    return {
      fixture,
      page: fixture.componentInstance,
      cart: TestBed.inject(Cart),
      navigate,
      params,
    };
  }
  it('prints the current snapshot only after loading and outside cancellation', () => {
    const { fixture, page } = setup();
    const print = vi.spyOn(window, 'print').mockImplementation(() => {});
    page.printOrder({ ...order });
    page.cancelling.set(true);
    page.printOrder(order);
    page.cancelling.set(false);
    page.loading.set(true);
    page.printOrder(order);
    expect(print).not.toHaveBeenCalled();
    page.loading.set(false);
    fixture.detectChanges();
    const button = (
      Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[]
    ).find((button) => button.textContent?.includes('afdrukken'))!;
    button.click();
    expect(print).toHaveBeenCalledOnce();
    expect(fixture.nativeElement.textContent).toContain('MS-1');
    expect(fixture.nativeElement.textContent).toContain('Old name');
    expect(fixture.nativeElement.textContent).toContain('Street 1');
    expect(fixture.nativeElement.classList.contains('printable-order')).toBe(true);
  });
  it('adds original identifiers and quantities once and opens the cart', () => {
    const { page, cart, navigate } = setup();
    const confirm = vi.spyOn(window, 'confirm');
    page.reorder(order);
    page.reorder(order);
    expect(cart.lines()).toEqual([{ productId, variantId, quantity: 2 }]);
    expect(navigate).toHaveBeenCalledExactlyOnceWith(['/winkel/winkelmand']);
    expect(confirm).not.toHaveBeenCalled();
    expect(page.notice()).toContain('actuele prijzen');
  });
  it('requires approval before merging an existing cart', () => {
    const { page, cart, navigate } = setup();
    cart.add(productId, variantId);
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);
    page.reorder(order);
    expect(cart.count()).toBe(1);
    expect(navigate).not.toHaveBeenCalled();
    confirm.mockReturnValue(true);
    page.reorder(order);
    expect(cart.count()).toBe(3);
    expect(navigate).toHaveBeenCalledOnce();
  });
  it('rejects stale order data and pending cancellation', () => {
    const { page, cart } = setup();
    page.reorder({ ...order });
    page.cancelling.set(true);
    page.reorder(order);
    expect(cart.count()).toBe(0);
    expect(page.reordered()).toBe(false);
  });
  it('reports overflow without changing the basket or navigating', () => {
    const { page, cart, navigate } = setup();
    cart.addLines([{ productId, variantId, quantity: 98 }]);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    page.reorder(order);
    expect(cart.count()).toBe(98);
    expect(page.actionFailure()).toContain('99');
    expect(page.reordered()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });
  it.each(['blocked', 'error'])(
    'offers a cart link when navigation is %s without adding again',
    async (result) => {
      const { fixture, page, cart, navigate } = setup();
      if (result === 'blocked') navigate.mockResolvedValue(false);
      else navigate.mockRejectedValue(new Error('Navigation'));
      page.reorder(order);
      await fixture.whenStable();
      fixture.detectChanges();
      page.reorder(order);
      expect(cart.count()).toBe(2);
      expect(navigate).toHaveBeenCalledOnce();
      expect(page.actionFailure()).toContain('via de link');
      expect(fixture.nativeElement.querySelector('a[href="/winkel/winkelmand"]')).not.toBeNull();
      expect(
        (Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[]).find(
          (button) => button.textContent?.includes('Artikelen opnieuw'),
        )!.disabled,
      ).toBe(true);
    },
  );
  it('clears the added state when switching to another order', () => {
    const { page, params } = setup();
    page.reorder(order);
    expect(page.reordered()).toBe(true);
    params.next(convertToParamMap({ id: 'other' }));
    expect(page.reordered()).toBe(false);
  });
});
