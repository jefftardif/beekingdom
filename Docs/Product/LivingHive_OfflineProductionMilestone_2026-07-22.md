# LivingHive — production manuelle après une absence

## Résultat joueur

La production de miel, de cire et de pollen ne disparaît plus lorsque
l’application mobile passe en arrière-plan ou se ferme. Au retour, LivingHive
calcule une durée reconnue, remplit uniquement les caches des trois bâtiments et
affiche un bilan localisé des quantités produites pendant l’absence.

Le bouton `Voir / View`, avec une cible de 44 px, ouvre le premier bâtiment
concerné. Il ne collecte rien. Le stock du joueur et la capacité globale ne
changent qu’après le geste manuel normal sur l’icône de production du bâtiment.
Fermer le bilan ne détruit pas les quantités en attente.

## Frontière mobile et serveur

| Responsabilité | Appareil mobile actuel | Serveur officiel |
|---|---|---|
| Rendu, animation et navigation | Oui | Non |
| Dernier instant reconnu | UTC local de démonstration, non fiable | Horloge UTC serveur |
| Production en attente | Cache local `v1` par bâtiment | État persistant futur par joueur/ruche/bâtiment |
| Taux, capacités et catalogue | Valeurs de la preview | Autorité serveur exclusive |
| Durée reconnue | Bornée à 12 h; les bâtiments atteignent leur propre capacité | Calcul serveur borné; le client ne fournit ni heure ni durée |
| Partition | Identifiant de profil embarqué | Joueur, ruche, monde et serveur |
| Protection | `PlayerPrefs` non protégé, démonstration seulement | Stockage serveur protégé |
| Crédit au stock | Jamais pendant la reprise | Mutation atomique lors de la collecte manuelle |
| Réconciliation | Remplacement futur par snapshot officiel | Révision et catalogue autoritaires |

Le téléphone n’est donc jamais une autorité économique. Le journal local rend
la démonstration jouable hors ligne; il ne constitue ni une preuve de temps, ni
un solde officiel, ni un reçu de collecte.

## Journal local et cycle de vie

`LocalPreviewManualProductionJournal` conserve une version, l’identifiant de
profil, une révision, le dernier marqueur UTC et au plus 16 entrées triées. Il
rejette une version inconnue et un autre profil, remet à zéro un JSON illisible,
supprime les bâtiments inconnus, déduplique par quantité maximale et borne
chaque pending à la capacité courante.

Pendant une session, LivingHive continue d’utiliser le temps monotone Unity et
écrit le journal toutes les 30 secondes. Une écriture forcée se produit à la
collecte, à la perte de focus, au passage en arrière-plan, à la désactivation de
la scène et à la fermeture. Le retour au premier plan relit le journal avant de
reprendre les ticks.

Un recul de l’horloge ne produit rien et ne réduit jamais le marqueur déjà
conservé. Un saut en avant est plafonné à 12 heures dans la preview; chaque
bâtiment reste ensuite limité par sa propre capacité. Ces garde-fous limitent la
corruption de la démonstration, sans prétendre sécuriser une économie officielle.

## Noyau serveur coordonné

L’Intégrateur a ajouté `HiveOfflineProductionSnapshotFactory`, une projection
read-only avec `PlayerId`, `HiveId`, `WorldId`, `GameServerId`, révision,
`ServerUtc`, marqueur antérieur, durée reconnue, version de catalogue et entrées
triées par bâtiment. Le calcul borne la durée et le pending, ramène une date
future à zéro et ne modifie aucun stock.

`HiveOfflineProduction:Enabled=false` reste désactivé par défaut et en
Production. Aucune route HTTP, activation, notification, candidature ou
opération de déploiement n’a été ajoutée. Le modèle durable de marqueur/pending
par bâtiment dans `PlayerHiveState`, le raccordement session/appartenance, la
mutation de collecte et la preuve de staging restent des portes explicites : ce
noyau n’est pas présenté comme une production serveur complète.

