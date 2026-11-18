import { Component, inject, input } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap } from 'rxjs';

interface Vehicle {
  name: string;
  licensePlate: string;
  fields: { key: string; name: string; value: string }[];
}
@Component({
  selector: 'app-rdw-vehicle',
  template: `
    @for (vehicle of vehicles(); track vehicle.name + vehicle.licensePlate) {
      @if (vehicle.fields.length) {
        <details>
          <summary>Voertuiggegevens · {{ vehicle.licensePlate }}</summary>
          <dl class="detail-list">
            @for (field of vehicle.fields; track field.key) {
              <dt>{{ field.name }}</dt>
              <dd>{{ field.value }}</dd>
            }
          </dl>
          <p class="muted">Bron RDW Open Data</p>
        </details>
      }
    }
  `,
  styles: `details { margin-top: 28px; } summary { cursor: pointer; font-weight: 600; } dd { overflow-wrap: anywhere; }`,
})
export class RdwVehicle {
  readonly productId = input.required<string>();
  private readonly http = inject(HttpClient);
  readonly vehicles = toSignal(toObservable(this.productId).pipe(
    switchMap(id => this.http.get<Vehicle[]>('/api/shop/products/' + id + '/rdw')
      .pipe(catchError(() => of([] as Vehicle[])))),
  ), { initialValue: [] });
}
