import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductImagesEdit } from './product-images';
import { ProductEditState } from './product-edit-state';
import { Product } from '../catalog.models';

const product: Product = { id: 'p', name: 'Shirt', productTypeId: 't', revision: 'r', variants: [], categoryIds: [], attributeValues: [] };
const photo = { id: 'image', url: '/api/shop/product-images/image', alternativeText: 'Blue shirt', fileName: 'shirt.png' };

describe('Product images', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ProductImagesEdit], providers: [provideHttpClient(), provideHttpClientTesting(), ProductEditState] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  async function setup() {
    const fixture = TestBed.createComponent(ProductImagesEdit);
    fixture.componentRef.setInput('product', product); await fixture.whenStable();
    http.expectNone('/api/products/p/images');
    fixture.componentInstance.opened.set(true); await fixture.whenStable();
    http.expectOne('/api/products/p/images').flush({ revision: 'gallery-revision', mainImageUrl: photo.url, images: [photo] });
    await fixture.whenStable(); return fixture;
  }
  it('uploads multiple files and metadata without allowing duplicate submissions', async () => {
    const fixture = await setup(), editor = fixture.componentInstance;
    const input = { files: [new File(['png'], 'front.png', { type: 'image/png' }), new File(['jpeg'], 'back.jpg', { type: 'image/jpeg' })], value: 'selected' } as unknown as HTMLInputElement;
    editor.choose(input); editor.alternativeText = ' Blue shirt ';
    const saved = vi.fn(); editor.saved.subscribe(saved);
    editor.upload(input); editor.upload(input);
    const request = http.expectOne('/api/products/p/images');
    expect(request.request.method).toBe('POST');
    const body = request.request.body as FormData;
    expect(body.getAll('files')).toHaveLength(2);
    expect(body.get('revision')).toBe('gallery-revision');
    expect(body.get('alternativeText')).toBe('Blue shirt');
    request.flush(null); expect(input.value).toBe(''); expect(saved).toHaveBeenCalledOnce();
  });
  it('rejects oversized or unsupported files before sending and shows server validation errors', async () => {
    const fixture = await setup(), editor = fixture.componentInstance;
    for (const file of [new File(['svg'], 'image.svg', { type: 'image/svg+xml' }), new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'large.png', { type: 'image/png' })]) {
      editor.choose({ files: [file], value: '' } as unknown as HTMLInputElement);
      expect(editor.files()).toHaveLength(0); expect(editor.error()).toContain('maximaal 5 MB');
    }
    editor.files.set([new File(['png'], 'photo.png', { type: 'image/png' })]);
    editor.upload({ value: '' } as HTMLInputElement);
    http.expectOne('/api/products/p/images').flush({ message: 'Afbeelding ongeldig.' }, { status: 400, statusText: 'Invalid' });
    expect(editor.error()).toBe('Afbeelding ongeldig.'); expect(editor.busy()).toBe(false);
  });
  it('selects a main image and requires explicit deletion confirmation', async () => {
    const fixture = await setup(), editor = fixture.componentInstance;
    editor.main(photo);
    const main = http.expectOne('/api/products/p/images/image/main');
    expect(main.request.body).toEqual({ revision: 'gallery-revision' }); main.flush(null);
    editor.remove(photo); http.expectNone('/api/products/p/images/image');
    editor.removing.set(photo.id); editor.remove(photo);
    const deletion = http.expectOne('/api/products/p/images/image');
    expect(deletion.request.method).toBe('DELETE'); expect(deletion.request.body).toEqual({ revision: 'gallery-revision' }); deletion.flush(null);
  });
  it('cancels stale gallery reads when changing product', async () => {
    const fixture = await setup(), editor = fixture.componentInstance;
    editor.load(); const old = http.expectOne('/api/products/p/images');
    fixture.componentRef.setInput('product', { ...product, id: 'second' }); await fixture.whenStable();
    expect(old.cancelled).toBe(true);
    http.expectOne('/api/products/second/images').flush({ revision: 'new', mainImageUrl: null, images: [] });
    expect(editor.gallery()?.images).toEqual([]);
  });
});
