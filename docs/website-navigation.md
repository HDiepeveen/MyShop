# Pagina’s en menubalk

In Beheer → Website en SEO staan Home pagina en Contact pagina. Beide kunnen worden in- en uitgeschakeld; bestaande contactpagina’s blijven standaard ingeschakeld. Uitschakelen bewaart de contactgegevens en foto voor later. Telefoon- en WhatsApp-instellingen voor de menubalk blijven onafhankelijk beschikbaar.

Bij Volgorde in menubalk verplaatsen Omhoog en Omlaag alle zichtbare paginalinks, inclusief categorieën. Instellingen opslaan bewaart de volgorde. Account-, winkelmand- en beheerlinks blijven op hun bestaande positie. Nieuwe zichtbare links worden achteraan toegevoegd zodra een eigen volgorde is opgeslagen. Uitgeschakelde links behouden hun opgeslagen positie als ze opnieuw worden ingeschakeld.

De instellingen ContactEnabled en MenuOrder worden samen met de bestaande websitepagina-instellingen in CatalogSeoSettings.HomePageJson opgeslagen. Een schemawijziging is hiervoor niet nodig. MenuOrder verwijst naar vaste sleutels home, assortment, contact, page:{PageId} en category:{CategoryId}; een titelwijziging verandert de positie niet. Verwijderde sleutels worden bij weergave genegeerd.

Een uitgeschakelde contactpagina levert via /api/shop/company en de serverpagina /contact een 404 op. Contactlinks in de menubalk, Home, extra pagina’s en juridische pagina’s worden verborgen. De HTML voor zoekmachines gebruikt dezelfde menulinkvolgorde als de interactieve website.
