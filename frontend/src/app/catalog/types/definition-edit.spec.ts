import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DefinitionEdit } from './definition-edit';
import { TypeEditState } from './type-edit-state';
describe('DefinitionEdit', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DefinitionEdit],
      providers: [TypeEditState, provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const f = TestBed.createComponent(DefinitionEdit);
    f.componentRef.setInput('typeId', 't/a');
    f.componentRef.setInput('definition', {
      id: 'd/b',
      code: 'color',
      displayName: 'Kleur',
      dataType: 'Text',
      scope: 'Product',
      isRequired: false,
      isFilterable: true,
    });
    f.detectChanges();
    return f.componentInstance;
  }
  it('renames only display name and prevents empty, unchanged or duplicate writes', () => {
    const c = setup();
    c.rename();
    c.displayName = ' ';
    c.rename();
    http.expectNone(() => true);
    c.displayName = ' Nieuwe kleur ';
    const saved = vi.fn();
    c.saved.subscribe(saved);
    c.rename();
    c.rename();
    const req = http.expectOne('/api/product-types/t%2Fa/attributes/d%2Fb/name');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ displayName: 'Nieuwe kleur' });
    req.flush(null);
    expect(saved).toHaveBeenCalledOnce();
  });

  it('changes both flags without changing immutable identity or the name', () => {
    const c = setup();
    c.configure();
    http.expectNone(() => true);
    c.required = true;
    c.filterable = false;
    c.configure();
    c.configure();
    const req = http.expectOne('/api/product-types/t%2Fa/attributes/d%2Fb/configuration');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ isRequired: true, isFilterable: false });
    req.flush(null);
  });
  it('preserves flags on failure and blocks another editor while saving', () => {
    const c = setup();
    c.required = true;
    c.configure();
    c.displayName = 'Nieuw';
    c.rename();
    http.expectNone('/api/product-types/t%2Fa/attributes/d%2Fb/name');
    http
      .expectOne('/api/product-types/t%2Fa/attributes/d%2Fb/configuration')
      .flush({}, { status: 500, statusText: 'Error' });
    expect(c.required).toBe(true);
    expect(c.busy()).toBe(false);
  });

  it('requires confirmation before removal and never repeats a failed delete', () => {
    const c = setup();
    c.remove();
    http.expectNone(() => true);
    c.confirming.set(true);
    c.confirming.set(false);
    c.remove();
    http.expectNone(() => true);
    c.confirming.set(true);
    const saved = vi.fn();
    c.saved.subscribe(saved);
    c.remove();
    c.remove();
    const req = http.expectOne('/api/product-types/t%2Fa/attributes/d%2Fb');
    expect(req.request.method).toBe('DELETE');
    req.flush({}, { status: 500, statusText: 'Error' });
    expect(saved).not.toHaveBeenCalled();
    expect(c.error()).toBeTruthy();
    http.expectNone(() => true);
    c.remove();
    http.expectOne('/api/product-types/t%2Fa/attributes/d%2Fb').flush(null);
    expect(saved).toHaveBeenCalledOnce();
  });
  it('retains a failed edit and observes the shared write lock', () => {
    const c = setup();
    c.displayName = 'Nieuw';
    TestBed.inject(TypeEditState).busy.set(true);
    c.rename();
    http.expectNone(() => true);
    TestBed.inject(TypeEditState).busy.set(false);
    c.rename();
    http
      .expectOne('/api/product-types/t%2Fa/attributes/d%2Fb/name')
      .flush({}, { status: 404, statusText: 'Missing' });
    expect(c.displayName).toBe('Nieuw');
    expect(c.error()).toBeTruthy();
    expect(c.busy()).toBe(false);
  });
});
