import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductPresentationEdit } from './product-presentation';
import { ProductEditState } from './product-edit-state';
import { Product } from '../catalog.models';

const product: Product = {
  id: 'p',
  name: 'Shirt',
  productTypeId: 't',
  revision: 'revision',
  variants: [],
  categoryIds: [],
  attributeValues: [],
  presentation: { description: '', imageUrl: null, imageAlt: '', isPublished: false },
};

describe('Product presentation editor', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductPresentationEdit],
      providers: [provideHttpClient(), provideHttpClientTesting(), ProductEditState],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  async function setup(value = product) {
    const fixture = TestBed.createComponent(ProductPresentationEdit);
    fixture.componentRef.setInput('product', value);
    await fixture.whenStable();
    return fixture;
  }
  it('sends the visible revision and publication details once', async () => {
    const fixture = await setup();
    const editor = fixture.componentInstance;
    const saved = vi.fn();
    editor.saved.subscribe(saved);
    editor.description = ' Description ';
    editor.imageUrl = 'https://example.com/a.jpg';
    editor.imageAlt = ' Image ';
    editor.save(true);
    editor.save(true);
    const request = http.expectOne('/api/products/p/presentation');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      description: 'Description',
      imageUrl: 'https://example.com/a.jpg',
      imageAlt: 'Image',
      isPublished: true,
      revision: 'revision',
    });
    expect(TestBed.inject(ProductEditState).busy()).toBe(true);
    request.flush(null);
    expect(saved).toHaveBeenCalledWith('De productpresentatie is gepubliceerd.');
    expect(editor.busy()).toBe(false);
  });
  it('keeps the draft on a concurrency failure and does not retry the write', async () => {
    const fixture = await setup();
    const editor = fixture.componentInstance;
    editor.description = 'My draft';
    editor.save(false);
    http
      .expectOne('/api/products/p/presentation')
      .flush({}, { status: 409, statusText: 'Conflict' });
    expect(editor.description).toBe('My draft');
    expect(editor.error()).toContain('Vernieuw');
    expect(editor.busy()).toBe(false);
  });
  it.each([
    'http://example.com/a.jpg',
    '/other/image.png',
    '/api/shop/product-images/not-an-id',
    'javascript:alert(1)',
    'https://user:password@example.com/a.jpg',
  ])('rejects unsafe image URL %s', async (imageUrl) => {
    const editor = (await setup()).componentInstance;
    editor.imageUrl = imageUrl;
    editor.imageAlt = 'Image';
    editor.save(false);
    expect(editor.error()).toContain('HTTPS');
    http.expectNone('/api/products/p/presentation');
  });
  it('publishes a product with its uploaded main image', async () => {
    const editor = (await setup()).componentInstance;
    editor.description = 'Blue shirt';
    editor.imageUrl = '/api/shop/product-images/00e3952b-9eac-4f72-b27f-bf895c06af36';
    editor.imageAlt = 'Front of blue shirt';
    editor.save(true);
    const request = http.expectOne('/api/products/p/presentation');
    expect(request.request.body.imageUrl).toBe(editor.imageUrl);
    request.flush(null);
  });
  it('requires complete details to publish, while allowing an empty draft', async () => {
    const editor = (await setup()).componentInstance;
    editor.save(true);
    http.expectNone('/api/products/p/presentation');
    expect(editor.error()).toContain('Publiceren vereist');
    editor.save(false);
    const request = http.expectOne('/api/products/p/presentation');
    expect(request.request.body.isPublished).toBe(false);
    expect(request.request.body.imageUrl).toBeNull();
    request.flush(null);
  });
  it('renders description as text and explains failed images', async () => {
    const fixture = await setup({
      ...product,
      presentation: {
        description: '<script>alert(1)</script>',
        imageUrl: 'https://example.com/a.jpg',
        imageAlt: 'A shirt',
        isPublished: true,
      },
    });
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.description')?.textContent).toBe('<script>alert(1)</script>');
    expect(element.querySelector('script')).toBeNull();
    expect(element.querySelector('img')?.alt).toBe('A shirt');
    element.querySelector('img')!.dispatchEvent(new Event('error'));
    fixture.detectChanges();
    expect(element.textContent).toContain('De afbeelding kon niet worden geladen');
  });
  it('respects the shared product write lock', async () => {
    const editor = (await setup()).componentInstance;
    TestBed.inject(ProductEditState).busy.set(true);
    editor.description = 'Draft';
    editor.save(false);
    http.expectNone('/api/products/p/presentation');
  });
});
