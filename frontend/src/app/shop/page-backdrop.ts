import { Injectable, signal, untracked } from '@angular/core';
@Injectable({providedIn:'root'})
export class PageBackdrop {
  readonly url=signal<string|null>(null); readonly mode=signal('cover');
  readonly barBackground=signal('rgba(0,0,0,.68)'); readonly barText=signal('#fff');
  private owner:object|null=null;
  show(owner:object,url:string|null|undefined,mode:string|null|undefined) {
    this.owner=owner;
    if(untracked(this.url)!==(url||null)){this.barBackground.set('rgba(0,0,0,.68)');this.barText.set('#fff');}
    this.url.set(url||null);this.mode.set(mode==='stretch'?'fill':'cover');
  }
  clear(owner:object){if(this.owner===owner){this.owner=null;this.url.set(null);}}
  loaded(image:HTMLImageElement) {
    if(!this.url() || image.currentSrc !== new URL(this.url()!,document.baseURI).href)return;
    try {
      const canvas=document.createElement('canvas');canvas.width=32;canvas.height=32;const ctx=canvas.getContext('2d');if(!ctx)return;
      ctx.drawImage(image,0,0,32,32);const pixels=ctx.getImageData(0,0,32,32).data;let light=0;
      for(let i=0;i<pixels.length;i+=4) light+=(.2126*pixels[i]+.7152*pixels[i+1]+.0722*pixels[i+2])/255;
      const dark=light/(pixels.length/4)<.5;
      this.barBackground.set(dark?'rgba(255,255,255,.72)':'rgba(0,0,0,.68)');
      // An opaque-enough backing also keeps text readable over locally contrasting details.
      this.barText.set(dark?'#16231d':'#fff');
    } catch { /* Keep the readable fallback if the browser cannot inspect the image. */ }
  }
}
