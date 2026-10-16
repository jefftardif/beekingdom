# BeeKingdom — Client Unity des couvées, 15 octobre 2026

## Statut certifié

- Source : Assets/BeeKingdom/Networking/NurseryCohortClient.cs.
- 12 tests spécifiques : Assets/BeeKingdom/Tests/Editor/NurseryCohortClientTests.cs.
- 26 tests Unity EditMode réussis sur 26 dans le vrai projet Unity 6000.5.3f1.
- Résultats XML : C:\BeeKingdom\DEV\BK-Studio\reports\nursery-client-20261015\unity-editmode-results.xml.
- Sources de référence : branche bkstudio/nursery-growth-20261014;
  commits e7a8f974 et 0666078f.
- Contrats HTTP déjà présents dans le serveur officiel, activables seulement
  via l'option NurseryCohorts, désactivée par défaut en développement et en production.

## Règles de sécurité

- Aucune création locale de recrues et aucun mouvement dans la Caserne.
- Le nouveau client est également désactivé par défaut et n'est relié à
  aucun contrôleur automatique de la session mobile.
- Authentification du joueur, validation de la ruche et des données serveur.
- POST de démarrage uniquement sur demande explicite; clé idempotente fournie
  par l'appelant, jamais régénérée en retry HTTP implicite.
- Réauthentification 401 une fois avec identité inchangée et même requête.
- Erreur de transport : arrêt immédiat, sans recréer une couvée.
- Données incohérentes et réponse d'un autre joueur : refus.

## Restauration de l'interface premium

La fenêtre Nurserie plein écran previously Play-approved n'était plus dans le
main réconcilié 9d689c53. L'essai de restauration sélective du stash de
sécurité BKSTUDIO-SAFETY-20261014 révélait des dépendances manquantes dans
le main actuel : interface Banque, constructeur de Blasons, fonctions
Activities CanvasScale/SafeCanvasRect et ressources premium du Presenter.

L'ensemble des 18 fichiers restaurés a été remis exactement dans son état
précédent, grâce aux empreintes et sauvegardes :
C:\BeeKingdom\DEV\BK-Studio\reports\nursery-ui-recovery-20261015\manifest.json

Aucun stub fictif ne remplace les fonctions Banque ou Blasons et aucun
reset/merge global n'a été exécuté.

## Étapes restantes

1. Réintégrer les fenêtres premium et leurs vrais contrôleurs à partir
   de la sauvegarde approuvée, dans une branche d'intégration complète,
   en préservant le main et ses ressources non suivies.
2. Brancher explicitement NurseryCohortClient à une présentation de couvée,
   avec un optionnement staging uniquement; aucune mutation hors ligne.
3. Tester le cycle réel d'une couvée, la minuterie, le retour des recrues,
   les changements de session et les éventuelles pannes serveur dans Unity Play.
4. Valider sur base SQL de staging et certifier l'absence de création d'une
   ruche supprimée en parallèle.
5. Lier ensuite réserve et Caserne par migration sécurisée et test de non-perte
   des effectifs militaires existants.
6. Ne déployer ni activer sur SRVESDT tant que ces critères ne sont pas passés.

Agents BK Studio arrêtés; publication mobile et API non effectuées.
