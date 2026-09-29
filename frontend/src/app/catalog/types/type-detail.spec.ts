import { By } from '@angular/platform-browser';
import { TypeEdit } from './type-edit';
import { DefinitionCreate } from './definition-create';
import { DefinitionEdit } from './definition-edit';
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
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: params,
            queryParamMap: new BehaviorSubject(
              convertToParamMap({ search: 'shirt', offset: '20' }),
            ),
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('preserves list search and page in the back link', () => {
    const fixture = TestBed.createComponent(TypeDetail);
    http.expectOne('/api/product-types/t').flush({}, { status: 404, statusText: 'Missing' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('a.back').getAttribute('href')).toContain(
      'search=shirt',
    );
    expect(fixture.nativeElement.querySelector('a.back').getAttribute('href')).toContain(
      'offset=20',
    );
  });
  it('shows definition labels, scope and constraints', () => {
    const fixture = TestBed.createComponent(TypeDetail);
    http.expectOne('/api/product-types/t').flush({
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
    expect(
      fixture.nativeElement.querySelector('a[href="/producten?productTypeId=t"]'),
    ).not.toBeNull();
    TestBed.tick();
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 2, isInUse: true });
    expect(fixture.nativeElement.textContent).toContain('Materiaal');
    expect(fixture.nativeElement.textContent).toContain('Tekst · Verplicht · Niet filterbaar');
  });

  it('shares the write lock across type and definition editors and reloads persisted state', () => {
    const f = TestBed.createComponent(TypeDetail);
    const definition = {
      id: 'a',
      code: 'material',
      displayName: 'Materiaal',
      dataType: 'Text',
      scope: 'Product',
      isRequired: false,
      isFilterable: false,
    };
    const type = { id: 't', name: 'Kleding', attributeDefinitions: [definition] };
    http.expectOne('/api/product-types/t').flush(type);
    f.detectChanges();
    TestBed.tick();
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 1, isInUse: true });
    const editor = f.debugElement.query(By.directive(DefinitionEdit))
      .componentInstance as DefinitionEdit;
    const rename = f.debugElement.query(By.directive(TypeEdit)).componentInstance as TypeEdit;
    const create = f.debugElement.query(By.directive(DefinitionCreate))
      .componentInstance as DefinitionCreate;
    f.componentInstance.notice.set('Old notice');
    editor.required = true;
    editor.configure();
    f.detectChanges();
    expect(f.componentInstance.notice()).toBe('');
    rename.name = 'Nieuw';
    rename.rename();
    create.displayName = 'Kleur';
    create.code = 'color';
    create.create();
    http.expectNone('/api/product-types/t/name');
    http.expectNone('/api/product-types/t/attributes');
    http.expectOne('/api/product-types/t/attributes/a/configuration').flush(null);
    expect(f.componentInstance.state()?.loading).toBe(true);
    f.detectChanges();
    http
      .expectOne('/api/product-types/t')
      .flush({ ...type, attributeDefinitions: [{ ...definition, isRequired: true }] });
    f.detectChanges();
    TestBed.tick();
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 1, isInUse: true });
    expect(f.nativeElement.textContent).toContain('Verplicht');
    expect(f.componentInstance.notice()).toContain('instellingen');
  });
  it('does not reset an active write on equivalent route parameters', () => {
    const f = TestBed.createComponent(TypeDetail);
    http.expectOne('/api/product-types/t').flush({}, { status: 404, statusText: 'Missing' });
    f.componentInstance.editState.busy.set(true);
    params.next(convertToParamMap({ id: 't' }));
    expect(f.componentInstance.editState.busy()).toBe(true);
    http.expectNone('/api/product-types/t');
    params.next(convertToParamMap({ id: 'other' }));
    expect(f.componentInstance.editState.busy()).toBe(false);
    http.expectOne('/api/product-types/other').flush({}, { status: 404, statusText: 'Missing' });
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
