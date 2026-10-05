import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CustomerProfile } from './customer-profile';

describe('Customer profile', () => {
  let http: HttpTestingController;

  afterEach(() => http.verify());

  it('loads, saves and shows the stored profile', () => {
    TestBed.configureTestingModule({
      imports: [CustomerProfile],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerProfile);

    http.expectOne('/api/customer/profile').flush({
      email: 'ada@example.test',
      name: 'Ada Lovelace',
      addressLine: 'Main street 1',
      postalCode: '1234 AB',
      city: 'Utrecht',
      countryCode: 'NL',
      revision: '11111111-1111-1111-1111-111111111111',
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Profiel en afleveradres');
    expect(fixture.componentInstance.email).toBe('ada@example.test');
    expect(fixture.componentInstance.name).toBe('Ada Lovelace');

    fixture.componentInstance.name = 'Ada Byron';
    fixture.componentInstance.addressLine = 'Second street 2';
    fixture.componentInstance.postalCode = '5678 CD';
    fixture.componentInstance.city = 'Amsterdam';
    fixture.componentInstance.countryCode = 'be';
    fixture.componentInstance.save();

    const request = http.expectOne('/api/customer/profile');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      name: 'Ada Byron',
      addressLine: 'Second street 2',
      postalCode: '5678 CD',
      city: 'Amsterdam',
      countryCode: 'BE',
      revision: '11111111-1111-1111-1111-111111111111',
    });
    request.flush({
      email: 'ada@example.test',
      name: 'Ada Byron',
      addressLine: 'Second street 2',
      postalCode: '5678 CD',
      city: 'Amsterdam',
      countryCode: 'BE',
      revision: '22222222-2222-2222-2222-222222222222',
    });
    fixture.detectChanges();

    expect(fixture.componentInstance.countryCode).toBe('BE');
    expect(fixture.nativeElement.textContent).toContain('Je profiel is opgeslagen.');
  });

  it('shows a loading error', () => {
    TestBed.configureTestingModule({
      imports: [CustomerProfile],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerProfile);

    http.expectOne('/api/customer/profile').flush({}, { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Er ging iets mis. Probeer het opnieuw.');
  });
});