Rapport serveur :
`Docs/ProductionIntegration/LivingHive_OfflineProductionSnapshot_Core_2026-07-22.md`.

## Validation

- Compilation Unity jeu et éditeur : succès Tundra, `0 error CS`,
  `0 Compilation failed` dans `Artifacts/LivingHiveOfflineProductionCompile.log`;
  la F8 finale a recompilé le garde de cycle de vie livré.
- Suite F8 LivingHive finale : 60 contrôles, succès dans
  `Artifacts/LivingHiveOfflineProductionF8Final.log`.
- Cas nouveaux : accrual exact, capacité, journal corrompu, version inconnue,
  profil étranger, entrées dupliquées/inconnues, saut futur borné, recul
  d’horloge, redémarrage, navigation sans collecte et collecte persistée.
- Catalogues : `758/758` clés uniques et alignées en `fr-CA` et `en-US`.
- Serveur : suite HiveOperations `35/35`; build Release `0 erreur` après le
  noyau, puis preuve finale sans changement du code de production. Les gardes
  couvrent identités, UTC, durées, catalogue borné à 64, tri, capacités,
  futur/recul et non-mutation logique complète de l’état.
- Fin des validations Unity : `Unity=0`, `dotnet=0`, `testhost=0`.

## Preuves visuelles

Les deux captures ont été générées depuis `Assets/Scenes/LivingHive.unity` et
inspectées à leur résolution native :

- `LivingHive_OfflineProduction_Return_FR_390x844.png`, `390x844`, SHA-256
  `862d8c794542514d7bbe292dacd31ab11c8c0ca4a19b4ec75e0a4ed92c5528d4`;
- `LivingHive_OfflineProduction_Building_EN_1600x900.png`, `1600x900`, SHA-256
  `590bcdc154f332407212574350d1f57f8c7329eaf8f1b36a8f044d74d26e26e4`.

Manifeste :
`Docs/Product/Evidence/LivingHiveOfflineProduction/LivingHiveOfflineProduction_CaptureManifest.md`.

Les fondations conservent exactement leurs empreintes de référence :

- scène canonique 50x50 : 7 776 octets, SHA-256
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène `LivingHive.unity` : 9 160 octets, SHA-256
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive : 7 489 785 octets, SHA-256
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Aucun fichier Communication n’a été touché.

## Synchronisation VM

La synchronisation finale a été tentée avec la commande officielle
`tools/vm-sync/Synchroniser-BeeKingdom.cmd`. Elle a échoué avant toute copie :
le partage `\\DESKTOP-D3D29K7\BeeKingdomHost` a répondu `Accès refusé` dans
`BeeKingdom-VmSync.ps1`. Aucun contournement, remappage ou accès direct à `Z:`
n’a été effectué.

Le dernier rapport demeuré valide est daté du `2026-07-22T02:57:51Z`; il
indique `0` conflit bloqué et `4` suppressions historiques en attente. Cette
tranche reste donc disponible et validée uniquement dans la copie locale
`C:\projets\beekingdomgame-master` jusqu’à la prochaine synchronisation normale.

## Test manuel conseillé

1. Ouvrir `Assets/Scenes/LivingHive.unity`, passer en `Play/Game` et entrer dans
   la démonstration locale.
2. Fermer l’introduction en deux clics si son texte défile encore.
3. Noter les quantités à collecter dans la réserve de miel, l’atelier de cire et
   l’entrepôt de pollen, puis quitter Play pendant quelques minutes.
4. Relancer Play. Le bilan de retour doit afficher de nouvelles quantités sans
   changer les stocks du HUD.
5. Cliquer `Voir`: le bon bâtiment s’ouvre, mais aucune ressource n’est créditée.
6. Toucher ensuite l’icône de production du bâtiment. Cette seule action doit
   augmenter le stock et vider tout ou partie du cache selon la capacité libre.
