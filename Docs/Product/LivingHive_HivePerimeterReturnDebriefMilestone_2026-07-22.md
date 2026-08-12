# LivingHive — débrief de retour de sortie

Date : 22 juillet 2026  
Statut : tranche verticale locale ratifiée; production et déploiement fermés

## Valeur joueur

Après une réclamation de sortie, le joueur ne voit plus seulement l’état suivant
du cycle. Il reçoit un débrief exact qui explique ce que le serveur a réellement
crédité :

- montant de miel crédité;
- montant de pollen crédité;
- stock et capacité résultants pour chaque ressource;
- indication explicite d’un crédit complet ou plafonné par la capacité;
- identité du signal et révision du reçu serveur;
- rappel que le mobile n’a créé aucun gain.

Le débrief est accessible dans le parcours déjà ratifié
`Armée -> Préparer -> Voir sorties`. Le bouton `Continuer` masque seulement la
présentation du reçu en mémoire; il ne provoque aucune nouvelle mutation serveur.

## Autorité serveur

Le contrat Phase 5 retourne maintenant un `claimReceipt` durable contenant :

- joueur, ruche, sortie, signal, instance et cycle;
- révision et `ServerTimeUtc`;
- crédits réellement appliqués par ressource;
- soldes et capacités résultants.

Le crédit est plafonné séparément pour le miel et le pollen. Un stockage presque
plein produit un crédit partiel; un stockage plein produit un crédit nul. Même
clé et même commande rejouent la même preuve après reconstruction DurableJson,
sans second crédit. Une commande contradictoire avec la même clé retourne
`game.idempotency_conflict`.

Preuves serveur fournies par l’Intégrateur :

- service Phase 5 : 6/6;
- HTTP Phase 5 : 6/6;
- HiveOperations : 53/53;
- suite serveur : 266 réussis, 7 SQL externes ignorés, 0 échec;
- build Release : 0 erreur;
- aucun candidat, transfert, déploiement ou activation.

`HivePerimeterSortie`, `Chat`, `Realtime` et `DeploymentAuthorized` restent
fermés. Le serveur local validé n’est pas présenté comme production.

## Responsabilité de l’appareil

Le mobile :

- transmet l’identité de session, la révision attendue et la clé d’idempotence;
- valide strictement l’appartenance joueur/ruche/cycle/signal du reçu;
- refuse les ressources absentes ou supplémentaires;
- refuse un crédit négatif, supérieur à la récompense annoncée ou un crédit
  réduit que la capacité résultante n’explique pas;
- conserve le snapshot et le débrief seulement en mémoire pendant la session du
  panneau;
- ne persiste ni jeton d’accès, ni reçu autoritaire, ni gain;
- ne permet aucune mutation hors ligne.

Le contrôleur opérationnel injectable garde une même clé d’idempotence pour une
mutation en échec et son nouvel essai dans la session courante. Le contrôleur de
production par défaut demeure `NotConfigured` tant que le shell officiel de
session et le transport authentifié ne sont pas injectés.

## Présentation mobile

Portrait 390x844 et paysage 1600x900 partagent la même hiérarchie :

1. retour confirmé et signal;
2. preuve durable du serveur;
3. deux cartes de crédit réel;
4. explication crédit complet/plafonné;
5. action locale `Continuer`.

Toutes les cibles existantes restent au minimum à 44 px. Les états QA sont
marqués `APERÇU QA` et n’exécutent aucun appel serveur. Une première série de
captures a révélé un chevauchement portrait entre le crédit et le stock; elle a
été rejetée. La répartition verticale a été corrigée, puis F8 et les six captures
ont été entièrement rejoués.

## Localisation

Les catalogues `fr-CA` et `en-US` contiennent chacun :

- 1022 clés totales et uniques;
- 73 clés `perimeter_sortie.*`;
- 16 clés `perimeter_sortie.debrief.*`;
- aucun doublon ni asymétrie.

## Vérifications

- harnais contrat/presentation : 21/21;
- assemblage jeu généré par Unity : 0 erreur;
- assemblage éditeur généré par Unity : 0 erreur;
- avertissements existants de compatibilité/données inchangés et hors tranche;
- F8 final : `Artifacts/LivingHiveReturnDebrief_FinalF8.log`;
- marqueur `LivingHive manual collection checks passed.` : 1;
- `error CS`, `Compilation failed`, `AssertionException` : 0;
- capture finale : `Artifacts/LivingHiveReturnDebrief_FinalCapture.log`;
- succès capture : 1; échec, mauvaise dimension et erreur C# : 0;
- Unity, dotnet et testhost après validation : 0.

## Preuves visuelles finales

Manifeste :
`Docs/Product/Evidence/LivingHivePerimeterSortie/LivingHivePerimeterSortie_CaptureManifest.md`.

Nouveaux débriefs inspectés à leur résolution native :

- `LivingHive_PerimeterSortie_DebriefPartialQA_FR_390x844.png`, 390x844,
  crédit miel plafonné, SHA-256
  `b5f865b24a9eb2f2175070ed38945d8755fc5a4f9e67e0e8718ea0683c159011`;
- `LivingHive_PerimeterSortie_DebriefFullQA_EN_1600x900.png`, 1600x900,
  crédit complet, SHA-256
  `9238e91fe6fa7f2af0fd1648f7f01011973e701dd07bf4a5c7fa4ad9232faad6`.

Les quatre états sortie existants ont aussi été recapturés aux dimensions exactes
et contrôlés contre toute régression.

## Fondations protégées

- scène canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Les trois empreintes sont inchangées. Aucune scène, carte terrain ou image de
base n’a été modifiée.

## Synchronisation VM

La synchronisation prescrite a été tentée après la ratification le 22 juillet à
18:56 UTC. Elle a échoué avant toute copie : `Test-Path` reçoit `Accès refusé`
sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun partage n'a été remappé et aucune
écriture directe vers `Z:` n'a été tentée.

Le dernier rapport valide demeure `.codex/vm-sync-last-report.txt`, daté du
22 juillet à 02:57:51 UTC : 0 conflit, 0 copie VM vers hôte et 4 suppressions
historiques en attente. La tranche reste donc sur `C:` jusqu'à la prochaine
synchronisation accessible.

## Fichiers produit modifiés

- `Assets/BeeKingdom/Networking/HivePerimeterSortieClient.cs`;
- `Assets/BeeKingdom/Playground/HivePerimeterSortiePresentation.cs`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Tests/Editor/HivePerimeterSortieClientTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieCapture.cs`;
- catalogues `strings.fr-CA.json` et `strings.en-US.json`;
- six preuves PNG et leur manifeste.

Les fichiers serveur et tests exacts sont consignés dans le rapport de
l’Intégrateur Phase 5. Les petits projets sous `Artifacts/HivePerimeterClientHarness`
restent des harnais locaux de compilation et de preuve, pas du code joueur.

## Portes restantes

1. shell officiel d’authentification/session mobile;
2. transport REST authentifié de production;
3. cache protégé, borné et partitionné par joueur pour lecture hors ligne;
4. validation Android et staging;
5. décision explicite d’activation des drapeaux et autorisation de déploiement;
6. tests SQL externes dans leur environnement natif.

Aucune de ces portes n’est maquillée par les preuves QA locales.
