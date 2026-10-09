# Alleen assortiment tonen

Open in beheer **Betaalopties** en zet **Winkelmand en bestellen inschakelen** uit.
Sla de instellingen op. Het assortiment, prijzen, productkenmerken en variantkeuzes
blijven zichtbaar. Aantalkeuze, winkelmandknoppen, de navigatielink naar de winkelmand
en opnieuw bestellen verdwijnen. Een directe winkelmandlink toont dat bestellen is
uitgeschakeld; opgeslagen winkelmandinhoud wordt niet verwijderd.

De server weigert nieuwe bestellingen en nieuwe betaalstarts wanneer de instelling
uitstaat. Bestaande bestellingen, facturen en lopende betaalbevestigingen blijven
beschikbaar. Betaalopties en instructies blijven bewaard voor opnieuw inschakelen.
Vernieuw eerder geopende winkelpagina’s om de nieuwe instelling op te halen.

De instelling staat standaard aan. De migratie StorefrontCheckoutSwitch voegt de
kolom CheckoutEnabled aan PaymentOptions toe, met true voor bestaande gegevens.
Bij wijzigingen geldt dezelfde revisiecontrole als bij andere betaalinstellingen.
