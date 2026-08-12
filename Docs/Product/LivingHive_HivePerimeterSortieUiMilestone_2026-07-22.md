# LivingHive — interface mobile de sortie au périmètre

## Résultat

Le parcours `Armée -> Préparer -> Voir sorties` ouvre maintenant une surface
bilingue adaptée au téléphone et au paysage. Elle montre l'état de connexion,
la réservation d'escouade, les deux signaux non-combat du cycle, le risque
doctrinal, le seuil, la durée, la récompense annoncée et les actions que le
snapshot serveur autorise réellement.

Le contrôleur de production demeure volontairement indisponible. Sans injection
du shell mobile officiel, la surface affiche `Non configuré`, deux cartes en
attente du serveur et aucun bouton de mutation actif. Elle ne transforme pas le
brouillon local en réservation, ne lance rien et ne crédite rien.

## Interaction et ergonomie mobile

- portrait 390x844 : les deux signaux sont empilés et le panneau reste entre le
  HUD supérieur et le rail inférieur;
- paysage 1600x900 : les deux signaux sont côte à côte;
- retour, cartes de signal et actions ont une cible minimale de 44 px;
- revenir à l'escouade conserve le brouillon de composition en mémoire;
- seul un signal marqué `CanLaunch` par le snapshot peut être sélectionné;
- réserver transmet exactement le brouillon courant Gardiennes/Voltigeuses/
  Lanceuses au contrôleur injecté;
- réclamer et rappeler ne sont exposés que dans les états serveur correspondants;
- le compte à rebours part de `ServerTimeUtc` et avance avec un temps monotone
  local; l'horloge murale du téléphone n'autorise jamais une réclamation.

## Frontière appareil / serveur

### Reste sur l'appareil

- rendu, langue et navigation;
- sélection courante d'un signal;
- brouillon de composition volatile;
- dernier modèle reçu en mémoire et durée monotone écoulée depuis sa réception;
- futures notifications locales, sans crédit économique.

### Reste sur le serveur

- compte, session et jetons;
- roster, capacité et réservation officielle;
- cycle UTC, révision, `SignalInstanceId`, éligibilité et sortie active;
- heure de fin, idempotence, rappel, réclamation et soldes de ressources.

Cette tranche ne persiste aucun jeton, snapshot, signal, réservation ou gain sur
l'appareil. Elle n'ajoute aucune coordonnée ni combat à la carte mondiale.

## Preuves techniques

- harnais autonome client + présentation : **18/18**;
- compilation `netstandard2.1` : **0 erreur, 0 avertissement**;
- assemblage `BeeKingdom.Tests` : **0 erreur, 0 avertissement**;
- assemblages jeu + éditeur, nouveau harnais compris : **0 erreur**;
- 217 avertissements historiques du projet, aucun nouveau blocage de tranche;
- catalogues `fr-CA` et `en-US` : **1006/1006** clés uniques et alignées,
  dont **57/57** clés `perimeter_sortie.*`;
- dix assertions Unity de présentation, routage des actions, conservation du
  brouillon, rectangles et localisation passent dans la suite F8 complète;
- F8 Unity 6000.5.3f1 : marqueur
  `LivingHive manual collection checks passed.`, zéro `error CS`, sortie propre
  dans `Artifacts/LivingHivePerimeterSortie_FinalF8.log`.

## Ratification Unity

Le harnais
`BeeKingdom.Playground.Editor.SandboxLivingHivePerimeterSortieCapture.CaptureAndExit`
produit exactement quatre preuves sous
`Docs/Product/Evidence/LivingHivePerimeterSortie` :

- état réel non configuré, FR, 390x844;
- sortie active de mise en page, FR, 390x844, marquée `APERÇU QA`;
- état réel non configuré, EN, 1600x900;
- signaux prêts de mise en page, EN, 1600x900, marqués `QA PREVIEW`.

Les états QA utilisent un contrôleur sans effet : aucune requête, réservation,
sortie ou récompense ne peut être produite. Le harnais refuse toute dimension
différente, ne recadre ni ne redimensionne les PNG et écrit un manifeste SHA-256.

Le harnais passe avec marqueur `LivingHive perimeter sortie proofs captured.`,
zéro erreur de dimension et fermeture propre dans
`Artifacts/LivingHivePerimeterSortie_Capture.log`. Les quatre PNG ont été
inspectés à résolution native : aucune collision, coupure ou cible hors panneau.
La première passe avait révélé le sous-titre portrait tronqué; une clé compacte
bilingue a été ajoutée, puis F8 et les quatre captures ont été rejoués avant
ratification. Le manifeste final est
`Docs/Product/Evidence/LivingHivePerimeterSortie/LivingHivePerimeterSortie_CaptureManifest.md`.

## Fichiers de la tranche visible

- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieCapture.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieCapture.cs.meta`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`

Les contrats client et serveur sont documentés dans
`LivingHive_HivePerimeterSortieMobileContractMilestone_2026-07-22.md` et
`LivingHive_HivePerimeterSortieServerMilestone_2026-07-22.md`.

Communication est resté gelé. Aucune scène, image LivingHive ou image de terrain
n'a été modifiée.

## Synchronisation et fondations protégées

La synchronisation normale finale tentée à `2026-07-22T18:09:35Z` a échoué
avant toute copie avec `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport
`.codex/vm-sync-last-report.txt` demeure daté du
`2026-07-22T02:57:51Z`, avec 0 conflit et 4 suppressions historiques en attente.
Le jalon reste sur `C:`; aucun accès direct à `Z:` ni remappage n'a été tenté.

- scène canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Les trois empreintes sont inchangées. Fin de tranche : Unity=0, dotnet=0,
testhost=0.
