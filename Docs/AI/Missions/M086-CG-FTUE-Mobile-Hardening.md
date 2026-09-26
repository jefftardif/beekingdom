# M086 — FTUE Mobile Hardening

## Cause et audit
À 320 px, le dialogue réservait 170 px au portrait et 114 px aux espacements/actions : seulement 12 px pour le texte, avec un bouton haut de 28 px et aucune safe area. Écart prioritaire corrigé : lisibilité et interaction du dialogue mobile. Le registre de highlight utilise déjà des rectangles horodatés ; fermeture coordonnée, événements gameplay et persistance restent inchangés.

## Fichiers modifiés
- `Assets/BeeKingdom/Tutorial/Runtime/TutorialDialoguePresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/FtuePresentationCleanupTests.cs`
- Ce rapport.

## Implémentation
Disposition compacte avec portrait contenu, texte pleine largeur (272 px à 320 px sans encoche), police 16 unités, défilement et actions séparées de 48 unités ; prise en compte de la safe area et de la résolution. Bouton localisé « Continuer » ou « Fermer » selon le callback ; chemin commun protégé contre une seconde activation après fermeture, sans validation artificielle des étapes Require*. Les rectangles publics d’occlusion suivent le panneau compact ; aucune modification d’architecture.

## Vérification
Compilation Roslyn ciblée des sources FTUE et tests avec références Unity 6000.5.3f1 locales : 0 erreur, 1 avertissement préexistant (`_completedNotified` inutilisé). **17 contrôles managés réussis** via un lanceur .NET local : 6 cas de géométrie (320, 390, 1170 px portrait, paysage, bureau, safe area vide) et 11 tests existants du moteur, incluant reprise/persistance en mémoire, idempotence et parcours complet.

Deux lancements Unity EditMode ciblés ont échoué avant compilation, code 1 : serveur Package Manager/IPC indisponible. Les tests natifs de présentation, la compilation complète jeu/éditeur et le rendu tactile ne sont donc pas certifiés ; traces locales `Logs/M086-*` (non versionnées).

## Points ouverts
Exécuter les tests EditMode et vérifier sur appareil le défilement tactile, la rotation, l’encoche et la coexistence dialogue/highlight. L’audit relève aussi des dialogues encore écrits directement en français dans les définitions ; leur migration complète vers les catalogues reste hors de ce correctif ciblé. Aucune certification de persistance serveur réalisée.

## Acceptation
- [x] Audit FTUE ciblé et correction de l’écart principal mobile.
- [x] Géométrie sûre, boutons distincts, contrôles managés et compilation ciblée.
- [x] Architecture, moteur et stockage inchangés ; Chat Royal et World Map Marches intacts.
- [x] Travail dans le worktree ; aucun push ni merge main.
- [ ] Tests Unity natifs et validation visuelle/tactile sur appareil.
