import { DefinitionCreate } from './definition-create';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, switchMap, tap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { loadState } from '../load-state';
import { attributeTypeLabel } from '../attribute-types';

@Component({
  imports: [RouterLink, DefinitionCreate],
  template: `
    <a class="back" routerLink="/producttypen">← Alle producttypen</a>
    @if (state()?.loading) {
      <p role="status">Producttype ophalen…</p>
    }
    @if (state()?.error) {
      <p class="error" role="alert">
        {{ state()?.error }} <button class="secondary" (click)="reload()">Opnieuw proberen</button>
      </p>
    }
    @if (state()?.data; as type) {
      <div class="eyebrow">Producttype</div>
      <h1>{{ type.name }}</h1>
      @if (notice()) {
        <p class="success" role="status">{{ notice() }}</p>
      }
      <app-definition-create [typeId]="type.id" (started)="notice.set('')" (saved)="onSaved()" />
      <section class="panel">
        <h2>Kenmerken</h2>
        @if (!type.attributeDefinitions.length) {
          <p class="muted">Dit producttype heeft nog geen kenmerken.</p>
        }
        <ul class="issue-list">
          @for (definition of type.attributeDefinitions; track definition.id) {
            <li>
              <strong>{{ definition.displayName }}</strong>
              <span class="badge">{{
                definition.scope === 'Product' ? 'Product' : 'Variant'
              }}</span>
              <p>
                {{ typeLabel(definition.dataType) }} ·
                {{ definition.isRequired ? 'Verplicht' : 'Optioneel' }} ·
                {{ definition.isFilterable ? 'Filterbaar' : 'Niet filterbaar' }}
              </p>
              <small>Code: {{ definition.code }}</small>
            </li>
          }
        </ul>
      </section>
    }
  `,
})
export class TypeDetail {
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  private readonly refresh = new BehaviorSubject(0);
  readonly notice = signal('');
  onSaved() {
    this.notice.set('Het kenmerk is toegevoegd.');
    this.reload();
  }
  readonly typeLabel = attributeTypeLabel;
  readonly state = toSignal(
    combineLatest([this.route.paramMap.pipe(tap(() => this.notice.set(''))), this.refresh]).pipe(
      switchMap(([params]) => loadState(this.api.type(params.get('id')!))),
    ),
  );
  reload() {
    this.refresh.next(this.refresh.value + 1);
  }
}
