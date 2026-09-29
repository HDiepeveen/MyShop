import { TypeEditState } from './type-edit-state';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TypeEdit } from './type-edit';
describe('TypeEdit', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TypeEdit],
      providers: [TypeEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const f = TestBed.createComponent(TypeEdit);
    f.componentRef.setInput('type', { id: 't/a', name: 'Kleding', attributeDefinitions: [] });
    f.detectChanges();
    return f.componentInstance;
  }
  it('rejects empty and unchanged names, trims and blocks duplicate writes', () => {
    const c = setup();
    c.rename();
    c.name = ' ';
    c.rename();
    http.expectNone(() => true);
    c.name = ' Nieuwe naam ';
    const saved = vi.fn();
    c.saved.subscribe(saved);
    c.rename();
    c.rename();
    const req = http.expectOne('/api/product-types/t%2Fa/name');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ name: 'Nieuwe naam' });
    req.flush(null);
    expect(saved).toHaveBeenCalledOnce();
  });

  it('respects a write already running in another editor', () => {
    const c = setup();
    c.name = 'Nieuw';
    TestBed.inject(TypeEditState).busy.set(true);
    c.rename();
    http.expectNone(() => true);
    TestBed.inject(TypeEditState).busy.set(false);
    c.rename();
    http.expectOne('/api/product-types/t%2Fa/name').flush(null);
  });
  it('retains input after failure and allows an explicit retry', () => {
    const c = setup();
    c.name = 'Nieuw';
    c.rename();
    http
      .expectOne('/api/product-types/t%2Fa/name')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(c.name).toBe('Nieuw');
    expect(c.busy()).toBe(false);
    expect(c.error()).toBeTruthy();
    c.rename();
    http.expectOne('/api/product-types/t%2Fa/name').flush(null);
  });
});
