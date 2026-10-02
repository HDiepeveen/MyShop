import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CustomerManagementList } from './customer-management-list';
import { CustomerManagementDetail } from './customer-management-detail';

describe('Customer management', () => {
  let http: HttpTestingController;
  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
  });
  it('searches and lists customers', () => {
    TestBed.configureTestingModule({
      imports: [CustomerManagementList],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerManagementList);
    http
      .expectOne((r) => r.url === '/api/customers' && r.params.get('offset') === '0')
      .flush({ items: [], offset: 0, limit: 20, totalCount: 0 });
    fixture.componentInstance.searchText = ' Ada ';
    fixture.componentInstance.applySearch();
    http
      .expectOne((r) => r.url === '/api/customers' && r.params.get('search') === 'Ada')
      .flush({
        items: [
          {
            id: 'user',
            email: 'ada@example.com',
            name: 'Ada',
            isLocked: false,
            lockedUntil: null,
            orderCount: 2,
          },
        ],
        offset: 0,
        limit: 20,
        totalCount: 1,
      });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('ada@example.com');
    expect(fixture.nativeElement.textContent).toContain('Zoekterm wissen');

    fixture.componentInstance.clearSearch();
    http
      .expectOne(
        (r) =>
          r.url === '/api/customers' &&
          r.params.get('offset') === '0' &&
          !r.params.has('search'),
      )
      .flush({ items: [], offset: 0, limit: 20, totalCount: 0 });
    expect(fixture.componentInstance.searchText).toBe('');
  });
  it('unblocks a customer after confirmation', () => {
    TestBed.configureTestingModule({
      imports: [CustomerManagementDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: new BehaviorSubject(convertToParamMap({ id: 'user' })) },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerManagementDetail);
    http.expectOne('/api/customers/user').flush({
      id: 'user',
      email: 'ada@example.com',
      name: 'Ada',
      isLocked: true,
      lockedUntil: '9999-12-31T23:59:59Z',
      orderCount: 2,
      addressLine: 'Straat 1',
      postalCode: '1234 AB',
      city: 'Utrecht',
      countryCode: 'NL',
      lastOrderAt: '2026-10-01T10:00:00Z',
      revision: 'old',
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Account deblokkeren');
    expect(fixture.nativeElement.textContent).toContain('Straat 1');

    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.setLocked(fixture.componentInstance.customer()!, false);
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/customers/user/access');
    expect(request.request.body).toEqual({ locked: false, revision: 'old' });
    request.flush({ locked: false, revision: 'new' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Het klantaccount is gedeblokkeerd.');
    expect(fixture.nativeElement.textContent).toContain('Account blokkeren');
  });
  it('blocks a customer after confirmation', () => {
    TestBed.configureTestingModule({
      imports: [CustomerManagementDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: new BehaviorSubject(convertToParamMap({ id: 'user' })) },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CustomerManagementDetail);
    http
      .expectOne('/api/customers/user')
      .flush({
        id: 'user',
        email: 'ada@example.com',
        name: 'Ada',
        isLocked: false,
        lockedUntil: null,
        orderCount: 2,
        addressLine: null,
        postalCode: null,
        city: null,
        countryCode: null,
        lastOrderAt: null,
        revision: 'old',
      });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.componentInstance.setLocked(fixture.componentInstance.customer()!, true);
    http.expectOne('/api/auth/csrf').flush(null);
    const request = http.expectOne('/api/customers/user/access');
    expect(request.request.body).toEqual({ locked: true, revision: 'old' });
    request.flush({ locked: true, revision: 'new' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Het klantaccount is geblokkeerd.');
  });
});
