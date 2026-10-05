import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { CustomerOrderList } from './customer-order-list';
import { CustomerOrderApi, CustomerOrderPage } from './customer-order.api';
import { CustomerWishlist } from './customer-wishlist';
import { CustomerWishlistApi, WishlistPage } from './customer-wishlist.api';

const product = {
  productId: 'p',
  name: 'Shirt',
  imageUrl: null,
  imageAlt: '',
  isAvailable: true,
  addedAt: '',
};
for (const kind of ['orders', 'wishlist'] as const) {
  describe(kind + ' page guards', () => {
    function setup() {
      const orders = new Subject<CustomerOrderPage>();
      const wishlist = new Subject<WishlistPage>();
      const api = {
        list: vi.fn(() => (kind === 'orders' ? orders : wishlist)),
        remove: vi.fn(() => new Subject<void>()),
      };
      TestBed.configureTestingModule({
        imports: [CustomerOrderList, CustomerWishlist],
        providers: [
          provideRouter([]),
          { provide: CustomerOrderApi, useValue: api },
          { provide: CustomerWishlistApi, useValue: api },
        ],
      });
      const fixture =
        kind === 'orders'
          ? TestBed.createComponent(CustomerOrderList)
          : TestBed.createComponent(CustomerWishlist);
      if (kind === 'orders') orders.next({ items: [], offset: 0, limit: 20, totalCount: 0 });
      else wishlist.next({ items: [product], offset: 0, limit: 20, totalCount: 1 });
      return { page: fixture.componentInstance, api, orders, wishlist };
    }
    it.each([-20, 1, 20.5, NaN, Infinity, Number.MAX_SAFE_INTEGER + 1])(
      'rejects invalid offset %s without changing the displayed page',
      (offset) => {
        const { page, api } = setup();
        const displayed = page.page();
        page.load(offset);
        expect(api.list).toHaveBeenCalledTimes(1);
        expect(page.offset()).toBe(0);
        expect(page.page()).toBe(displayed);
        expect(page.loading()).toBe(false);
      },
    );
    if (kind === 'orders') {
      it('does not request previous page at zero or next page without more orders', () => {
        const { page, api, orders } = setup();
        const list = page as CustomerOrderList;
        list.previous();
        list.next();
        expect(api.list).toHaveBeenCalledTimes(1);
        orders.next({ items: [], offset: 20, limit: 20, totalCount: 50 });
        list.next();
        expect(api.list).toHaveBeenCalledTimes(1);
        list.previous();
        expect(api.list).toHaveBeenLastCalledWith(0);
      });
    } else {
      it('does not confirm or remove a stale wishlist item', () => {
        const { page, api } = setup();
        const list = page as CustomerWishlist;
        const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);
        list.remove({ ...product });
        expect(confirm).not.toHaveBeenCalled();
        expect(api.remove).not.toHaveBeenCalled();
        expect(list.removing()).toBe('');
        list.remove(product);
        expect(confirm).toHaveBeenCalledOnce();
        expect(api.remove).toHaveBeenCalledWith('p');
        confirm.mockRestore();
      });
    }
  });
}
