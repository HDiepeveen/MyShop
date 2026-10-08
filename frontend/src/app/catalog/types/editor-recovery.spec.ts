import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { TypeEdit } from './type-edit';
import { DefinitionCreate } from './definition-create';
import { DefinitionEdit } from './definition-edit';
import { TypeEditState } from './type-edit-state';
import { ProductCreate } from '../products/product-create';

const definition = { id: 'attribute-1', code: 'color', displayName: 'Kleur', dataType: 'Text', scope: 'Product', isRequired: false, isFilterable: false };
const type = { id: 'type-1', name: 'Kleding', attributeDefinitions: [definition] };

describe('Catalog editor recovery', () => {
  function setup(kind: 'type' | 'create' | 'definition') {
    const response = new Subject<void>();
    const api = { renameType: vi.fn(() => response), addDefinition: vi.fn(() => response), renameDefinition: vi.fn(() => response) };
    TestBed.configureTestingModule({
      imports: [TypeEdit, DefinitionCreate, DefinitionEdit],
      providers: [TypeEditState, { provide: CatalogApi, useValue: api }],
    });
    const fixture = kind === 'type' ? TestBed.createComponent(TypeEdit)
      : kind === 'create' ? TestBed.createComponent(DefinitionCreate) : TestBed.createComponent(DefinitionEdit);
    if (kind === 'type') fixture.componentRef.setInput('type', type);
    else {
      fixture.componentRef.setInput('typeId', type.id);
      if (kind === 'definition') fixture.componentRef.setInput('definition', definition);
    }
    fixture.detectChanges();
    const page = fixture.componentInstance;
    function write() {
      if (page instanceof TypeEdit) { page.name = 'Nieuw'; page.rename(); }
      else if (page instanceof DefinitionCreate) { page.displayName = 'Materiaal'; page.code = 'material'; page.create(); }
      else { page.displayName = 'Nieuwe kleur'; page.rename(); }
    }
    function changeInput() {
      if (kind === 'type') fixture.componentRef.setInput('type', { ...type, id: 'type-2', name: 'Schoenen' });
      else {
        fixture.componentRef.setInput('typeId', 'type-2');
        if (kind === 'definition') fixture.componentRef.setInput('definition', { ...definition, id: 'attribute-2', displayName: 'Maat' });
      }
      fixture.detectChanges();
    }
    return { fixture, page, response, write, changeInput };
  }

  for (const kind of ['type', 'create', 'definition'] as const) {
    it(`clears previous ${kind} editor errors and draft when inputs change`, () => {
      const { page, changeInput } = setup(kind);
      page.error.set('Old error');
      if (page instanceof DefinitionEdit) page.confirming.set(true);
      if (page instanceof DefinitionCreate) { page.code = 'draft'; page.displayName = 'Draft'; }
      changeInput();
      expect(page.error()).toBe('');
      if (page instanceof TypeEdit) expect(page.name).toBe('Schoenen');
      if (page instanceof DefinitionEdit) {
        expect(page.displayName).toBe('Maat');
        expect(page.confirming()).toBe(false);
      }
      if (page instanceof DefinitionCreate) {
        expect(page.displayName).toBe('');
        expect(page.code).toBe('');
      }
    });

    it.each(['success', 'error'])(`ignores a late ${kind} write %s after inputs change`, (result) => {
      const { page, response, write, changeInput } = setup(kind);
      const saved = vi.fn();
      page.saved.subscribe(saved);
      write();
      expect(page.busy()).toBe(true);
      changeInput();
      if (result === 'success') response.next();
      else response.error(new Error('Offline'));
      expect(saved).not.toHaveBeenCalled();
      expect(page.error()).toBe('');
      expect(page.busy()).toBe(false);
    });

    it(`releases the shared lock when the active ${kind} editor is destroyed`, () => {
      const { fixture, page, response, write } = setup(kind);
      write();
      expect(page.busy()).toBe(true);
      fixture.destroy();
      expect(response.observed).toBe(false);
      expect(TestBed.inject(TypeEditState).busy()).toBe(false);
    });

    it(`does not release another editor's lock when an idle ${kind} editor is destroyed`, () => {
      const { fixture } = setup(kind);
      TestBed.inject(TypeEditState).busy.set(true);
      fixture.destroy();
      expect(TestBed.inject(TypeEditState).busy()).toBe(true);
    });
  }

  it('keeps product type search and paging unchanged during product creation', () => {
    const creation = new Subject<{ id: string }>();
    const api = { types: vi.fn(() => of([])), createProduct: vi.fn(() => creation) };
    TestBed.configureTestingModule({
      imports: [ProductCreate], providers: [provideRouter([]), { provide: CatalogApi, useValue: api }],
    });
    const fixture = TestBed.createComponent(ProductCreate);
    const page = fixture.componentInstance;
    page.selected.set({ id: type.id, name: type.name, attributeDefinitionCount: 1 });
    page.name = 'Shirt';
    page.create();
    page.searchText = 'Schoenen';
    page.search(); page.changePage(20); page.retry();
    expect(api.types).toHaveBeenCalledTimes(1);
    expect(page.offset()).toBe(0);
    creation.error(new Error('Offline'));
    page.search();
    expect(api.types).toHaveBeenLastCalledWith(0, 'Schoenen');
  });
});
