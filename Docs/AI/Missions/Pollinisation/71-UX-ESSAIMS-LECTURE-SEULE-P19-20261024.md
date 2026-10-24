# P19 - L'aperçu Essaims ne doit pas faire croire que la formation est active

Date : 24 octobre 2026.
Constat en GameView authentifiee : Disponibles 0, en mission 0,
capacite 80000, "Nurserie disponible" et bouton dore "Former une cohorte".

## Correction UI
- La capacite est explicitement une limite MAXIMALE, non une population.
- Nombres formates suivant la langue FR/EN (ex. 80 000 / 80,000).
- Si seuls les champs civils sont visibles et expeditions non ouvertes,
  afficher FORMATION A VENIR / TRAINING COMING SOON.
- Carte d'information : APERCU EN LECTURE SEULE / READ-ONLY PREVIEW.
- Texte sans ambiguite : formation desactivee, aucun miel ni pollen debite.
- NE PAS CREER le bouton payant de cohorte dans ce mode.
- En mode productif futur, conserver la vraie commande et ses gardes
  server-authoritative. Un statut "Nurserie disponible" s'affiche seulement
  quand la commande peut effectivement demarrer.
- Ne pas modifier les soldes, la capacite, les regles SQL ou les drapeaux
  de production. L'aperçu de population est valide, un effectif de 0 est reel.

## Tests
- Nouveau test de non-regression de l'interface P8 :
  transition en preview, libelles FR et EN, et absence totale de bouton de
  formation; retour a la presentation productive fictive.
- Unity EditMode Pollination (copie isolee) : 11/11 PASS.
- WorldMap : deux campagnes 7/8 pour erreurs reseau du module tiers
  Unity MCP (ObjectDisposedException/HubConnection), aucun changement
  WorldMap dans ce P19. Ne pas annoncer 8/8 sur cette livraison.
- Le test de controle premium cible : 1/1 PASS.
- API SRVESDT en P18 : GET civil actif, POST cohorte 503, expeditions et
  projets d'alliance OFF; aucune mutation necessaire pour P19.

## Validation restante
- Transferer les deux fichiers Unity au projet principal apres sauvegarde
  et verification des empreintes; la recompilation peut redemarrer Play.
- Revoir l'onglet Essaims en GameView officielle et FR/EN.
- Ne pas activer de depense tant que l'equilibrage n'a pas ete approuve.