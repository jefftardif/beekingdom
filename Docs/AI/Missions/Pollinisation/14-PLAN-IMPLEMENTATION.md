# 14 — Plan concret d'implantation Alpha puis Bêta

**Statut : PROPOSÉ — dossier de conception, pas mandat d'exécuter ce plan immédiatement.**
Cible du dépôt : C:/projets/beekingdomgame-master ; développement Unity sur PCJEFF, API officielle sur SRVESDT, GitHub comme historique. Adapter au fonctionnement réellement constaté à la date du développement.

## Discipline de livraison

- Chaque lot commence par **audit des contrats existants** et se termine par tests, preuve E2E et mise à jour de ces documents.
- Toujours modifications ciblées, branches/commits explicites ; **ne pas nettoyer/reset** le worktree main déjà modifié sans traitement préalable.
- Pas de refonte du paysage, des tuiles, des UI éprouvées ni du modèle de combat à chaque lot.
- Flags progressifs et procédure de retour arrière ; observer la charge et les ressources avant d'ouvrir à d'autres utilisateurs.
- Authentification, révision et idempotence d'abord ; visuels ensuite, aucune fausse action d'interface.

## Lot 0 — Réconciliation du réel et contrat produit

**Livrables :** liste des contrats officiels (Nurserie, Académie, Génétique, Recherche, Championnes, Alliance, Caserne, collecte, monde), schéma d'état, noms de bâtiment valides, catalogues de ressources, matrice de dépendances et paramètres V1 arbitrés.

**Sortie :** chaque modif future pointe vers une extension valide et un test de non-régression ; pas d'hypothèse sur déploiement de l'Académie ou NurseryCohorts.

## Lot 1 — Population civile persistante

- Agrégat d'effectifs civils, cohortes civiles versionnées et migration additive.
- Réservations, libération, récupération, stock et limites, sans tromper les compteurs militaires.
- Endpoints de consultation / lancement d'une cohorte / réclamation idempotente.
- Affichage minimal dans Nurserie (sous-section civile) ; FR-CA/EN-US.
- **Sortie :** naissance/croissance + reconnexion + doubles clics testés.

## Lot 2 — Sites floraux et expédition complète solo

- Catalogue stable 2 à 3 familles/sites pilotes publics sur la carte existante.
- Serveur partagé : réserves, fertilité initiale, lifecycle, permissions publiques.
- Mission civile : réservation -> voyage -> action -> retour -> bilan ; temps serveur.
- Butinage équilibré comme premier mode ; route source -> destination dans la capacité du modèle (livraison 1 ou 2 étapes à arbitrer).
- UI carte : marqueur, fiche, préparer, lancer, suivre, rapport.
- **Sortie :** 1 client peut lancer/fermer/revenir et voit même solde/essaim/écologie.

## Lot 3 — Première vraie pollinisation collective

- Même site visible identiquement par plusieurs sessions.
- Contributions des missions, ledger, jalon, nombre **distinct** de contributeurs actifs.
- Projet partagé pour une alliance, progression en temps réel ou par lecture, rewards exact-once.
- Bonus du niveau d'alliance et niveaux joueur introduits avec caps.
- **Sortie :** 2+ joueurs aboutissent ensemble ; 1 joueur ne peut pas simuler 2 participants.

## Lot 4 — Profondeur économique et progression (fin Alpha)

- Trois modes intensif / équilibré / restauration, régénération et biodiversité si non intégrées au lot 2.
- Parcours Académie, Génétique, Recherche et bonus championne compatibles avec les contrats existants.
- Premier objectif collectif clairement récompensé et tutoriel de butinage.
- **Sortie :** différence mesurable entre joueurs de niveau distinct, sans rendement exponentiel, et entre modes de mission.

## Lot 5 — Territoires, relais et escorte (Alpha avancée/Bêta)

- Relais floral d'alliance sur carte, accès et niveaux de contrôle (sans renverser frontières existantes).
- Escortes militaires réelles et missions exposées au danger PvE, protections novices.
- Captures de relais éventuelles avec fenêtre, règles de reprise et historique.
- **Sortie :** économie territoriale crée un objectif de protection et de conflit reproductible.

## Lot 6 — Pactes, coalitions et guerre florale (Bêta)

- Droit d'accès contractuel, pactes temporaires et projets multi-alliances.
- Partage de récompenses, jalons interalliances, journaux, refus/revocations sécurisés.
- Blocus/conflits/diplomatie avancée, défense des petites alliances et équilibrage PvP.
- **Sortie :** deux alliances coopèrent, rompent un pacte, protègent leurs civils et règlent les récompenses sans incohérence.

## Lot 7 — Événements et monde vivant (Bêta)

- Grande floraison, pucerons/parasites, fleurs rares, cycles saisonniers.
- Participation/compétition mondiale ; classements et récompenses distinctes.
- Relier lorsque pertinent les grandes ressources au raid de la tanière de l'ours, sans changer sa vocation initiale de raid multi-alliances.
- **Sortie :** événements rejouables, persistants, équitables, sûrs avec nombreux participants.

## Conditions minimales de l'Alpha jouable

- Le joueur possède une population civile réellement suivie par le serveur.
- Il voit un site floral sur la carte et effectue une mission complète qui produit une récompense et modifie un état écologique officiel.
- Plusieurs joueurs peuvent contribuer à un même objectif ; niveau joueur + alliance influencent le résultat.
- Le jeu fonctionne hors connexion entre lancement et retour.
- Aucun abus de soldes, duplication, régression militaire ou perte de compte.
- Interfaces FR/EN avec accents intacts, aucun clic à travers et marqueurs lisibles.
- Les autres idées restent documentées pour la Bêta, même si elles ne sont pas encore codées.

## Définition de « terminé » pour chaque lot

1. Documents de contrat/API approuvés et décision ouverte tranchée.
2. Migrations compatibles, tests unitaires/contractuels/intégration passés.
3. Test multi-joueur authentifié et test hors ligne réussis.
4. Validation visuelle Unity des deux langues + pas de clignotement UI.
5. Inventaire régressions des systèmes adjacents.
6. Feature flag, rollback documenté et télémétrie prête.
7. Commit(s) ciblé(s), review, pas de « git add . » sur main dirty.
8. Un rapport de livraison avec ce qui est réellement en place, non ce qui est simulé.

