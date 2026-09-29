import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { TypeDetail } from './type-detail';
describe('TypeDetail', () => {
  let http: HttpTestingController;
  const params = new BehaviorSubject(convertToParamMap({ id: 't' }));
  beforeEach(() => {
    params.next(convertToParamMap({ id: 't' }));
    TestBed.configureTestingModule({
      imports: [TypeDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('shows definition labels, scope and constraints', () => {
    const fixture = TestBed.createComponent(TypeDetail);
    http
      .expectOne('/api/product-types/t')
      .flush({
        id: 't',
        name: 'Kleding',
        attributeDefinitions: [
          {
            id: 'a',
            code: 'material',
            displayName: 'Materiaal',
            dataType: 'Text',
            scope: 'Product',
            isRequired: true,
            isFilterable: false,
          },
        ],
      });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Materiaal');
    expect(fixture.nativeElement.textContent).toContain('Tekst · Verplicht · Niet filterbaar');
  });

  it('removes a previous success message when switching product types', () => {
    const fixture = TestBed.createComponent(TypeDetail);
    http
      .expectOne('/api/product-types/t')
      .flush({ id: 't', name: 'Type', attributeDefinitions: [] });
    fixture.componentInstance.notice.set('Old success');
    params.next(convertToParamMap({ id: 'other' }));
    http
      .expectOne('/api/product-types/other')
      .flush({ id: 'other', name: 'Other', attributeDefinitions: [] });
    expect(fixture.componentInstance.notice()).toBe('');
  });
  it('cancels an old request when navigating and exposes a retry after failure', () => {
    const fixture = TestBed.createComponent(TypeDetail);
    const old = http.expectOne('/api/product-types/t');
    params.next(convertToParamMap({ id: 'other' }));
    expect(old.cancelled).toBe(true);
    http.expectOne('/api/product-types/other').flush({}, { status: 404, statusText: 'Missing' });
    expect(fixture.componentInstance.state()?.error).toBeTruthy();
    fixture.componentInstance.reload();
    http
      .expectOne('/api/product-types/other')
      .flush({ id: 'other', name: 'Other', attributeDefinitions: [] });
    expect(fixture.componentInstance.state()?.error).toBe('');
  });
});
