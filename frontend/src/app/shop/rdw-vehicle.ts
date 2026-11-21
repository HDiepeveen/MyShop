import { Component, computed, inject, input } from '@angular/core';
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
    @if (fields().length || manualAttributes().length) {
      <section class="vehicle-details">
        <dl class="detail-list">
          @for (field of fields(); track $index) {
            <dt>{{ field.name }}</dt>
            <dd>{{ field.value }}</dd>
          }
          @for (attribute of manualAttributes(); track attribute.attributeDefinitionId) {
            <dt>{{ attribute.name }}</dt>
            <dd>{{ attribute.value }}</dd>
          }
        </dl>
      </section>
    }
  `,
  styles: `section { margin-top: 28px; } dd { overflow-wrap: anywhere; }`,
})
export class RdwVehicle {
  readonly productId = input.required<string>();
  readonly attributes = input<{ attributeDefinitionId: string; name: string; value: string; dataType?: string }[]>([]);
  readonly manualAttributes = computed(() => this.attributes().filter(attribute => attribute.dataType !== 'Rdw'));
  private readonly http = inject(HttpClient);
  readonly vehicles = toSignal(toObservable(this.productId).pipe(
    switchMap(id => this.http.get<Vehicle[]>('/api/shop/products/' + id + '/rdw')
      .pipe(catchError(() => of([] as Vehicle[])))),
  ), { initialValue: [] });
  readonly fields = computed(() => this.vehicles().flatMap(vehicle => vehicle.fields));
}
