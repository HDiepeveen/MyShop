import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { DeliverySettings } from './delivery-settings';
import { DeliveryMethodsApi, DeliveryMethod } from './delivery-methods.api';

const method: DeliveryMethod = {
  id: 'd',
  name: 'Post',
  description: null,
  amount: '2.00',
  currency: 'EUR',
  enabled: true,
  revision: 'r',
};
describe('Delivery input validation', () => {
  function setup() {
    const response = new Subject<DeliveryMethod>();
    const api = {
      adminMethods: vi.fn(() => of([method])),
      create: vi.fn(() => response),
      update: vi.fn(() => response),
      delete: vi.fn(() => new Subject<void>()),
    };
    TestBed.configureTestingModule({
      imports: [DeliverySettings],
      providers: [provideRouter([]), { provide: DeliveryMethodsApi, useValue: api }],
    });
    const fixture = TestBed.createComponent(DeliverySettings);
    fixture.componentInstance.name = 'Post';
    return { fixture, page: fixture.componentInstance, api };
  }
  afterEach(() => vi.restoreAllMocks());
  it.each([
    'blank name',
    'long name',
    'long description',
    'negative amount',
    'three decimals',
    'exponent',
    'large amount',
    'invalid currency',
  ])('rejects %s before calling the API', (invalid) => {
    const { fixture, page, api } = setup();
    if (invalid === 'blank name') page.name = '  ';
    if (invalid === 'long name') page.name = 'x'.repeat(101);
    if (invalid === 'long description') page.description = 'x'.repeat(501);
    if (invalid === 'negative amount') page.price = '-1';
    if (invalid === 'three decimals') page.price = '1.001';
    if (invalid === 'exponent') page.price = '1e2';
    if (invalid === 'large amount') page.price = '10000000000000000.00';
    if (invalid === 'invalid currency') page.currency = 'EU1';
    fixture.detectChanges();
    page.save();
    expect(page.failure()).toBeTruthy();
    expect(page.busy()).toBe(false);
    expect(api.create).not.toHaveBeenCalled();
    expect(api.update).not.toHaveBeenCalled();
  });
  it.each(['0', '9999999999999999.99'])(
    'preserves exact amount %s and normalizes text',
    (amount) => {
      const { page, api } = setup();
      page.name = ' Post ';
      page.description = ' Details ';
      page.currency = ' eur ';
      page.price = ' ' + amount.replace('.', ',') + ' ';
      page.save();
      expect(api.create).toHaveBeenCalledWith({
        name: 'Post',
        description: 'Details',
        amount,
        currency: 'EUR',
        enabled: true,
      });
    },
  );
  it('rejects stale edit and delete objects before confirmation', () => {
    const { page, api } = setup();
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
    const old = { ...method, revision: 'old' };
    page.edit(old);
    page.remove(old);
    expect(page.editingId).toBe('');
    expect(confirm).not.toHaveBeenCalled();
    expect(api.delete).not.toHaveBeenCalled();
    page.edit(method);
    page.revision = 'old';
    page.save();
    expect(api.update).not.toHaveBeenCalled();
    expect(page.failure()).toContain('gewijzigd');
  });
  it('accepts current edit and delete objects', () => {
    const { page, api } = setup();
    page.edit(method);
    page.save();
    expect(api.update).toHaveBeenCalledWith('d', expect.objectContaining({ revision: 'r' }));
  });
});
