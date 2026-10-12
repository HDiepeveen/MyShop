import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { MollieNavigation } from './mollie-navigation';
describe('Direct Mollie navigation', () => {
  it('navigates the current page to the Mollie checkout', () => {
    const assign = vi.fn(); TestBed.configureTestingModule({ providers: [{ provide: DOCUMENT, useValue: { location: { assign } } }] });
    TestBed.inject(MollieNavigation).open('https://www.mollie.com/checkout/existing');
    expect(assign).toHaveBeenCalledWith('https://www.mollie.com/checkout/existing');
  });
  it.each(['http://www.mollie.com/checkout', 'https://other.example/checkout', 'javascript:alert(1)', 'https://user:password@www.mollie.com/checkout'])('rejects unsafe destination %s', url => {
    const assign = vi.fn(); TestBed.configureTestingModule({ providers: [{ provide: DOCUMENT, useValue: { location: { assign } } }] });
    expect(() => TestBed.inject(MollieNavigation).open(url)).toThrow(); expect(assign).not.toHaveBeenCalled();
  });
});
