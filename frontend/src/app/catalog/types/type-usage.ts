import { Router } from '@angular/router';
import { TypeEditState } from './type-edit-state';
import { errorMessage } from '../error-message';
import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { loadState } from '../load-state';
@Component({
  selector: 'app-type-usage',
  template: ` <section class="panel">
    <h2>Gebruik van dit producttype</h2>
    @if (state()?.loading) {
      <p role="status">Gebruik ophalen…</p>
    }
    @if (state()?.error) {
      <p class="error" role="alert">{{ state()?.error }}</p>
    }
    @if (state()?.data; as usage) {
      <p>
        {{ usage.productCount }}
        {{ usage.productCount === 1 ? 'product gebruikt' : 'producten gebruiken' }} dit producttype.
      </p>
      <p class="muted">
        Wijzigingen aan kenmerken gelden voor alle producten van dit type. Bestaande waarden blijven
        behouden.
      </p>
    }
    <button class="secondary" [disabled]="state()?.loading || busy()" (click)="reload()">
      Gebruik opnieuw ophalen
    </button>
    @if (state()?.data?.isInUse) {
      <p class="muted">Een producttype dat in gebruik is, kan niet worden verwijderd.</p>
    }
    @if (canRemove()) {
      <div class="remove-section">
        @if (confirming()) {
          <p>
            Producttype “{{ typeName() }}” en alle bijbehorende kenmerkdefinities definitief
            verwijderen?
          </p>
          <button type="button" [disabled]="busy()" (click)="remove()">
            Ja, producttype verwijderen
          </button>
          <button
            type="button"
            class="secondary"
            [disabled]="busy()"
            (click)="confirming.set(false)"
          >
            Annuleren
          </button>
        } @else {
          <button
            type="button"
            class="secondary"
            [disabled]="busy()"
            (click)="confirming.set(true)"
          >
            Producttype verwijderen
          </button>
        }
      </div>
    }
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }
  </section>`,
})
export class TypeUsage {
  readonly typeId = input.required<string>();
  readonly typeName = input('');
  readonly listSearch = input('');
  readonly busy = inject(TypeEditState).busy;
  readonly confirming = signal(false);
  readonly error = signal('');
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private writing = false;
  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.writing) this.busy.set(false);
    });
    effect(() => {
      this.typeId();
      this.confirming.set(false);
      this.error.set('');
    });
  }
  private readonly api = inject(CatalogApi);
  private readonly refresh = new BehaviorSubject(0);
  readonly state = toSignal(
    combineLatest([toObservable(this.typeId), this.refresh]).pipe(
      switchMap(([id]) => loadState(this.api.typeUsage(id))),
    ),
  );
  canRemove() {
    const state = this.state();
    return (
      !state?.loading &&
      !state?.error &&
      state?.data?.productTypeId === this.typeId() &&
      state.data.productCount === 0 &&
      state.data.isInUse === false
    );
  }
  remove() {
    if (this.busy() || !this.confirming() || !this.canRemove()) return;
    const typeId = this.typeId();
    const listSearch = this.listSearch();
    this.writing = true;
    this.busy.set(true);
    this.error.set('');
    this.api
      .deleteType(this.typeId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.writing = false;
          this.busy.set(false);
          if (this.typeId() !== typeId) return;
          void this.router.navigate(['/producttypen'], {
            queryParams: { search: listSearch || null },
          });
        },
        error: (error) => {
          this.writing = false;
          this.busy.set(false);
          if (this.typeId() !== typeId) return;
          this.error.set(
            error.status === 409
              ? 'Dit producttype is inmiddels in gebruik en kan niet worden verwijderd.'
              : errorMessage(error),
          );
          this.reload();
        },
      });
  }
  reload() {
    if (this.busy() || this.state()?.loading) return;
    this.confirming.set(false);
    this.refresh.next(this.refresh.value + 1);
  }
}
