# LivingHive — confort et performance sur mobile

## Statut

La tranche est implémentée et ratifiée dans Unity 6000.5.3f1. La compilation
jeu/éditeur, la suite F8, les deux formats visuels et la frontière serveur sont
validés sans toucher aux fondations protégées ni au chantier Communication.

## Résultat joueur

`Plus -> Confort mobile` ouvre un panneau bilingue et tactile dans LivingHive :

- `Mouvement réduit` fige les pulsations décoratives, supprime les déplacements
  rapides et révèle immédiatement les textes narratifs animés;
- `Mode économie` réduit les abeilles d’ambiance de 5 à 3 en portrait et de 8 à
  5 en paysage, puis désactive leurs traînées;
- les abeilles réellement affectées à une collecte, un soin, une construction,
  une formation ou une défense restent toutes visibles. Le réglage ne falsifie
  jamais l’effectif utile à la compréhension d’une tâche.

Le menu `Plus` fonctionne maintenant aussi en paysage. Il conserve les entrées
Communication et Recherche existantes, sans modifier leur contrat ou leurs
ancrages. Le panneau de réglages possède deux cibles de 76 px et une fermeture
de 44 px aux formats 390x844 et 1600x900.

## Persistance et frontière serveur

`MobileComfortPreferences` est un document appareil `v1` avec révision locale,
`reducedMotion` et `economyMode`. Il est enregistré dans `PlayerPrefs`, tolère
une absence, remet à zéro un JSON illisible ou une version inconnue et ne dépend
pas du profil joueur. Ces préférences décrivent le terminal lui-même; elles ne
contiennent ni secret, ni solde, ni progression.

| Responsabilité | Appareil | Serveur |
|---|---|---|
| Rendu, densité d’ambiance, mouvement | Autoritaire | Aucune |
| Préférence persistée | Document local versionné | Aucun contrat actuel |
| Ressources, files, timers, progression | Lecture seulement | Autorité inchangée |
| Synchronisation multiappareil future | Facultative et non autoritaire | Contrat distinct éventuel |

L’Intégrateur a ratifié qu’aucun endpoint, mutation, reçu d’idempotence,
candidat ou déploiement serveur n’est requis :
`Docs/ProductionIntegration/LivingHive_DevicePreferences_ServerBoundary_2026-07-22.md`.

## Validation

- compilation Unity jeu/éditeur : sortie 0, `0 error CS` et
  `0 Compilation failed` dans `Artifacts/LivingHiveMobileComfortCompile.log`;
- compilation C# de secours, incluant code, tests et harnais : `0 erreur`, 217
  avertissements historiques dans
  `Artifacts/LivingHiveMobileComfortFallbackBuild.log`;
- catalogues `fr-CA` et `en-US` : `769/769` clés uniques et alignées, dont 11
  nouvelles clés `settings.mobile.*`;
- cinq contrôles Unity ajoutés : codec/version, réparation corruption/version,
  persistance/reprise, budgets visuels sans perte d’abeilles affectées et
  disposition/localisation;
- suite F8 : 65 contrôles réussis, aucun échec ni erreur de compilation dans
  `Artifacts/LivingHiveMobileComfortF8.log`;
- harnais exécuté :
  `BeeKingdom.Playground.Editor.SandboxLivingHiveMobileComfortCapture.CaptureAndExit`,
  sortie propre et deux dimensions strictes dans
  `Artifacts/LivingHiveMobileComfortCapture.log`.
- fondations vérifiées après Unity : carte 50x50
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`,
  scène LivingHive
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`,
  image de base
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

## Preuves visuelles

Les deux images ont été inspectées à leur résolution native. Les titres,
descriptions, états, fermetures et divulgations sont lisibles sans collision;
`Plus` reste actif et la barre Communication paysage demeure `NotConfigured`
sans contenu fictif.

- `LivingHive_MobileComfort_FR_390x844.png`, `390x844`, mouvement réduit activé,
  SHA-256 `1ac5b611b92b8ea2f5182342dd9b12fcdca25168a285043ac8417442e28a52a7`;
- `LivingHive_MobileComfort_EN_1600x900.png`, `1600x900`, mode économie activé,
  SHA-256 `c5f988df797d78989cbf00305c73fe1a616823c0f2533bf547d7ddc4eae0b887`.

Manifeste :
`Docs/Product/Evidence/LivingHiveMobileComfort/LivingHiveMobileComfort_CaptureManifest.md`.

## Test manuel conseillé

1. Ouvrir `Assets/Scenes/LivingHive.unity`, passer en Play et entrer dans le
   profil de démonstration.
2. Toucher `Plus`, puis `Confort mobile`.
3. Activer `Mouvement réduit`: les pulsations doivent se figer sans déplacer les
   commandes ni masquer une abeille de tâche.
4. Activer `Mode économie`: l’ambiance doit être moins dense, tandis qu’une
   opération active conserve son nombre d’abeilles affectées.
5. Quitter puis relancer Play. Les deux choix doivent être restaurés sur cet
   appareil.
6. Revenir dans le panneau, désactiver les options et vérifier que les ressources
   du HUD, les productions en attente et les files n’ont pas changé.

La scène 50x50, `LivingHive.unity`, l’image de base de la ruche et tous les
fichiers Communication restent inchangés.

## Synchronisation VM

La commande officielle `tools/vm-sync/Synchroniser-BeeKingdom.cmd` a été
exécutée après la tranche. Elle a échoué avant toute copie avec `Accès refusé`
sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun contournement, remappage ou accès
direct à `Z:` n’a été tenté. Le dernier rapport valide demeure celui du
`2026-07-22T02:57:51Z`, avec 0 conflit bloqué et 4 suppressions historiques en
attente. Cette tranche est donc disponible uniquement dans la copie locale
`C:\projets\beekingdomgame-master` jusqu’à la prochaine synchronisation normale.
Une nouvelle tentative après la ratification F8/captures, le
`2026-07-22T09:05:33Z`, a rencontré exactement le même refus avant copie.
