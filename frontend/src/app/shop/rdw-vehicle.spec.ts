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
    fixture.componentRef.setInput('attributes', [
      {attributeDefinitionId: 'rdw', name: 'Voertuiggegevens', value: 'AB12CD', dataType: 'Rdw'},
      {attributeDefinitionId: 'transmission', name: 'Transmissietype', value: 'Schakel', dataType: 'Text'},
    ]);
    fixture.detectChanges();
    http.expectOne('/api/shop/products/car/rdw').flush([{name: 'RDW', licensePlate: 'AB12CD',
      fields: [{key: 'merk', name: 'Merk', value: 'OPEL'}]}]);
    fixture.detectChanges();
    const details = fixture.nativeElement.querySelector('.vehicle-details') as HTMLElement;
    expect(fixture.nativeElement.querySelector('details, summary')).toBeNull();
    expect(details.querySelector('h3')).toBeNull();
    expect(Array.from(details.querySelectorAll('dt')).map(node => node.textContent)).toEqual(['Merk', 'Transmissietype']);
    expect(Array.from(details.querySelectorAll('dd')).map(node => node.textContent)).toEqual(['OPEL', 'Schakel']);
    expect(fixture.nativeElement.querySelectorAll('dl').length).toBe(1);
    http.verify();
  });
});
