import { TestBed } from '@angular/core/testing';
import { PageBackdrop } from './page-backdrop';
describe('Page background across header and footer',()=>{
 afterEach(()=>vi.restoreAllMocks());
 it('keeps the new page background when the previous page is destroyed',()=>{
  const backdrop=TestBed.inject(PageBackdrop), oldPage={},nextPage={};
  backdrop.show(oldPage,'/first.png','stretch');expect(backdrop.mode()).toBe('fill');
  backdrop.show(nextPage,'/next.png','cover');backdrop.clear(oldPage);expect(backdrop.url()).toBe('/next.png');expect(backdrop.mode()).toBe('cover');
  backdrop.clear(nextPage);expect(backdrop.url()).toBeNull();
 });
 it('uses lighter bars with dark text for a dark photo and darker bars with white text for a light photo',()=>{
  const backdrop=TestBed.inject(PageBackdrop);backdrop.show({},'/photo.png','cover');
  const image=document.createElement('img');Object.defineProperty(image,'currentSrc',{value:new URL('/photo.png',document.baseURI).href});
  let brightness=20;
  vi.spyOn(HTMLCanvasElement.prototype,'getContext').mockReturnValue({drawImage:vi.fn(),getImageData:()=>({data:new Uint8ClampedArray([brightness,brightness,brightness,255])})} as never);
  backdrop.loaded(image);expect(backdrop.barText()).toBe('#16231d');expect(backdrop.barBackground()).toContain('255,255,255');
  brightness=240;backdrop.loaded(image);expect(backdrop.barText()).toBe('#fff');expect(backdrop.barBackground()).toContain('0,0,0');
 });
});
