# LivingHive — qualification active du lot témoin

Date : 21 juillet 2026  
Statut : réalisé et ratifié dans la preview locale

## Résultat joueur

Après la production puis la collecte manuelle du lot témoin du chapitre 4, le
joueur doit maintenant relier la spécialisation choisie à son risque dominant :

- **Rendement** attend `Maîtriser la chaleur`;
- **Stockage** attend `Vérifier la tenue sous charge`.

Une réponse incorrecte conserve l'étape et explique le risque sans retirer de
ressource, ajouter de délai, appliquer de bonus ou incrémenter un compteur
économique. La bonne réponse ouvre une seule fois la première application. La
réserve est recommandée après Rendement et la nurserie après Stockage, mais les
deux chantiers restent disponibles.

La quantité affichée est celle réellement collectée : 120 cire normalement ou
160 après le gabarit du chapitre 3. La trousse conserve son lot de 120 et sa
réduction de coût d'application à 40 cire; le gabarit conserve 160 et le coût
normal de 80 cire.

Cette tranche ajoute une décision sans minuterie artificielle. Le chapitre 4
conserve 13 objectifs et 145 à 165 secondes, mais passe de 28 à 29 interactions
actives. Les chapitres 1, 4 et 5 partagent maintenant le plancher à 29.

## Correctif d'entrée tactile

Le test manuel a révélé que le premier écran narratif restait visuellement au
premier plan mais ne recevait pas le clic. Sur une Game View basse, son bouton
recouvrait le rail inférieur. IMGUI traitait les contrôles du rail avant le
bouton du tutoriel, qui ne recevait donc jamais l'événement.

Le présentateur désactive désormais les contrôles du HUD, des menus et des rails
pendant une étape guidée, puis restaure explicitement `GUI.enabled` avant de
dessiner le tutoriel. Les seules exceptions sont `WorldOpenMap` et
`ForageOpenMap`, où le bouton contextuel Carte/Ruche est la cible demandée. Les
hotspots de collecte et d'inspection restent gérés par leur filtre guidé.

La suite prouve que les sept introductions acceptent leur bouton en portrait et
paysage, que la couche inférieure est bloquée, et que l'exception Carte/Ruche
reste active. Le clic réel sera rejoué manuellement dans `LivingHive` lors de la
prochaine session utilisateur.

## Frontière appareil / serveur

La preview locale conserve seulement le rendu, les compteurs pédagogiques de
preuve et le dernier instantané reconnu. Elle est marquée
`local_preview_non_official`. Elle ne peut pas qualifier officiellement le lot
hors ligne.

Le contrat de production réserve au serveur l'identité, l'appartenance à la
ruche, la spécialisation, la collecte et sa quantité, la progression ordonnée,
la révision, l'idempotence et l'horodatage UTC. La commande de qualification ne
porte jamais la spécialisation ni la quantité comme preuve client et ne produit
aucun reçu économique. La première application reste une commande distincte.

L'Intégrateur de production a livré le noyau local sous `Server/`. La route
`POST /game/v1/hives/{hiveId}/workshop/batch-qualification` et le drapeau
`WorkshopBatchQualification:Enabled` restent fermés par défaut et en Production
jusqu'au raccordement du shell mobile, de l'authentification et de l'adaptateur
HTTP. Le hash d'idempotence inclut la révision attendue et la transaction relit
la spécialisation, la quantité collectée, l'étape et la révision autoritaires.
Références :

- `Docs/Product/LivingHive_WorkshopBatchQualificationDesign_2026-07-21.md`;
- `Docs/ProductionIntegration/Chapter4_WorkshopBatchQualificationContractAudit_2026-07-21.md`.

Validation serveur locale : 24/24 tests HiveOperations, build Release 0 erreur
avec un avertissement SqlClient préexistant, et smoke du drapeau fermé en 503
`game.unavailable`. Les tests HTTP WebApplicationFactory compilent mais aucun
test n'a été découvert avec le runtime de repli; ils restent à rejouer sous un
runtime .NET 8 natif avant ratification ou ouverture. Aucun candidat ni
déploiement n'a été produit. Rapport :
`Docs/ProductionIntegration/Chapter4_WorkshopBatchQualificationServerImplementation_2026-07-21.md`.

## Validation Unity

- Unity : `6000.5.3f1`;
- compilation globale : 0 `error CS`, 0 `Compilation failed`;
- suite LivingHive F8 : succès dans
  `Artifacts/WorkshopBatchQualificationFinalF8.log`;
- catalogues : 509 clés uniques dans `fr-CA` et 509 dans `en-US`, ensembles
  strictement identiques, dont 9 clés de qualification;
- campagne visuelle : 52 PNG sur 52, 26 en 390 × 844 et 26 en 1600 × 900,
  aucune dimension refusée;
- manifeste :
  `Artifacts/GuidedOpeningInstallation/GuidedOpeningInstallationManifest.md`;
- captures inspectées à résolution native :
  `Chapter4_BatchQualification_390x844.png`, SHA-256
  `86B0C5572769FFA56792765D74C71098AD995A0E503A483C67E7C3396F478E55`,
  et `Chapter4_BatchQualification_1600x900.png`, SHA-256
  `B7B0DAFA19572B651BD6BD632FCD92D84A8A121DF6A5DC956E0B4421C21C896A`.

Les deux écrans montrent le titre, la spécialisation Rendement, le lot reconnu de
120 cire, l'explication, les deux commandes tactiles et la fermeture sans texte
coupé ni collision avec le HUD ou le rail.

## Fondations protégées

Le validateur exact-crop Unity passe dans
`Artifacts/WorkshopBatchQualificationExactCropValidation.log` :

- 2 500 tuiles et 2 500 importeurs contrôlés;
- 4 900 voisinages;
- 0 gouttière incohérente et 0 pixel incohérent;
- scène canonique : 7 776 octets, SHA-256
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- image LivingHive : 7 489 785 octets, SHA-256
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`;
- manifeste runtime : 862 548 octets, SHA-256
  `880B30C432D44803BA118C29ADAE0B0A6F0093D1E64A2707FC46D5395B3F230D`,
  grille 50 × 50, 2 500 identifiants et 2 500 fichiers distincts.

Aucune scène, image, tuile, ressource terrain ni module Communication n'a été
modifié.

## Synchronisation

Le rapport local `.codex/vm-sync-last-report.txt` le plus récent indique zéro
conflit et quatre suppressions en attente. La synchronisation finale a été
relancée, mais le bac à sable a refusé la lecture du partage
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun droit n'a été élargi et aucune écriture
directe sur `Z:` n'a été tentée. La tranche reste donc complète sur la copie
locale `C:` et doit être synchronisée demain depuis la session utilisateur,
puis contrôlée par un nouveau rapport.

## Test manuel recommandé

Demain, ouvrir `Assets/Scenes/LivingHive.unity`, entrer en mode Play dans l'onglet
Game, puis cliquer deux fois au besoin sur le bouton central : le premier clic
peut révéler instantanément la narration si l'effet d'écriture n'est pas terminé;
le suivant ferme l'introduction et ouvre le premier objectif. Continuer jusqu'à
la collecte du lot du chapitre 4, essayer volontairement la mauvaise réponse,
puis la bonne.
