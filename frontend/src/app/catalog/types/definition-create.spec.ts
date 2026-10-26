import { TypeEditState } from './type-edit-state';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DefinitionCreate } from './definition-create';
describe('DefinitionCreate', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DefinitionCreate],
      providers: [TypeEditState, provideHttpClient(), provideHttpClientTesting()],
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
  it('suggests lowercase valid codes and follows name edits until the code is customized', () => {
    const editor=setup();
    editor.changeName('Kenteken');expect(editor.code).toBe('kenteken');
    editor.changeName('Transmissie type');expect(editor.code).toBe('transmissie_type');
    editor.changeName('Café');expect(editor.code).toBe('cafe');
    editor.code='custom_code';editor.changeName('Model');expect(editor.code).toBe('custom_code');
    editor.code='';editor.changeName('Kleur');expect(editor.code).toBe('kleur');
    editor.changeName('123 nummer');expect(editor.code).toBe('kenmerk_123_nummer');expect(editor.valid()).toBe(true);
  });
  it('fills the code when typing a name in the form', async () => {
    const fixture=TestBed.createComponent(DefinitionCreate);fixture.componentRef.setInput('typeId','t');
    await fixture.whenStable();
    const input=fixture.nativeElement.querySelector('input[name=displayName]') as HTMLInputElement;
    input.value='Brandstof';input.dispatchEvent(new Event('input'));await fixture.whenStable();
    expect((fixture.nativeElement.querySelector('input[name=code]') as HTMLInputElement).value).toBe('brandstof');
  });
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
