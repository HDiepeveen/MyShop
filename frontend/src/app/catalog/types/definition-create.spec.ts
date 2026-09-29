import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DefinitionCreate } from './definition-create';
describe('DefinitionCreate', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DefinitionCreate],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const fixture = TestBed.createComponent(DefinitionCreate);
    fixture.componentRef.setInput('typeId', 't');
    fixture.detectChanges();
    return fixture.componentInstance;
  }
  it('validates codes and sends numeric enum values, without duplicate submissions', () => {
    const editor = setup();
    editor.displayName = ' Materiaal ';
    for (const code of ['', 'Upper', '1code', 'has space', 'a'.repeat(65)]) {
      editor.code = code;
      editor.create();
    }
    http.expectNone('/api/product-types/t/attributes');
    editor.code = 'material';
    editor.scope = 1;
    editor.dataType = 5;
    editor.required = true;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.create();
    editor.create();
    const request = http.expectOne('/api/product-types/t/attributes');
    expect(request.request.body).toEqual({
      code: 'material',
      displayName: 'Materiaal',
      scope: 1,
      dataType: 5,
      isRequired: true,
      isFilterable: false,
    });
    expect(saved).not.toHaveBeenCalled();
    request.flush({ id: 'a' });
    expect(saved).toHaveBeenCalledOnce();
  });
  it('retains the definition when the server rejects a duplicate code', () => {
    const editor = setup();
    editor.code = 'material';
    editor.displayName = 'Materiaal';
    editor.create();
    http
      .expectOne('/api/product-types/t/attributes')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(editor.code).toBe('material');
    expect(editor.busy()).toBe(false);
    expect(editor.error()).toBeTruthy();
  });
});
