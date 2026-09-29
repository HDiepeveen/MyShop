import { Router, provideRouter } from '@angular/router';
import { TypeEditState } from './type-edit-state';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TypeUsage } from './type-usage';
describe('TypeUsage', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TypeUsage],
      providers: [
        TypeEditState,
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  function setup() {
    const f = TestBed.createComponent(TypeUsage);
    f.componentRef.setInput('typeId', 't');
    f.detectChanges();
    TestBed.tick();
    return f;
  }
  it('shows usage and retries a read failure without implying zero usage', () => {
    const f = setup();
    http.expectOne('/api/product-types/t/usage').flush({}, { status: 500, statusText: 'Failure' });
    f.detectChanges();
    expect(f.componentInstance.state()?.data).toBeNull();
    expect(f.nativeElement.textContent).not.toContain('0 producten');
    f.componentInstance.reload();
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 3, isInUse: true });
    f.detectChanges();
    expect(f.nativeElement.textContent).toContain('3 producten gebruiken');
  });

  it('deletes only a confirmed unused type and navigates after success', () => {
    const f = setup(),
      c = f.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    c.confirming.set(true);
    c.remove();
    http.expectNone((r) => r.method === 'DELETE');
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 0, isInUse: false });
    c.confirming.set(false);
    c.remove();
    http.expectNone((r) => r.method === 'DELETE');
    c.confirming.set(true);
    c.remove();
    c.remove();
    const req = http.expectOne('/api/product-types/t');
    expect(req.request.method).toBe('DELETE');
    expect(navigate).not.toHaveBeenCalled();
    req.flush(null);
    expect(navigate).toHaveBeenCalledWith(['/producttypen']);
  });
  it('blocks used types and refreshes usage after a concurrent-use conflict', () => {
    const c = setup().componentInstance;
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 1, isInUse: true });
    c.confirming.set(true);
    c.remove();
    http.expectNone((r) => r.method === 'DELETE');
    c.reload();
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 0, isInUse: false });
    c.confirming.set(true);
    c.remove();
    http.expectOne('/api/product-types/t').flush({}, { status: 409, statusText: 'Conflict' });
    expect(c.error()).toContain('inmiddels in gebruik');
    expect(c.canRemove()).toBe(false);
    expect(c.confirming()).toBe(false);
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 1, isInUse: true });
    expect(c.canRemove()).toBe(false);
  });
  it('never treats a failed usage read as permission to delete', () => {
    const c = setup().componentInstance;
    http.expectOne('/api/product-types/t/usage').flush({}, { status: 500, statusText: 'Error' });
    c.confirming.set(true);
    c.remove();
    expect(c.canRemove()).toBe(false);
    http.expectNone((r) => r.method === 'DELETE');
  });
  it('cancels an old read when the type changes', () => {
    const f = setup();
    const old = http.expectOne('/api/product-types/t/usage');
    f.componentRef.setInput('typeId', 'new');
    f.detectChanges();
    TestBed.tick();
    expect(old.cancelled).toBe(true);
    http
      .expectOne('/api/product-types/new/usage')
      .flush({ productTypeId: 'new', productCount: 0, isInUse: false });
    expect(f.componentInstance.state()?.data?.productCount).toBe(0);
  });
});
