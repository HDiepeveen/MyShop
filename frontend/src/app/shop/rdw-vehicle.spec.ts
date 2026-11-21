import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RdwVehicle } from './rdw-vehicle';

describe('Vehicle details', () => {
  it('renders RDW fields permanently without a disclosure control', () => {
    TestBed.configureTestingModule({ imports: [RdwVehicle], providers: [provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(RdwVehicle);
    fixture.componentRef.setInput('productId', 'car');
    fixture.detectChanges();
    http.expectOne('/api/shop/products/car/rdw').flush([{name: 'RDW', licensePlate: 'AB12CD',
      fields: [{key: 'merk', name: 'Merk', value: 'OPEL'}]}]);
    fixture.detectChanges();
    const details = fixture.nativeElement.querySelector('.vehicle-details') as HTMLElement;
    expect(fixture.nativeElement.querySelector('details, summary')).toBeNull();
    expect(details.querySelector('h3')).toBeNull();
    expect(details.querySelector('dd')!.textContent).toBe('OPEL');
    http.verify();
  });
});
