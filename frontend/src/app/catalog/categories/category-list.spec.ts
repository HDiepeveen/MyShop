import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CategoryList } from './category-list';

describe('CategoryList', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CategoryList],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
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
  it('creates only a root category and prevents double submission', () => {
    const fixture = TestBed.createComponent(CategoryList);
    http.expectOne((r) => r.url === '/api/categories').flush([]);
    fixture.componentInstance.name = ' Wonen ';
    fixture.componentInstance.create();
    fixture.componentInstance.create();
    const request = http.expectOne('/api/categories');
    expect(request.request.body).toEqual({ name: 'Wonen', parentCategoryId: null });
    request.flush({ id: 'category', name: 'Wonen' });
    http.expectOne((r) => r.url === '/api/categories' && r.method === 'GET').flush([]);
    expect(fixture.componentInstance.saved()).toContain('Wonen');
  });
  it('allows returning from an empty page after an exact full page', () => {
    const fixture = TestBed.createComponent(CategoryList);
    http
      .expectOne((r) => r.url === '/api/categories')
      .flush(
        Array.from({ length: 20 }, (_, i) => ({
          id: String(i),
          name: 'Category ' + i,
          isRoot: true,
          directChildCount: 0,
        })),
      );
    fixture.componentInstance.changePage(20);
    http.expectOne((r) => r.params.get('offset') === '20').flush([]);
    fixture.detectChanges();
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button'),
    ) as HTMLButtonElement[];
    expect(buttons.find((b) => b.textContent?.trim() === 'Vorige')?.disabled).toBe(false);
    expect(buttons.find((b) => b.textContent?.trim() === 'Volgende')?.disabled).toBe(true);
  });
});
