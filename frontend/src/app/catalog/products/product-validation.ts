import { Component, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, combineLatest, switchMap } from 'rxjs';
import { CatalogApi } from '../catalog.api';
import { AttributeIssue, Product, ProductType } from '../catalog.models';
import { loadState } from '../load-state';

@Component({
  selector: 'app-product-validation',
  template: ` <section class="panel">
    <div class="page-head">
      <div>
        <h2>Controle van kenmerken</h2>
        <p class="muted">Ontbrekende of niet-passende productgegevens.</p>
      </div>
      <button class="secondary" [disabled]="state()?.loading" (click)="reload()">
        Opnieuw controleren
      </button>
    </div>
    @if (state()?.loading) {
      <p role="status">Kenmerken controleren…</p>
    }
    @if (state()?.error) {
      <p class="error" role="alert">{{ state()?.error }}</p>
    }
    @if (state()?.data; as validation) {
      @if (validation.isValid) {
        <p class="success" role="status">
          Alle kenmerken passen bij dit producttype. Er ontbreken geen verplichte waarden.
        </p>
      } @else {
        <p class="badge">
          {{ validation.issues.length }}
          {{ validation.issues.length === 1 ? 'aandachtspunt' : 'aandachtspunten' }}
        </p>
        <ul class="issue-list">
          @for (issue of validation.issues; track $index) {
            <li>
              <strong>{{ attributeName(issue) }}</strong
              ><span class="muted"> · {{ scopeName(issue) }}</span>
              <p>{{ reason(issue.code) }}</p>
            </li>
          }
        </ul>
      }
    }
  </section>`,
})
export class ProductValidation {
  readonly product = input.required<Product>();
  readonly type = input.required<ProductType>();
  private readonly api = inject(CatalogApi);
  private readonly refresh = new BehaviorSubject(0);
  readonly state = toSignal(
    combineLatest([toObservable(this.product), this.refresh]).pipe(
      switchMap(([product]) => loadState(this.api.validation(product.id))),
    ),
  );
  reload() {
    this.refresh.next(this.refresh.value + 1);
  }
  attributeName(issue: AttributeIssue) {
    return (
      this.type().attributeDefinitions.find((a) => a.id === issue.attributeDefinitionId)
        ?.displayName ?? 'Verwijderd of onbekend kenmerk'
    );
  }
  scopeName(issue: AttributeIssue) {
    return issue.variantId
      ? (this.product().variants.find((v) => v.id === issue.variantId)?.name ?? 'Onbekende variant')
      : 'Product';
  }
  reason(code: string) {
    return (
      (
        {
          MissingRequired: 'Een verplichte waarde is nog niet ingevuld.',
          UnknownDefinition: 'Dit kenmerk bestaat niet meer in het producttype.',
          WrongScope: 'Deze waarde staat op het verkeerde niveau: product of variant.',
          WrongDataType: 'Deze waarde heeft niet het verwachte gegevenstype.',
        } as Record<string, string>
      )[code] ?? 'Dit kenmerk moet worden nagekeken.'
    );
  }
}
