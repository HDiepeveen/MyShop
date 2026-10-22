# Cookiebeleid [Bedrijfsnaam]

**CONCEPT — 22 oktober 2026. Gebaseerd op de gecontroleerde HD Web Framework-code en beperkte live controles. Controleer na wijzigingen en publicatie opnieuw welke opslag en externe diensten actief zijn.**

## Huidige inrichting van deze website

Dit concept is bedoeld voor een bedrijf dat HD Web Framework gebruikt als webshop: een website met assortiment, online bestellen en online betalen. Pas de bedrijfsgegevens, aangeboden producten en diensten, betaalmethoden en gebruikte leveranciers aan de werkelijke inrichting aan.


Dit beleid geldt voor [Websiteadres], aangeboden door [Bedrijfsnaam], [Volledig bedrijfsadres], KvK [KvK-nummer], btw [Btw-identificatienummer]. Vragen: [E-mailadres], telefoon [Telefoonnummer].

## Wat gebruiken wij?

Cookies zijn kleine gegevensbestanden die uw browser bij websiteverzoeken kan meesturen. Wij gebruiken ook lokale browseropslag, genaamd localStorage. Deze gegevens worden niet bij ieder verzoek automatisch meegestuurd, maar de website kan ze wel uitlezen.

Onze huidige website gebruikt deze technieken voor inloggen, beveiliging en — wanneer beschikbaar — de winkelmand en het hervatten van een betaling.

| Naam | Techniek en doel | Bewaring |
|---|---|---|
| `HDWebFramework.Admin` | Functionele inlogcookie voor klant- én beheeraccounts. Ondanks de technische naam wordt deze ook voor klanten gebruikt. | Sessiecookie; het inlogbewijs is maximaal dertig minuten geldig. De website verlengt die termijn niet automatisch bij ieder bezoek. |
| `HDWebFramework.Antiforgery` | Beveiligingscookie die samen met een controletoken helpt voorkomen dat een andere website ongewenst handelingen namens u uitvoert. | Sessiecookie, zonder ingestelde vaste vervaldatum. |
| `XSRF-TOKEN` | Bijbehorend beveiligingstoken dat de website kan meesturen bij wijzigingen en formulierverzoeken. | Sessiecookie, zonder ingestelde vaste vervaldatum. |
| `HDWebFramework.cart.v1` | localStorage met product- en variantidentificaties en aantallen van de winkelmand. | Geen automatische tijdslimiet ingesteld. Wordt bijgewerkt door winkelmandhandelingen; kan blijven staan tot overschrijven of verwijderen van browsergegevens. |
| `HDWebFramework.pending-payment.v1` | localStorage met een verwijzing naar een betaalpoging en een kenmerkenlijst van de bijbehorende winkelmand. Helpt een betaling terugvinden en dubbele betaling voorkomen. | Geen automatische tijdslimiet ingesteld. Wordt bij afhandeling van de bijbehorende poging verwijderd, of blijft bestaan tot overschrijven of verwijderen van browsergegevens. |

Een sessiecookie is niet hetzelfde als een blijvende cookie met een vaste einddatum. Browsers kunnen sessies herstellen; daarom kan het gedrag bij afsluiten verschillen. De geldigheidsduur van het inlogbewijs blijft afzonderlijk van toepassing.

De beveiligingscookies kunnen ook nodig zijn op een website waarop bestellen is uitgeschakeld. Winkelmand- en betaalopslag worden gebruikt wanneer u die functies gebruikt. Eerder opgeslagen gegevens verdwijnen niet noodzakelijk door het later uitschakelen van bestellen.

## Toestemming en externe websites

Voor opslag die strikt nodig is om de gevraagde websitefuncties te leveren, is geen voorafgaande cookietoestemming vereist. Wij hebben in de gecontroleerde applicatie geen ingebouwde advertentiepixels of Google Analytics aangetroffen. Een algemene cookietoestemmingsbanner is voor uitsluitend de beschreven noodzakelijke toepassingen niet vereist.

Wanneer u naar Mollie gaat om te betalen, gebruikt u een externe betaalomgeving. Daar geldt het cookie- en privacybeleid van de aanbieder. De telefoon- en WhatsApp-icoontjes in onze huidige menubalk laden op zichzelf geen WhatsApp-trackingcode.

Toevoegen van bijvoorbeeld advertentietracking, externe video's of andere ingesloten diensten vraagt een nieuwe beoordeling. Waar toestemming vereist is, mogen die onderdelen pas na die toestemming worden geactiveerd.

## Beheren en verwijderen

U kunt cookies en websitegegevens via de instellingen van uw browser bekijken, blokkeren en verwijderen. Kies daarbij ook lokale websiteopslag als u winkelmand- of betaalgegevens wilt wissen. Blokkeren kan inloggen, beveiligde formulieren, de winkelmand of het hervatten van betalingen verstoren. Het wissen van browseropslag annuleert geen bestelling of betaling bij ons of bij de betaalprovider.

Voor verwerking van gegevens op onze server verwijzen wij naar de privacyverklaring.
