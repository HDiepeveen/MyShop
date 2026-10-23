import { Component, DestroyRef, inject, input, model, output, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Auth } from '../auth/auth';
import { ShopSeoSettings } from './seo.models';
import { errorMessage } from '../catalog/error-message';
@Component({selector:'app-page-background-editor', imports:[FormsModule], template: `
  <fieldset [disabled]="disabled() || busy()">
    <legend>Achtergrondfoto</legend>
    @if (imageUrl()) { <img [src]="imageUrl()" alt="Huidige achtergrondfoto" /> }
    <label>Weergave achtergrondfoto<select [ngModel]="mode() || 'cover'" (ngModelChange)="mode.set($event)" [ngModelOptions]="{standalone:true}">
      <option value="stretch">Uitrekken over het hele vlak</option>
      <option value="cover">Verhoudingen behouden, gecentreerd bijsnijden</option>
    </select></label>
    @if (!pageId() || pageId() === emptyId) { <p>Sla de nieuwe pagina eerst op om een foto te uploaden.</p> }
    @else {
      <label>Afbeelding kiezen<input #fileInput type="file" accept="image/jpeg,image/png" (change)="choose(fileInput)" /></label>
      <p>JPEG of PNG, maximaal 5 MB en 20 miljoen pixels. Uploaden bewaart alleen de foto; sla de overige velden op met de opslagknop van de betreffende tegel.</p>
      <button type="button" [disabled]="!file" (click)="change(fileInput)">Afbeelding uploaden</button>
      @if (imageUrl()) { <button type="button" class="secondary" (click)="change(fileInput,true)">Afbeelding verwijderen</button> }
    }
    @if (error()) { <p role="alert">{{ error() }}</p> }
    @if (notice()) { <p role="status">{{ notice() }}</p> }
  </fieldset>
`,styles:['img {display:block;max-width:300px;max-height:180px;object-fit:contain;border-radius:12px;}']})
export class PageBackgroundEditor {
  readonly emptyId='00000000-0000-0000-0000-000000000000';
  readonly pageId=input<string>(); readonly revision=input.required<string>(); readonly imageUrl=input<string|null>();
  readonly disabled=input(false); readonly mode=model<string|null|undefined>();
  readonly stored=output<ShopSeoSettings>(); readonly working=output<boolean>();
  readonly busy=signal(false); readonly error=signal(''); readonly notice=signal(''); file:File|null=null;
  private readonly http=inject(HttpClient); private readonly auth=inject(Auth); private readonly destroyRef=inject(DestroyRef);
  choose(input:HTMLInputElement) {
    this.file=null; this.error.set(''); this.notice.set(''); const file=input.files?.[0]; if(!file)return;
    if(!['image/jpeg','image/png'].includes(file.type)||file.size>5*1024*1024){this.error.set('Kies een JPEG- of PNG-afbeelding van maximaal 5 MB.');input.value='';return;}
    this.file=file;
  }
  change(input:HTMLInputElement,remove=false) {
    const id=this.pageId(); if(!id||id===this.emptyId||this.busy()||this.disabled()||(!remove&&!this.file))return;
    const form=new FormData();form.append('revision',this.revision());if(this.file)form.append('file',this.file,this.file.name);
    this.busy.set(true);this.working.emit(true);this.error.set('');this.notice.set('');
    this.auth.prepare().pipe(switchMap(()=>remove?this.http.delete('/api/page-images/'+id,{body:{revision:this.revision()}}):this.http.post('/api/page-images/'+id,form)),
      switchMap(()=>this.http.get<ShopSeoSettings>('/api/seo-settings')),takeUntilDestroyed(this.destroyRef)).subscribe({
      next:settings=>{this.stored.emit(settings);this.file=null;input.value='';this.busy.set(false);this.working.emit(false);this.notice.set(remove?'Afbeelding verwijderd.':'Afbeelding opgeslagen.');},
      error:error=>{this.busy.set(false);this.working.emit(false);this.error.set(errorMessage(error));}
    });
  }
}
