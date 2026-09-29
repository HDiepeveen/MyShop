import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AttributeEdit } from './attribute-edit';
import { ProductEditState } from './product-edit-state';
describe('AttributeEdit', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AttributeEdit],
      providers: [provideHttpClient(), provideHttpClientTesting(), ProductEditState],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup(dataType = 'Text') {
    const fixture = TestBed.createComponent(AttributeEdit);
    fixture.componentRef.setInput('productId', 'p');
    fixture.componentRef.setInput('definition', {
      id: 'a',
      displayName: 'Materiaal',
      dataType,
      scope: 'Product',
      isRequired: true,
    });
    fixture.detectChanges();
    return fixture;
  }
  it('validates empty text and persists a value only once with the proper type', () => {
    const editor = setup().componentInstance;
    editor.save();
    expect(editor.validationError()).toBeTruthy();
    http.expectNone('/api/products/p/attributes/a');
    editor.text = 'Linnen';
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.save();
    editor.save();
    const request = http.expectOne('/api/products/p/attributes/a');
    expect(JSON.parse(request.request.body)).toEqual({ dataType: 0, value: 'Linnen' });
    expect(saved).not.toHaveBeenCalled();
    request.flush(null);
    expect(saved).toHaveBeenCalledOnce();
  });

  it('prefills false and saves it as a boolean, not empty text', () => {
    const fixture = setup('Boolean');
    fixture.componentRef.setInput('current', {
      attributeDefinitionId: 'a',
      dataType: 'Boolean',
      value: false,
    });
    fixture.detectChanges();
    const editor = fixture.componentInstance;
    expect(editor.text).toBe('false');
    editor.save();
    const request = http.expectOne('/api/products/p/attributes/a');
    expect(JSON.parse(request.request.body)).toEqual({ dataType: 3, value: false });
    request.flush(null);
  });

  it('edits multiple choices without changing their order or splitting multiline values', () => {
    const fixture = setup('MultiChoice');
    fixture.componentRef.setInput('current', {
      attributeDefinitionId: 'a',
      dataType: 'MultiChoice',
      value: ['First', 'line\nline'],
    });
    fixture.detectChanges();
    const editor = fixture.componentInstance;
    editor.addChoice();
    editor.choices[2] = 'Last';
    editor.save();
    const request = http.expectOne('/api/products/p/attributes/a');
    expect(JSON.parse(request.request.body).value).toEqual(['First', 'line\nline', 'Last']);
    request.flush(null);
  });

  it('writes variant values to their own scope and blocks mismatched definitions', () => {
    const fixture = setup();
    fixture.componentRef.setInput('variantId', 'v');
    fixture.detectChanges();
    const editor = fixture.componentInstance;
    editor.text = 'Blue';
    editor.save();
    http.expectNone('/api/products/p/variants/v/attributes/a');
    expect(editor.validationError()).toContain('ander niveau');
    fixture.componentRef.setInput('definition', {
      id: 'a',
      displayName: 'Kleur',
      dataType: 'Text',
      scope: 'Variant',
    });
    fixture.detectChanges();
    editor.text = 'Blue';
    editor.save();
    const request = http.expectOne('/api/products/p/variants/v/attributes/a');
    expect(JSON.parse(request.request.body)).toEqual({ dataType: 0, value: 'Blue' });
    request.flush(null);
    http.expectNone('/api/products/p/attributes/a');
  });

  it('clears a required false value only after server success and blocks duplicate writes', () => {
    const fixture = setup('Boolean');
    const editor = fixture.componentInstance;
    editor.clear();
    http.expectNone('/api/products/p/attributes/a');
    fixture.componentRef.setInput('current', {
      attributeDefinitionId: 'a',
      dataType: 'Boolean',
      value: false,
    });
    fixture.detectChanges();
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.clear();
    editor.clear();
    const request = http.expectOne('/api/products/p/attributes/a');
    expect(request.request.method).toBe('DELETE');
    request.flush({}, { status: 409, statusText: 'Conflict' });
    expect(saved).not.toHaveBeenCalled();
    expect(editor.current()?.value).toBe(false);
    editor.clear();
    http.expectOne('/api/products/p/attributes/a').flush(null);
    expect(saved).toHaveBeenCalledExactlyOnceWith('De kenmerkwaarde is gewist.');
  });

  it('preserves multiline text in the actual form control', async () => {
    const fixture = setup();
    fixture.componentRef.setInput('current', {
      attributeDefinitionId: 'a',
      dataType: 'Text',
      value: 'Line one\nLine two',
    });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('textarea').value).toBe('Line one\nLine two');
  });
  it('keeps draft text after a conflict and releases the shared write lock', () => {
    const editor = setup().componentInstance;
    editor.text = 'Linnen';
    editor.save();
    http
      .expectOne('/api/products/p/attributes/a')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(editor.text).toBe('Linnen');
    expect(editor.error()).toBeTruthy();
    expect(TestBed.inject(ProductEditState).busy()).toBe(false);
  });
});
