# Productpresentatie

Beheerders kunnen op de productdetailpagina een beschrijving, een hoofdafbeelding via een HTTPS-link, alternatieve tekst en de publicatiestatus beheren. Afbeeldingen worden in deze eerste versie elders gehost; MyShop slaat alleen de link op en haalt het bestand niet op via de server.

- Nieuwe en bestaande producten beginnen als concept, met een lege beschrijving en zonder afbeelding.
- Beschrijvingen zijn gewone tekst, maximaal 10.000 tekens. HTML wordt als tekst getoond, niet uitgevoerd. Regeleinden blijven zichtbaar.
- De afbeeldingslink is maximaal 2.048 tekens, gebruikt HTTPS en bevat geen gebruikersnaam of wachtwoord. Alternatieve tekst is verplicht bij een afbeelding en maximaal 250 tekens.
- Publiceren vereist een beschrijving en hoofdafbeelding met alternatieve tekst. Opslaan als concept trekt publicatie in. De beheerder kiest bij opslaan expliciet de gewenste status.
- Het voorbeeld toont de opgeslagen presentatie. Een onbereikbare externe afbeelding geeft een melding; MyShop kan de blijvende beschikbaarheid van die externe afbeelding niet garanderen.
- Een wijziging wordt met de revisie van het getoonde product opgeslagen. Is het product intussen gewijzigd, dan geeft de API 409 en blijft de invoer in het scherm staan; vernieuw de gegevens voordat je opnieuw opslaat.

`GET /api/products/{id}` geeft een `presentation`-object met `description`, `imageUrl`, `imageAlt` en `isPublished`. `PUT /api/products/{id}/presentation` ontvangt deze velden plus de verplichte `revision` uit de detailrespons. Ontbrekende producten geven 404; ongeldige invoer 400; verouderde revisies 409. De productlijst bevat `isPublished`. Alle routes blijven beveiligd met de beheerdersrol en CSRF-controle.

Publicatie is de selectie voor de toekomstige klantwinkel. Deze batch voegt geen openbare winkelroutes toe en verklaart producten niet automatisch verkoopbaar. Prijs, voorraad en checkout volgen in afzonderlijke mijlpalen. Er is één hoofdafbeelding; uploads en fotogalerijen vallen buiten deze versie.

Pas de migratie toe volgens [Database development](database-development.md). De migratie voegt vier kolommen aan Products toe en behoudt bestaande catalogusgegevens en revisies. Terugdraaien verwijdert de nieuwe presentatiegegevens. De automatische migratietest controleert het bijwerken van een bestaande catalogus.
