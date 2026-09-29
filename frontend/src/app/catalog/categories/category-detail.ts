import { CategoryEdit } from './category-edit';
import { CategoryManagement } from './category-management';
import { Component, effect, inject, signal } from '@angular/core';
import { CategoryEditState } from './category-edit-state';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, of, tap, distinctUntilChanged, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { loadState } from '../load-state';

@Component({
  providers: [CategoryEditState],
  imports: [RouterLink, CategoryEdit, CategoryManagement],
  template: `
    <a class="back" routerLink="/categorieen">← Alle categorieën</a>
    @if (state()?.loading) {
      <p class="loading" role="status">Categorie ophalen…</p>
    }
    @if (state()?.error) {
      <p class="error" role="alert">
        {{ state()?.error }} <button class="secondary" (click)="reload()">Opnieuw proberen</button>
      </p>
    }
    @if (notice()) {
      <p class="success" role="status">{{ notice() }}</p>
    }
    @if (editState.busy()) {
      <p role="status">Wijziging opslaan…</p>
    }
    @if (state()?.data; as category) {
      <div class="eyebrow">{{ category.isRoot ? 'Hoofdcategorie' : 'Subcategorie' }}</div>
      <h1>{{ category.name }}</h1>
      <app-category-edit [category]="category" (saved)="onSaved()" />
      <app-category-management
        [category]="category"
        (saved)="onManaged($event)"
        (removed)="onRemoved()"
      />
      <section class="panel">
        <h2>Plek in het assortiment</h2>
        @if (category.parentCategoryId) {
          @if (parentState()?.data; as parent) {
            <a [routerLink]="['/categorieen', category.parentCategoryId]"
              >Bovenliggende categorie: {{ parent.name }}</a
            >
          } @else {
            <a [routerLink]="['/categorieen', category.parentCategoryId]"
              >Bekijk bovenliggende categorie</a
            >
          }
          @if (parentState()?.error) {
            <p class="error" role="alert">
              De naam van de bovenliggende categorie kon niet worden opgehaald.
            </p>
            <button
              type="button"
              class="secondary"
              (click)="retryParent()"
              [disabled]="editState.busy()"
            >
              Naam opnieuw ophalen
            </button>
          }
        } @else {
          <p class="muted">Deze categorie staat op het hoogste niveau.</p>
        }
      </section>
    }
  `,
})
export class CategoryDetail {
  readonly editState = inject(CategoryEditState);
  constructor() {
    effect(() => {
      if (this.editState.busy()) this.notice.set('');
    });
  }
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly refresh = new BehaviorSubject(0);
  readonly notice = signal('');
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
    ]).pipe(switchMap(([params]) => loadState(this.api.category(params.get('id')!)))),
  );
  private readonly parentRefresh = new BehaviorSubject(0);
  readonly parentState = toSignal(
    combineLatest([toObservable(this.state), this.parentRefresh]).pipe(
      switchMap(([state]) => {
        const parentId = state?.data?.parentCategoryId;
        return parentId ? loadState(this.api.category(parentId)) : of(null);
      }),
    ),
  );
  retryParent() {
    if (!this.editState.busy()) this.parentRefresh.next(this.parentRefresh.value + 1);
  }
  onSaved() {
    this.notice.set('De categorienaam is bijgewerkt.');
    this.reload();
  }
  onManaged(message: string) {
    this.notice.set(message);
    this.reload();
  }
  onRemoved() {
    void this.router.navigate(['/categorieen']);
  }
  reload() {
    this.refresh.next(this.refresh.value + 1);
  }
}
