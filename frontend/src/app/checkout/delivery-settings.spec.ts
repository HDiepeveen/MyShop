import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DeliverySettings } from './delivery-settings';

describe('Delivery settings', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DeliverySettings],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('lists and creates a delivery method with an exact decimal amount', () => {
    const fixture = TestBed.createComponent(DeliverySettings);
    http.expectOne('/api/delivery-methods').flush([]);
    const page = fixture.componentInstance;
    page.name = 'Avondbezorging'; page.description = 'Na 18:00'; page.price = '4,95';
    page.save();
    http.expectOne('/api/auth/csrf').flush(null);
    const create = http.expectOne('/api/delivery-methods');
    expect(create.request.body).toEqual({ name: 'Avondbezorging', description: 'Na 18:00',
      amount: '4.95', currency: 'EUR', enabled: true });
    create.flush({ id: 'method', ...create.request.body, revision: 'revision' });
    http.expectOne('/api/delivery-methods').flush([]);
    expect(page.notice()).toContain('opgeslagen');
  });
});
