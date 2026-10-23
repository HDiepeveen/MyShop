import { orderedWebsiteLinks, WebsiteLink } from './website-navigation';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ShopBranding } from './shop-branding';

describe('Website navigation', () => {
  it('preserves the default order, ignores removed entries and appends new links', () => {
    const links: WebsiteLink[] = [{key:'home',name:'Home',path:'/'},{key:'contact',name:'Contact',path:'/contact'},{key:'page:1',name:'APK',path:'/apk'}];
    expect(orderedWebsiteLinks(links).map(x=>x.key)).toEqual(['home','contact','page:1']);
    expect(orderedWebsiteLinks(links,['contact','removed','home']).map(x=>x.key)).toEqual(['contact','home','page:1']);
    expect(links.map(x=>x.key)).toEqual(['home','contact','page:1']);
  });
  it('sorts all page and category links and hides disabled pages', () => {
    TestBed.configureTestingModule({providers:[provideHttpClient()]});
    const branding=TestBed.inject(ShopBranding);
    branding.homeEnabled.set(true);
    branding.homePages.set([{pageId:'p',name:'APK',webAddress:'apk'}]);
    branding.categoryLinks.set([{id:'c',name:'Occasions',webAddress:'occasions'}]);
    branding.menuOrder.set(['contact','category:c','page:p','home','assortment']);
    expect(branding.navigationLinks().map(x=>x.name)).toEqual(['Contact','Occasions','APK','Home','Assortiment']);
    expect(branding.navigationLinks()[1].path).toBe('/assortiment/occasions');
    branding.contactEnabled.set(false); branding.homeEnabled.set(false); branding.showAssortmentInMenu.set(false);
    expect(branding.navigationLinks().map(x=>x.name)).toEqual(['Occasions']);
  });
});
