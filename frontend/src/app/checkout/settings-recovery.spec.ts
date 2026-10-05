import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { PaymentSettings } from './payment-settings';
import { AdminPaymentOptions, PaymentOptionsApi } from './payment-options.api';
import { DeliverySettings } from './delivery-settings';
import { DeliveryMethod, DeliveryMethodsApi } from './delivery-methods.api';

const options: AdminPaymentOptions = {
  payLaterEnabled: true, onlinePaymentEnabled: false, payLaterInstructions: null,
  onlinePaymentConfigured: false, onlinePaymentProvider: null, revision: 'revision-1',
};
const method: DeliveryMethod = {
  id: 'delivery-1', name: 'PostNL', description: null, amount: '4.95',
  currency: 'EUR', enabled: true, revision: 'revision-1',
};

describe('Checkout settings recovery', () => {
  afterEach(() => vi.restoreAllMocks());

  function paymentFixture() {
    const response = new Subject<AdminPaymentOptions>();
    const update = new Subject<void>();
    const api = { adminOptions: vi.fn(() => response), update: vi.fn(() => update) };
    TestBed.configureTestingModule({ imports: [PaymentSettings], providers: [{ provide: PaymentOptionsApi, useValue: api }] });
    const fixture = TestBed.createComponent(PaymentSettings);
    response.next(options);
    fixture.detectChanges();
    return { fixture, response, update, api };
  }

  it('saves payment options only once during a pending update', () => {
    const { fixture, api } = paymentFixture();
    fixture.componentInstance.save(options);
    fixture.componentInstance.payLater = false;
    fixture.componentInstance.save(options);
    expect(api.update).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.validation()).toBe('');
  });

  it('rejects stale payment options after reloading', () => {
    const { fixture, response, api } = paymentFixture();
    response.next({ ...options, revision: 'revision-2' });
    fixture.componentInstance.save(options);
    expect(api.update).not.toHaveBeenCalled();
  });

  it('does not reload payment options while saving or already loading', () => {
    const { fixture, update, api } = paymentFixture();
    fixture.componentInstance.save(options);
    fixture.componentInstance.reload();
    expect(api.adminOptions).toHaveBeenCalledTimes(1);
    api.adminOptions.mockReturnValue(new Subject<AdminPaymentOptions>());
    update.next();
    fixture.componentInstance.reload();
    expect(api.adminOptions).toHaveBeenCalledTimes(2);
  });

  it('clears old validation when payment settings are reloaded', () => {
    const { fixture } = paymentFixture();
    fixture.componentInstance.payLater = false;
    fixture.componentInstance.save(options);
    expect(fixture.componentInstance.validation()).not.toBe('');
    fixture.componentInstance.reload();
    expect(fixture.componentInstance.validation()).toBe('');
  });

  function deliveryFixture() {
    const response = new Subject<readonly DeliveryMethod[]>();
    const operation = new Subject<DeliveryMethod>();
    const api = {
      adminMethods: vi.fn(() => response), create: vi.fn(() => operation),
      update: vi.fn(() => operation), delete: vi.fn(() => operation),
    };
    TestBed.configureTestingModule({ imports: [DeliverySettings], providers: [{ provide: DeliveryMethodsApi, useValue: api }] });
    return { fixture: TestBed.createComponent(DeliverySettings), response, operation, api };
  }

  it('ignores duplicate delivery loads and loads during a pending save', () => {
    const { fixture, response, api } = deliveryFixture();
    fixture.componentInstance.load();
    expect(api.adminMethods).toHaveBeenCalledTimes(1);
    response.next([method]);
    fixture.componentInstance.save();
    fixture.componentInstance.load();
    expect(api.adminMethods).toHaveBeenCalledTimes(1);
  });

  it('retries a failed delivery load through the error screen', () => {
    const { fixture, response, api } = deliveryFixture();
    response.error(new Error('Offline'));
    const retry = new Subject<readonly DeliveryMethod[]>();
    api.adminMethods.mockReturnValue(retry);
    fixture.detectChanges();
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    buttons.find((button) => button.textContent?.includes('Opnieuw proberen'))!.click();
    retry.next([method]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('PostNL');
    expect(fixture.componentInstance.loadFailed()).toBe(false);
  });

  it('prevents saving and deleting delivery options while loading', () => {
    const { fixture, api } = deliveryFixture();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.save();
    fixture.componentInstance.remove(method);
    expect(api.create).not.toHaveBeenCalled();
    expect(api.delete).not.toHaveBeenCalled();
    expect(confirm).not.toHaveBeenCalled();
  });

  it('keeps the edited delivery option while an update is pending', () => {
    const { fixture, response, operation, api } = deliveryFixture();
    response.next([method]);
    fixture.componentInstance.edit(method);
    fixture.componentInstance.save();
    fixture.componentInstance.edit({ ...method, id: 'delivery-2', name: 'DHL' });
    expect(fixture.componentInstance.editingId).toBe(method.id);
    expect(fixture.componentInstance.name).toBe(method.name);
    expect(api.update).toHaveBeenCalledTimes(1);
    operation.error(new Error('Offline'));
    fixture.componentInstance.edit({ ...method, id: 'delivery-2', name: 'DHL' });
    expect(fixture.componentInstance.editingId).toBe('delivery-2');
  });

  it('prevents clearing pending edits and resets the form after success', () => {
    const { fixture, response, operation } = deliveryFixture();
    response.next([method]);
    fixture.componentInstance.edit(method);
    fixture.componentInstance.save();
    fixture.componentInstance.clear();
    expect(fixture.componentInstance.editingId).toBe(method.id);
    operation.next(method);
    expect(fixture.componentInstance.editingId).toBe('');
    expect(fixture.componentInstance.name).toBe('');
    expect(fixture.componentInstance.notice()).toContain('opgeslagen');
  });

  it('locks delivery fields while saving and preserves input after failure', async () => {
    const { fixture, response, operation } = deliveryFixture();
    response.next([method]);
    fixture.componentInstance.edit(method);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.componentInstance.name = 'Avondbezorging';
    fixture.componentInstance.save();
    fixture.detectChanges();
    await fixture.whenStable();
    const fields = Array.from(fixture.nativeElement.querySelectorAll('input, textarea')) as (HTMLInputElement | HTMLTextAreaElement)[];
    expect(fields.length).toBe(5);
    expect(fields.every((field) => field.disabled)).toBe(true);
    operation.error(new Error('Offline'));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fields.every((field) => !field.disabled)).toBe(true);
    expect(fixture.componentInstance.name).toBe('Avondbezorging');
  });
});
