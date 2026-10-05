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
  it('keeps pending usage reads and confirmations intact during saving', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    const initial = http.expectOne('/api/product-types/t/usage');
    page.reload();
    expect(initial.cancelled).toBe(false);
    initial.flush({ productTypeId: 't', productCount: 0, isInUse: false });
    page.confirming.set(true);
    page.remove();
    const removal = http.expectOne('/api/product-types/t');
    page.reload();
    expect(page.confirming()).toBe(true);
    http.expectNone('/api/product-types/t/usage');
    removal.flush({}, { status: 409, statusText: 'Conflict' });
    const recovery = http.expectOne('/api/product-types/t/usage');
    page.reload();
    expect(recovery.cancelled).toBe(false);
    recovery.flush({ productTypeId: 't', productCount: 1, isInUse: true });
  });
  it.each(['success', 'error'])('ignores late removal %s after a type change', (result) => {
    const fixture = setup();
    const page = fixture.componentInstance;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 0, isInUse: false });
    page.confirming.set(true);
    page.remove();
    const removal = http.expectOne('/api/product-types/t');
    fixture.componentRef.setInput('typeId', 'new');
    fixture.detectChanges();
    TestBed.tick();
    const current = http.expectOne('/api/product-types/new/usage');
    if (result === 'success') removal.flush(null);
    else removal.flush({}, { status: 409, statusText: 'Conflict' });
    expect(navigate).not.toHaveBeenCalled();
    expect(page.error()).toBe('');
    expect(page.busy()).toBe(false);
    expect(current.cancelled).toBe(false);
    current.flush({ productTypeId: 'new', productCount: 0, isInUse: false });
  });
  it('releases its own removal lock when destroyed', () => {
    const fixture = setup();
    http
      .expectOne('/api/product-types/t/usage')
      .flush({ productTypeId: 't', productCount: 0, isInUse: false });
    fixture.componentInstance.confirming.set(true);
    fixture.componentInstance.remove();
    const removal = http.expectOne('/api/product-types/t');
    expect(TestBed.inject(TypeEditState).busy()).toBe(true);
    fixture.destroy();
    expect(removal.cancelled).toBe(true);
    expect(TestBed.inject(TypeEditState).busy()).toBe(false);
  });
  it('preserves another editor lock when destroyed while idle', () => {
    const fixture = setup();
    const initial = http.expectOne('/api/product-types/t/usage');
    TestBed.inject(TypeEditState).busy.set(true);
    fixture.destroy();
    expect(initial.cancelled).toBe(true);
    expect(TestBed.inject(TypeEditState).busy()).toBe(true);
  });
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
    f.componentRef.setInput('listSearch', 'shirt');
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
    expect(navigate).toHaveBeenCalledWith(['/producttypen'], { queryParams: { search: 'shirt' } });
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
