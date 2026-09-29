import { DefinitionEdit } from './definition-edit';
import { TypeEditState } from './type-edit-state';
import { TypeUsage } from './type-usage';
import { TypeEdit } from './type-edit';
import { DefinitionCreate } from './definition-create';
import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, distinctUntilChanged, switchMap, tap, map } from 'rxjs';
import { readListQuery } from '../list-query';
import { CatalogApi } from '../catalog.api';
import { loadState } from '../load-state';
import { attributeTypeLabel } from '../attribute-types';

@Component({
  providers: [TypeEditState],
  imports: [RouterLink, DefinitionCreate, TypeEdit, TypeUsage, DefinitionEdit],
  template: `
    <a class="back" routerLink="/producttypen" [queryParams]="listQuery()"
      >← Terug naar producttypen</a
    >
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
      <p>
        <a routerLink="/producten" [queryParams]="{ productTypeId: type.id }"
          >Producten van dit type bekijken</a
        >
      </p>
      @if (notice()) {
        <p class="success" role="status">{{ notice() }}</p>
      }
      <app-type-usage
        [typeId]="type.id"
        [typeName]="type.name"
        [listSearch]="listQuery()?.search ?? ''"
      />
      <app-type-edit [type]="type" (saved)="onRenamed()" />
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
              <app-definition-edit
                [typeId]="type.id"
                [definition]="definition"
                (saved)="onChanged($event)"
              />
            </li>
          }
        </ul>
      </section>
    }
  `,
})
export class TypeDetail {
  readonly editState = inject(TypeEditState);
  constructor() {
    effect(() => {
      if (this.editState.busy()) this.notice.set('');
    });
  }
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  readonly listQuery = toSignal(this.route.queryParamMap.pipe(map(readListQuery)));
  private readonly refresh = new BehaviorSubject(0);
  readonly notice = signal('');
  onChanged(message: string) {
    this.notice.set(message);
    this.reload();
  }
  onRenamed() {
    this.notice.set('De producttypenaam is gewijzigd.');
    this.reload();
  }
  onSaved() {
    this.notice.set('Het kenmerk is toegevoegd.');
    this.reload();
  }
  readonly typeLabel = attributeTypeLabel;
  readonly state = toSignal(
    combineLatest([
      this.route.paramMap.pipe(
        distinctUntilChanged((a, b) => a.get('id') === b.get('id')),
        tap(() => {
          this.notice.set('');
          this.editState.busy.set(false);
        }),
      ),
      this.refresh,
    ]).pipe(switchMap(([params]) => loadState(this.api.type(params.get('id')!)))),
  );
  reload() {
    this.refresh.next(this.refresh.value + 1);
  }
}
