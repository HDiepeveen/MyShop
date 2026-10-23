import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { ShopCart } from './shop-cart';
import { Cart } from './cart';
import { ShopApi } from './shop.api';
import { Auth } from '../auth/auth';
import { OrderApi } from '../checkout/order.api';
import { PaymentOptionsApi } from '../checkout/payment-options.api';
import { DeliveryMethodsApi, PublicDeliveryMethod } from '../checkout/delivery-methods.api';

describe('Cart total including delivery', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  it('updates the total with the selected delivery without changing the subtotal', async () => {
    const delivery = new Subject<readonly PublicDeliveryMethod[]>();
    const line = { productId: '10000000-0000-0000-0000-000000000001', variantId: '20000000-0000-0000-0000-000000000001', quantity: 1 };
    TestBed.configureTestingModule({ providers: [
      provideRouter([]),
      { provide: Auth, useValue: { session: signal(null) } },
      { provide: ShopApi, useValue: { quote: () => of({ at: '', lines: [{ ...line, name: 'Koffie', amount: '45.98', currency: 'EUR' }], totals: [{ currency: 'EUR', amount: '45.98' }] }) } },
      { provide: OrderApi, useValue: {} },
      { provide: PaymentOptionsApi, useValue: { publicOptions: () => of({ checkoutEnabled: true, items: [] }) } },
      { provide: DeliveryMethodsApi, useValue: { publicMethods: () => delivery } },
    ] });
    TestBed.inject(Cart).addLines([line]);
    const fixture = TestBed.createComponent(ShopCart);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).not.toContain('Totaal: €');
    delivery.next([
      { id: 'post', name: 'Post', description: null, amount: '2.00', currency: 'EUR' },
      { id: 'pickup', name: 'Afhalen', description: null, amount: '0.00', currency: 'EUR' },
      { id: 'other', name: 'Andere valuta', description: null, amount: '0.30', currency: 'USD' },
    ]);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Subtotaal: € 45,98');
    expect(fixture.nativeElement.textContent).toContain('Totaal: € 47,98');
    expect(fixture.nativeElement.textContent).not.toContain('inclusief bezorgkosten');
    const text = fixture.nativeElement.textContent as string;
    expect(text.indexOf('Totaal: €')).toBeGreaterThan(text.indexOf('Hoe wil je je bestelling ontvangen?'));
    expect(text.indexOf('Totaal: €')).toBeLessThan(text.indexOf('Hoe wil je betalen?'));
    const radios = Array.from(fixture.nativeElement.querySelectorAll('input[type="radio"]')) as HTMLInputElement[];
    const pickup = radios[1];
    pickup.click();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Totaal: € 45,98');
    const other = radios[2];
    other.click();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Totaal: € 45,98');
    expect(fixture.nativeElement.textContent).toContain('Totaal: $ 0,30');
    delivery.next([]);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).not.toContain('Totaal: €');
  });
});
