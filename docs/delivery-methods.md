# Bezorgopties

Beheerders bepalen onder **Bezorgopties** welke manieren van bezorgen tijdens het afrekenen beschikbaar zijn. Een optie heeft een naam, een optionele toelichting, een bedrag met valuta en een actieve status. De standaardmigratie voegt een gratis optie **Standaardbezorging** in, zodat bestaande installaties na de update direct kunnen blijven afrekenen.

De openbare winkel toont alleen ingeschakelde opties via `GET /api/shop/delivery-methods`. De klant kiest één optie voordat de adresgegevens worden verzonden. `POST /api/shop/orders` accepteert daarvoor `deliveryMethodId`. De server haalt de optie opnieuw op, weigert een ontbrekende of uitgeschakelde keuze en gebruikt nooit een bedrag uit de browser.

Naam, toelichting, bedrag en valuta worden samen met de bestelling als snapshot opgeslagen. Latere wijzigingen aan de beheeroptie veranderen bestaande bestellingen niet. De bezorgkosten worden bij het juiste valutaatotaal opgeteld; als de winkelmand andere valuta bevat, blijft ieder valutaatotaal afzonderlijk. Het beheer en de klantbestelgeschiedenis tonen de vastgelegde optie.

De beheerroutes `GET`, `POST`, `PUT` en `DELETE /api/delivery-methods` vereisen de rol `Administrator`; schrijfacties vereisen ook een antiforgerytoken. Wijzigen en verwijderen gebruiken de actuele revisie om gelijktijdige wijzigingen te beschermen. Bedragen worden als tekst met twee decimalen uitgewisseld.
