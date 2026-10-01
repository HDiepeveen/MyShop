# Betaalopties

Beheerders beheren betaalopties via **Betaalopties**. Minimaal één betaaloptie moet actief blijven.

**Later betalen** kan direct worden gebruikt. De instructietekst wordt bij het plaatsen van de bestelling vastgelegd, zodat latere wijzigingen geen oude bestellingen veranderen.

**Direct online betalen** is voorbereid maar vereist providerconfiguratie. Zet in configuratie `Payments:Online:Provider` op de naam van de gekozen provider, bijvoorbeeld `Mollie` of `Stripe`. Zolang deze waarde leeg is, kan beheer online betalen niet inschakelen en blijft de optie verborgen voor klanten.

Wanneer de providernaam is gevuld en beheer online betalen inschakelt, verschijnt de optie in de winkel met de melding dat de klant naar die provider wordt doorgestuurd. De echte betaalstart en providerredirect worden in de volgende online-betalingsslice gekoppeld.
