import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
@Component({
  selector: 'app-copy-text',
  host: { class: 'print-hide' },
  template: `<button
      type="button"
      class="secondary"
      [disabled]="busy() || !text().trim()"
      (click)="copy()"
    >
      {{ busy() ? 'Kopiëren…' : label() }}
    </button>
    @if (message()) {
      <span role="status">{{ message() }}</span>
    }
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    }`,
})
export class CopyText {
  readonly text = input.required<string>();
  readonly label = input('Kopiëren');
  readonly busy = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  private destroyed = false;
  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.destroyed = true;
    });
    effect(() => {
      this.text();
      this.label();
      this.message.set('');
      this.error.set('');
    });
  }
  async copy() {
    if (this.busy() || !this.text().trim()) return;
    const text = this.text(),
      label = this.label();
    this.message.set('');
    this.error.set('');
    if (!navigator.clipboard?.writeText) {
      this.error.set('Kopiëren is niet beschikbaar. Selecteer de tekst en kopieer handmatig.');
      return;
    }
    this.busy.set(true);
    try {
      await navigator.clipboard.writeText(text);
      if (!this.destroyed && this.text() === text && this.label() === label)
        this.message.set('Gekopieerd.');
    } catch {
      if (!this.destroyed && this.text() === text && this.label() === label)
        this.error.set('Kopiëren is niet gelukt. Selecteer de tekst en kopieer handmatig.');
    } finally {
      if (!this.destroyed) this.busy.set(false);
    }
  }
}
