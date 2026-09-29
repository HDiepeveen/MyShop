import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { CategoryList } from './category-list';

describe('CategoryList', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CategoryList],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
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
