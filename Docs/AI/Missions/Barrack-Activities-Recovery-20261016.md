# Caserne — restauration premium et compatibilité patrouille (2026-10-16)

## Cause de la régression
La Caserne affichait l’ancien écran plein écran sans navigation premium. Les composants
approuvés « Entraînement » / « Armées », ainsi que la réservation liée à une championne,
existaient encore dans la sauvegarde Git 55264b3b. Le code courant était revenu
aux anciennes versions de présentation et des contrats client de réservation.

## Restauration ciblée
La composition Activités est revenue : en-tête compact, deux onglets, formation
officielle, niveau de la Caserne, population, roster des trois familles, compteurs,
actions d’entraînement, suivi et accélérations, puis onglet Armées avec patrouille
préparée, répartition des unités et championne. La préparation d’une nouvelle
patrouille reste conditionnée aux disponibilités officielles et à la championne.
L’ancien raccourci Armée rejoint désormais cet onglet au lieu de créer une fenêtre
distincte. Les usages du contrat serveur et les commandes en attente restent
idempotents et protégés. Aucun nouvel entraînement ou recrutement n’est envoyé
à l’ouverture.

Petits correctifs de robustesse : défilement des deux sections sur écran étroit,
accès au bouton Carte du monde, remise à zéro du défilement au changement d’onglet,
écran explicite de lecture indisponible/synchronisation/commande incertaine,
et affichage des champs de préparation sans inventer de disponibilité.
La section historique « Formation » transmet aussi la championne requise.

La restauration du grand presenter est limitée à 22 membres Caserne/réservation,
en conservant 3714 membres strictement inchangés, zéro suppression et zéro
modification hors du périmètre. Les sauvegardes avant opération sont hors dépôt.

## Vérifications
150 tests EditMode ciblés réussis, aucun échec : Caserne, recrutement,
réservation, formation, interface mobile, Alliance, Infirmerie et startup.
Preuve : ui-regression-tests-v3.json.

Contrôle visuel réel en Play Mode 1920×1080, scène canonique :
barrack-training-real.png et barrack-army-settled.png.
Données réelles : Caserne niveau 6, formation Ready, roster 40 Gardiennes,
54 Voltigeuses, 16 Lanceuses, population 110 / 2000.
Onglet Armées : réservation déjà présente, 32 Gardiennes, 4 Voltigeuses,
0 Lanceuse, Zephyra (championne affectée). Contrôleur Ready.
Preuve : live-authoritative-state.txt.

Test sans mutation : fermeture libère les entrées du monde, clic du bâtiment
ouvre Entraînement, passage à Armées fonctionne, identifiants de réservation et
championne restent identiques. Preuve : live-navigation-proof.txt.
Aucun Commit/Release, lancement de combat, entraînement ou débit effectué.
Le bouton de navigation Carte du monde pointe vers une scène disponible ;
le trajet complet de combat n’a pas été exécuté dans ce contrôle.

## Attention avant un déploiement serveur ultérieur
La version *locale du code serveur* et sa configuration de production dans Git
indiquent CombatSquadReservation et CombatPatrol désactivés et ne contiennent
pas toutes les garanties championne de la sauvegarde. La session serveur
effectivement connectée a pourtant renvoyé une réservation « Ready » avec
ChampionBeeIds=zephyra. Il faut auditer l’état du serveur de production actif
et sa parité avec le dépôt **avant toute publication serveur**. Ne pas écraser
des contrats serveur existants ni activer une fonctionnalité sans certification.

## Livraison
Local uniquement, commit Git ciblé sur la branche main. Aucun nouveau worktree,
aucun reset global, aucun push, build ou déploiement serveur. Aucune modification
aux fichiers non pertinents déjà présents dans le répertoire de travail.
Unity conservé en Play Mode et fenêtre Caserne > Armées ouverte.