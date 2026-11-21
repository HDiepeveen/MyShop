import { HttpErrorResponse } from '@angular/common/http';
import { errorMessage } from './error-message';
describe('catalog errors', () => {
  it('keeps an RDW outage separate from an assortment outage', () => {
    const message = 'De verbinding met RDW is mislukt (HTTP 403).';
    expect(errorMessage(new HttpErrorResponse({ status: 502, error: { code: 'rdw', message } }))).toBe(message);
    expect(errorMessage(new HttpErrorResponse({ status: 502 }))).toBe('Het assortiment is niet bereikbaar. Probeer het over een moment opnieuw.');
  });
});
