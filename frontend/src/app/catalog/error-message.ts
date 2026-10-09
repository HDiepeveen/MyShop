import { HttpErrorResponse } from '@angular/common/http';
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0 || error.status === 502 || error.status === 504)
      return 'De winkel is niet bereikbaar. Probeer het over een moment opnieuw.';
    if (error.status === 401) return 'Je sessie is verlopen. Log opnieuw in.';
    if (error.status === 403) return 'Je hebt geen toestemming voor deze handeling.';
    if (error.status === 404)
      return 'Deze gegevens zijn niet meer beschikbaar. Vernieuw het overzicht.';
    if (error.status === 400 && error.error?.code === 'csrf')
      return 'Je beveiligingstoken is verlopen. Vernieuw de pagina en probeer opnieuw.';
    if (error.status === 400) return 'Controleer de ingevulde gegevens en probeer het opnieuw.';
    if (error.status === 409 && error.error?.code === 'accountExists')
      return 'Voor dit e-mailadres bestaat al een klantaccount. Log in of herstel je wachtwoord.';
    if (error.status === 409)
      return 'Deze wijziging kon niet worden opgeslagen. Vernieuw de gegevens en probeer het opnieuw.';
  }
  return 'Er ging iets mis. Probeer het opnieuw.';
}
