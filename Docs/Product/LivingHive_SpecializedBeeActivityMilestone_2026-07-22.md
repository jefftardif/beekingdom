# LivingHive — comportements spécialisés des abeilles

## Statut

La tranche est implémentée et ratifiée dans Unity 6000.5.3f1. Les abeilles déjà
présentes dans la ruche rendent maintenant leur fonction perceptible par de
petites boucles visuelles, sans ajouter de personnage, de clic ou d’état de jeu.

## Résultat joueur

Les cinq zones actives possèdent désormais une signature visuelle propre :

- réserve de miel : une ouvrière transporte des charges de nectar sur un trajet
  court;
- atelier de cire : des points de façonnage tournent autour du poste de travail;
- entrepôt : les charges de pollen sont triées le long de la cellule;
- nurserie : de petits repères de soin accompagnent la nourrice;
- poste de garde : une ligne de patrouille relie les deux points de surveillance.

La boucle du bâtiment sélectionné devient plus lisible, mais cette emphase reste
une présentation locale. Elle ne lance pas une opération et ne valide aucune
tâche. Les accessoires réemploient les abeilles existantes : la couche ajoute
exactement zéro abeille aux budgets de cinq en portrait et huit en paysage.

Mouvement réduit fige chaque boucle à des positions déterministes. Le mode
économie conserve un seul accessoire et réduit le budget ambiant global à trois
abeilles en portrait et cinq en paysage. Les abeilles réellement affectées à une
tâche restent préservées.

## Frontière appareil / serveur

| Responsabilité | Appareil mobile | Serveur |
|---|---|---|
| Dessiner abeilles, accessoires et trajets | Oui | Non |
| Mouvement réduit et mode économie | Préférences locales | Aucune autorité |
| Bâtiment, ressource, pending, capacité et taux | Lecture seulement | Autoritaire |
| Opération, destination et temps UTC | Dernier instantané reconnu | Autoritaire |
| Transport lié à une production explicite | Animation dérivée | Fait source |
| Soin, patrouille ou entretien non distingué | Identité générique ou cachée | Ne jamais inventer |
| Stock, population, file, progression ou coût | Aucune mutation | Autoritaire |

L’Intégrateur confirme que `HiveOfflineProductionSnapshot` et
`HiveOperationResumeSummary` suffisent lorsque le fait serveur est explicite.
Lorsque soin, patrouille, ventilation ou entretien ne sont pas distingués, le
client doit rester générique ou ne rien afficher. Aucun nouveau contrat, endpoint
ou drapeau n’est requis. Rapport :
`Docs/ProductionIntegration/LivingHive_SpecializedBeeVisuals_DeviceServerBoundary_2026-07-22.md`.

## Validation

- compilation Unity jeu/éditeur : `0 error CS`, `0 Compilation failed` dans
  `Artifacts/LivingHiveSpecializedBeeActivity_Compile.log`;
- suite F8 finale : 80 contrôles réussis, aucun échec ni erreur de compilation
  dans `Artifacts/LivingHiveSpecializedBeeActivity_F8.log`;
- cinq nouveaux contrôles : catalogue des cinq comportements, budgets des
  accessoires, mouvement réduit, autorité appareil/serveur et zones inconnues;
- compilation C# de secours incluant jeu, tests et harnais : `0 erreur`, 217
  avertissements historiques dans
  `Artifacts/LivingHiveSpecializedBeeActivity_FallbackBuild.log`;
- catalogues `fr-CA` et `en-US` inchangés : `793/793` clés uniques, alignées et
  sans doublon;
- harnais visuel :
  `BeeKingdom.Playground.Editor.SandboxLivingHiveSpecializedBeeActivityCapture.CaptureAndExit`,
  sortie propre et dimensions strictes dans
  `Artifacts/LivingHiveSpecializedBeeActivity_Capture.log`;
- fin de validation : `Unity=0`, `dotnet=0`, `testhost=0`, `bee_backend=0`.

## Preuves visuelles

Les deux images ont été inspectées à leur résolution native. En portrait, le
trajet statique et ses charges de nectar restent dans la chambre sélectionnée.
En paysage, les repères de pollen sont visibles près de l’entrepôt sans masquer
les contours, le panneau ou les abeilles.

- `LivingHive_SpecializedBeeActivity_FR_390x844.png`, `390x844`, transport du
  nectar en mouvement réduit, SHA-256
  `5b111d2bd00a4d8e6ecea7f85b7764b82a6831dcb656f6cec602b47da2c65895`;
- `LivingHive_SpecializedBeeActivity_EN_1600x900.png`, `1600x900`, tri du pollen
  animé, SHA-256
  `60a58413fe0f8c6c1085dc9eb77b11748a44482f92597cf6cde6d8442436f0b1`.

Manifeste :
`Docs/Product/Evidence/LivingHiveSpecializedBeeActivity/LivingHiveSpecializedBeeActivity_CaptureManifest.md`.

## Fondations protégées

- scène canonique 50x50 : SHA-256
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène `LivingHive.unity` : SHA-256
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive : SHA-256
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Aucun terrain, image de carte, image de ruche, scène ou fichier Communication
n’a été modifié.

## Fichiers exacts

- `Assets/BeeKingdom/Playground/HiveSpecializedBeeActivityPresentation.cs` et
  `.meta`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveSpecializedBeeActivityTests.cs`
  et `.meta`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveSpecializedBeeActivityCapture.cs`
  et `.meta`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`;
- rapport de frontière serveur de l’Intégrateur;
- ce rapport, le manifeste et les deux PNG;
- mémoire officielle LivingHive, plan d’exécution et continuation VM.

## Test manuel conseillé

1. Ouvrir `Assets/Scenes/LivingHive.unity`, passer en Play et entrer dans la
   démo locale.
2. Fermer l’introduction, puis sélectionner successivement la réserve, l’atelier,
   l’entrepôt, la nurserie et le poste de garde.
3. Observer le trajet ou les accessoires propres à chaque rôle et confirmer que
   la sélection ne modifie aucun stock, niveau, effectif ou minuterie.
4. Ouvrir `Plus -> Confort mobile`, activer mouvement réduit puis mode économie,
   et vérifier que les accessoires se figent puis se réduisent à un seul.
5. Revenir aux réglages normaux et confirmer que les abeilles affectées à une
   vraie tâche restent visibles.

## Synchronisation VM

La commande officielle `tools/vm-sync/Synchroniser-BeeKingdom.cmd` a été tentée
avant la tranche puis sur l’état final à `2026-07-22T10:03:26Z`. Elle a échoué
avant toute copie avec `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun contournement, remappage ou accès
direct à `Z:` n’a été tenté.

Le dernier rapport valide reste daté du `2026-07-22T02:57:51Z`, avec 0 conflit
bloqué et 4 suppressions historiques en attente. Cette tranche est donc validée
uniquement dans la copie locale `C:\projets\beekingdomgame-master` jusqu’à la
prochaine synchronisation lancée depuis la session utilisateur.
