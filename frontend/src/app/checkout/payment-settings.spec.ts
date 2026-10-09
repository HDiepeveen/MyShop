import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PaymentSettings } from './payment-settings';

const revision = '7732cb95-5f60-4f20-82f7-499a020a9d3e';

describe('Payment settings', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PaymentSettings],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function load(fixture: ComponentFixture<PaymentSettings>) {
    http.expectOne('/api/payment-options').flush({
      payLaterEnabled: true,
      onlinePaymentEnabled: false,
      payLaterInstructions: 'Betaal binnen 14 dagen.',
      onlinePaymentConfigured: false,
      onlinePaymentProvider: null,
      revision,
    });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('shows the stored choice and keeps unavailable online payment disabled', async () => {
    const fixture = TestBed.createComponent(PaymentSettings);
    await load(fixture);

    const inputs = fixture.nativeElement.querySelectorAll('input') as NodeListOf<HTMLInputElement>;
    expect(inputs[0].checked).toBe(true);
    expect(fixture.nativeElement.querySelector('input[name=online]').disabled).toBe(true);
    expect(fixture.componentInstance.payLaterInstructions).toBe('Betaal binnen 14 dagen.');
    expect(fixture.nativeElement.textContent).toContain('nadat een betaalprovider is gekoppeld');
  });

  it('saves catalogue-only mode without clearing the payment options', async () => {
    const fixture = TestBed.createComponent(PaymentSettings);
    await load(fixture);
    const page = fixture.componentInstance;
    page.checkoutEnabled = false;
    page.save(page.state()!.data!);
    const request = http.expectOne('/api/payment-options');
    expect(request.request.body.checkoutEnabled).toBe(false);
    expect(request.request.body.payLaterEnabled).toBe(true);
    request.flush({});
    http.expectOne('/api/payment-options').flush({
      checkoutEnabled: false, payLaterEnabled: true, onlinePaymentEnabled: false,
      onlinePaymentConfigured: false, onlinePaymentProvider: null,
      payLaterInstructions: 'Betaal binnen 14 dagen.', revision,
    });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.checkoutEnabled).toBe(false);
    expect(page.payLater).toBe(true);
  });
  it('validates and saves an allowed selection with its revision', async () => {
    const fixture = TestBed.createComponent(PaymentSettings);
    await load(fixture);
    const page = fixture.componentInstance;

    page.payLater = false;
    page.save(page.state()!.data!);
    expect(page.validation()).toContain('minimaal één');
    http.expectNone((request) => request.method === 'PUT');

    page.payLater = true;
    page.payLaterInstructions = '  Nieuwe instructies.  ';
    page.save(page.state()!.data!);
    const update = http.expectOne('/api/payment-options');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual({
      checkoutEnabled: true,
      payLaterEnabled: true,
      onlinePaymentEnabled: false,
      payLaterInstructions: 'Nieuwe instructies.',
      revision,
    });
    update.flush({
      payLaterEnabled: true,
      onlinePaymentEnabled: false,
      payLaterInstructions: 'Nieuwe instructies.',
      onlinePaymentConfigured: false,
      onlinePaymentProvider: null,
      revision: '98e2f651-835c-4821-8905-ec8c054256ea',
    });
    http.expectOne('/api/payment-options').flush({
      payLaterEnabled: true,
      onlinePaymentEnabled: false,
      payLaterInstructions: 'Nieuwe instructies.',
      onlinePaymentConfigured: false,
      onlinePaymentProvider: null,
      revision: '98e2f651-835c-4821-8905-ec8c054256ea',
    });
    expect(page.message()).toBe('Betaalopties opgeslagen.');
  });
  it('shows the prepared provider when online payment is configured', async () => {
    const fixture = TestBed.createComponent(PaymentSettings);
    http.expectOne('/api/payment-options').flush({
      payLaterEnabled: true,
      onlinePaymentEnabled: false,
      payLaterInstructions: null,
      onlinePaymentConfigured: true,
      onlinePaymentProvider: 'TestPay',
      revision,
    });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const inputs = fixture.nativeElement.querySelectorAll('input') as NodeListOf<HTMLInputElement>;
    expect(inputs[1].disabled).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Online betalen is voorbereid via TestPay.');
  });
});
