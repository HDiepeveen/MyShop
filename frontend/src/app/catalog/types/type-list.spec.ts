import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { TypeList } from './type-list';

describe('TypeList', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TypeList],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    const query = new BehaviorSubject(convertToParamMap({}));
    TestBed.overrideProvider(ActivatedRoute, { useValue: { queryParamMap: query } });
    http = TestBed.inject(HttpTestingController);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockImplementation((_commands, extras) => {
      const values: Record<string, unknown> = {};
      for (const key of query.value.keys) values[key] = query.value.get(key);
      Object.assign(values, extras?.queryParams);
      for (const key of Object.keys(values)) if (values[key] === null) delete values[key];
      query.next(convertToParamMap(values));
      return Promise.resolve(true);
    });
  });
  afterEach(() => http.verify());
  it('prevents duplicate submits and refreshes after success', () => {
    const fixture = TestBed.createComponent(TypeList);
    http.expectOne((r) => r.url === '/api/product-types').flush([]);
    fixture.componentInstance.name = ' Kleding ';
    fixture.componentInstance.create();
    fixture.componentInstance.create();
    const request = http.expectOne('/api/product-types');
    expect(request.request.body).toEqual({ name: 'Kleding' });
    request.flush({ id: 'type', name: 'Kleding' });
    http
      .expectOne((r) => r.url === '/api/product-types' && r.method === 'GET')
      .flush([{ id: 'type', name: 'Kleding', attributeDefinitionCount: 0 }]);
    expect(fixture.componentInstance.name).toBe('');
    expect(fixture.componentInstance.saved()).toContain('Kleding');
  });
  it('retains input after failure and does not refresh the list', () => {
    const fixture = TestBed.createComponent(TypeList);
    http.expectOne((r) => r.url === '/api/product-types').flush([]);
    fixture.componentInstance.name = 'Type';
    fixture.componentInstance.create();
    http.expectOne('/api/product-types').flush({}, { status: 500, statusText: 'Error' });
    expect(fixture.componentInstance.name).toBe('Type');
    expect(fixture.componentInstance.saving()).toBe(false);
    expect(fixture.componentInstance.saveError()).not.toBe('');
  });
});
