import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, input, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subscription } from 'rxjs';
import { Product } from '../catalog.models';
import { errorMessage } from '../error-message';
import { ProductEditState } from './product-edit-state';

export interface ProductImage { id: string; url: string; alternativeText: string; fileName: string }
interface Gallery { revision: string; mainImageUrl: string | null; images: ProductImage[] }

@Component({
  selector: 'app-product-images',
  imports: [FormsModule],
  styles: ['.photos { display:flex; flex-wrap:wrap; gap:16px; } .photo { width:200px; } .photo img { width:100%; height:150px; object-fit:contain; }'],
  template: `<section class="panel">
    <h2>Productafbeeldingen</h2>
    @if (!opened()) {
      <button class="secondary" [disabled]="busy()" (click)="opened.set(true)">Afbeeldingen beheren</button>
    } @else {
      <p>Upload JPEG- of PNG-foto’s: maximaal 5 MB per foto, 20 miljoen pixels en 10 foto’s per product.</p>
      @if (loading()) { <p role="status">Afbeeldingen ophalen…</p> }
      @if (error()) { <p class="error" role="alert">{{ error() }}</p>
        <button class="secondary" [disabled]="busy() || loading()" (click)="load()">Opnieuw ophalen</button> }
      @if (gallery(); as photos) {
        <form (ngSubmit)="upload(fileInput)">
          <label class="field">Foto’s kiezen<input #fileInput type="file" accept="image/jpeg,image/png" multiple
            [disabled]="busy() || loading()" (change)="choose(fileInput)" /></label>
          <p>{{ files().length }} foto’s geselecteerd</p>
          <label class="field">Alternatieve tekst voor deze foto’s<input name="photoAlt" [(ngModel)]="alternativeText"
            maxlength="250" required [disabled]="busy() || loading()" /></label>
          <p class="muted">Beschrijf wat de foto’s tonen. De eerste upload wordt de hoofdafbeelding als er nog geen is.</p>
          <button [disabled]="busy() || loading() || !files().length || !alternativeText.trim()">Foto’s uploaden</button>
        </form>
        <div class="photos">
          @for (photo of photos.images; track photo.id) {
            <article class="photo">
              <img [src]="photo.url" [alt]="photo.alternativeText" loading="lazy" />
              <p>{{ photo.fileName }}</p>
              @if (photo.url === photos.mainImageUrl) { <p class="badge">Hoofdafbeelding</p> }
              @else { <button class="secondary" [disabled]="busy() || loading()" (click)="main(photo)">Als hoofdafbeelding</button> }
              @if (removing() === photo.id) {
                <p>Deze foto verwijderen? Bij de laatste hoofdafbeelding wordt het product weer concept.</p>
                <button class="danger" [disabled]="busy()" (click)="remove(photo)">Ja, verwijderen</button>
                <button class="secondary" [disabled]="busy()" (click)="removing.set(null)">Annuleren</button>
              } @else { <button class="secondary" [disabled]="busy() || loading()" (click)="removing.set(photo.id)">Verwijderen</button> }
            </article>
          } @empty { <p>Nog geen foto’s geüpload.</p> }
        </div>
      }
    }
  </section>`,
})
export class ProductImagesEdit {
  readonly product = input.required<Product>();
  readonly saved = output<string>();
  readonly busy = inject(ProductEditState).busy;
  readonly opened = signal(false);
  readonly gallery = signal<Gallery | null>(null);
  readonly files = signal<File[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly removing = signal<string | null>(null);
  alternativeText = '';
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private read?: Subscription;
  private writing = false;
  constructor() {
    effect(() => {
      const product = this.product(), opened = this.opened();
      untracked(() => {
        this.alternativeText = product.name;
        this.files.set([]); this.removing.set(null); this.gallery.set(null);
        if (opened) this.load();
      });
    });
    this.destroyRef.onDestroy(() => { this.read?.unsubscribe(); if (this.writing) this.busy.set(false); });
  }
  private url() { return '/api/products/' + encodeURIComponent(this.product().id) + '/images'; }
  load() {
    this.read?.unsubscribe(); this.loading.set(true); this.error.set('');
    this.read = this.http.get<Gallery>(this.url()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: gallery => { this.gallery.set(gallery); this.loading.set(false); },
      error: error => { this.error.set(errorMessage(error)); this.loading.set(false); },
    });
  }
  choose(input: HTMLInputElement) {
    const files = Array.from(input.files ?? []);
    this.error.set('');
    if (files.length + (this.gallery()?.images.length ?? 0) > 10 || files.some(file =>
      file.size === 0 || file.size > 5 * 1024 * 1024 || !['image/jpeg', 'image/png'].includes(file.type))) {
      this.files.set([]); input.value = '';
      this.error.set('Kies JPEG of PNG van maximaal 5 MB, maximaal 10 foto’s per product.'); return;
    }
    this.files.set(files);
  }
  upload(input: HTMLInputElement) {
    if (this.busy() || this.loading() || !this.files().length || !this.alternativeText.trim() || this.alternativeText.trim().length > 250) return;
    const form = new FormData();
    form.set('revision', this.gallery()!.revision); form.set('alternativeText', this.alternativeText.trim());
    for (const file of this.files()) form.append('files', file, file.name);
    this.change(this.http.post<void>(this.url(), form), 'De productfoto’s zijn opgeslagen.', () => { input.value = ''; this.files.set([]); });
  }
  main(photo: ProductImage) {
    if (this.busy() || this.loading()) return;
    this.change(this.http.put<void>(this.url() + '/' + photo.id + '/main', { revision: this.gallery()!.revision }), 'De hoofdafbeelding is gewijzigd.');
  }
  remove(photo: ProductImage) {
    if (this.busy() || this.loading() || this.removing() !== photo.id) return;
    this.change(this.http.delete<void>(this.url() + '/' + photo.id, { body: { revision: this.gallery()!.revision } }), 'De foto is verwijderd.');
  }
  private change(request: Observable<void>, message: string, done: () => void = () => {}) {
    this.busy.set(true); this.writing = true; this.error.set('');
    const product = this.product();
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.busy.set(false); this.writing = false; if (this.product() !== product) return; done(); this.saved.emit(message); },
      error: error => {
        this.busy.set(false); this.writing = false; if (this.product() !== product) return;
        this.error.set(error instanceof HttpErrorResponse && error.status === 400 && error.error?.message
          ? error.error.message : errorMessage(error));
      },
    });
  }
}
