# Prijsinvoer en btw

Deze eerste facturatieslice voegt netto prijsinvoer en btw-opslag toe. Beheer voert een basisprijs exclusief btw in en kiest 21%, 9%, 0% of Vrijgesteld. De preview en de productdetails tonen prijs, btw en klanttotaal afzonderlijk. De winkel blijft uitsluitend totaalprijzen tonen. Het juiste tarief kiezen blijft de verantwoordelijkheid van de beheerder; deze slice bepaalt niet automatisch de fiscale behandeling van buitenlandse of zakelijke afnemers.

De backend berekent het btw-bedrag per artikel rekenkundig op centen: een halve cent wordt omhoog afgerond. De klantprijs is netto plus dat afgeronde btw-bedrag. In de browser gebeurt de preview met gehele centen, zonder berekening met zwevendekommagetallen. Bestaande beperkingen op exact verzendbare prijsinvoer blijven gelden. De backend is leidend.

Bijvoorbeeld: netto EUR 100,00 met 21% geeft EUR 21,00 btw en EUR 121,00 klantprijs. Netto EUR 0,50 met 9% geeft EUR 0,05 btw en EUR 0,55 klantprijs. 0% en Vrijgesteld blijven afzonderlijke behandelingen, ook als hun bedragen gelijk zijn. Zie [Belastingdienst: btw-tarieven](https://www.belastingdienst.nl/wps/wcm/connect/bldcontentnl/belastingdienst/zakelijk/btw/btw_berekenen_aan_uw_klanten/btw_berekenen/btw_tarief/btw_tarief) en [btw afronden](https://www.belastingdienst.nl/wps/wcm/connect/bldcontentnl/belastingdienst/zakelijk/btw/administratie_bijhouden/facturen_maken/btw-bedrag_afronden).

## Bestaande prijzen en API

De migratie `VatPriceAndOrderSnapshots` wijzigt geen bestaande bedragen en kent oude prijzen of bestellingen geen btw-percentage toe. Een bestaande klantprijs blijft zichtbaar, maar beheer moet bij omzetting bewust een netto bedrag invullen. Netto invoer wordt afzonderlijk opgeslagen om herhaald afronden bij opnieuw openen te voorkomen.

De prijsroute accepteert voor nieuwe invoer `netAmount`, `currency`, `vatRate` en `vatExempt`. De bestaande `amount` blijft een klantprijs inclusief btw. Netto invoer vereist een btw-keuze; een combinatie van een niet-nul legacy bedrag en netto invoer wordt afgewezen. Bestaande clients zonder btw-velden veranderen de betekenis van hun bedrag niet en behouden eventueel bekende btw-informatie. `price.amount` in beheer blijft voor compatibiliteit de klantprijs; aanvullende tekstvelden `netAmount`, `vatAmount`, `grossAmount`, plus `vatRate`, `vatExempt` en `isNetPrice`, beschrijven de uitsplitsing en invoerbasis.

Prijsregels blijven op de klantprijs inclusief btw werken, zoals vóór deze wijziging. Deze slice interpreteert bestaande vaste kortingen niet ineens als bedragen exclusief btw. Bij verkoop wordt de btw per artikel uit de effectieve klantprijs afgeleid en op centen afgerond. De regelbedragen exclusief btw en btw zijn de afgeronde artikelbedragen maal de hoeveelheid; samen blijven zij exact gelijk aan het regelbedrag inclusief btw.

## Vastlegging

Nieuwe orderregels bewaren de bekende btw-keuze, het nettoregelbedrag en het btw-regelbedrag. Betaalstarts bewaren de btw-keuze naast hun bestaande prijsgegevens; afronden van online betaling gebruikt die opgeslagen keuze en haalt geen nieuwe producttarieven op. Dit vormt de basis voor latere facturen. Oude regels zonder btw blijven expliciet onbekend.

Factuurnummering, bedrijfsgegevens in beheer, zakelijke factuurgegevens, factuur- en creditnotadocumenten, btw op bezorgkosten, buitenlandse belastingregels en btw-rapportage volgen in volgende gerichte slices. Het bestaande afdrukbare besteloverzicht is nog steeds geen fiscale factuur. Pas de migratie toe op de daadwerkelijke applicatiedatabase voordat de nieuwe code wordt gestart; tests gebruiken eigen geïsoleerde databases.
