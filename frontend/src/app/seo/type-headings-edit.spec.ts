import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TypeHeadingsEdit } from './type-headings-edit';
describe('Product type section headings', () => {
  let http: HttpTestingController;
  const stored = { productTypeId: 't', aboutHeading: null, attributesHeading: null, revision: 'r' };
  beforeEach(() => { TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }); http = TestBed.inject(HttpTestingController); });
  afterEach(() => http.verify());
  function editor() {
    const fixture = TestBed.createComponent(TypeHeadingsEdit); fixture.componentRef.setInput('typeId', 't'); fixture.detectChanges();
    http.expectNone('/api/product-types/t/section-headings');
    const details = fixture.nativeElement.querySelector('details') as HTMLDetailsElement;
    details.open = true; details.dispatchEvent(new Event('toggle'));
    http.expectOne('/api/product-types/t/section-headings').flush(stored);
    return fixture;
  }
  it('loads only on opening and saves headings with the revision', () => {
    const fixture = editor(); const page = fixture.componentInstance;
    page.aboutHeading = ' Over deze auto '; page.attributesHeading = 'Voertuiggegevens'; page.save(); page.save();
    const request = http.expectOne('/api/product-types/t/section-headings');
    expect(request.request.body).toEqual({ aboutHeading: 'Over deze auto', attributesHeading: 'Voertuiggegevens', revision: 'r' });
    request.flush(null); http.expectOne('/api/product-types/t/section-headings').flush({ ...stored, aboutHeading: 'Over deze auto', attributesHeading: 'Voertuiggegevens', revision: 'new' });
    expect(page.settings()?.revision).toBe('new'); expect(page.aboutHeading).toBe('Over deze auto');
  });
  it('restores defaults with blank values and preserves drafts on failure', () => {
    const page = editor().componentInstance; page.aboutHeading = ' '; page.save();
    const request = http.expectOne('/api/product-types/t/section-headings');
    expect(request.request.body.aboutHeading).toBeNull(); expect(request.request.body.attributesHeading).toBeNull();
    request.flush({}, { status: 409, statusText: 'Conflict' }); expect(page.aboutHeading).toBe(' '); expect(page.error()).toBeTruthy();
  });
  it('ignores late reads from a previous type', () => {
    const fixture = TestBed.createComponent(TypeHeadingsEdit); fixture.componentRef.setInput('typeId', 't'); fixture.detectChanges();
    fixture.componentInstance.load(); const old = http.expectOne('/api/product-types/t/section-headings');
    fixture.componentRef.setInput('typeId', 'other'); fixture.detectChanges(); old.flush(stored);
    expect(fixture.componentInstance.settings()).toBeNull(); expect(fixture.componentInstance.loading()).toBe(false);
  });
});
