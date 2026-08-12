# LivingHive — progression persistante de la ruche

## Résultat joueur

Les niveaux de bâtiments et les effectifs ne dépendent plus du dernier élément
conservé dans la file. Plusieurs améliorations et plusieurs formations peuvent
maintenant être terminées successivement, puis être toutes restaurées après un
redémarrage de la démo locale.

Le panneau `Armée` indique que ses effectifs sont sauvegardés sur l’appareil et
que l’autorité officielle reste sur le serveur. Le panneau d’un bâtiment affiche
la révision locale reconnue. Les boutons concernés font au moins 44 px dans les
formats mobile et paysage.

## Frontière mobile et serveur

| Responsabilité | Appareil mobile actuel | Serveur officiel futur |
|---|---|---|
| Rendu et interactions | Oui | Non |
| Niveaux/effectifs hors ligne | Cache local `v1`, borné à 32 bâtiments | Snapshot complet autoritaire |
| Partition | Identifiant de profil embarqué; profil incompatible refusé | Joueur, ruche, monde et serveur |
| Protection | `PlayerPrefs` non protégé, démo seulement | Stockage serveur protégé |
| Révisions | Révision locale de cache | Révisions bâtiment et armée séparées |
| Fusion | Monotone et idempotente pour les gains de la preview | Remplacement/réconciliation par snapshot autoritaire |
| Transactions, pertes et conflits | Jamais décidés hors ligne | Autorité serveur exclusive |

Le cache local conserve quatre effectifs (`workers`, `soldiers`, `guardians`,
`scouts`) et une liste déterministe de niveaux. Il rejette une version inconnue,
un autre profil et un JSON illisible; il borne les populations, déduplique les
bâtiments par niveau maximal et réécrit une forme saine. Le profil stratégique
nouvellement créé est désormais écrit immédiatement afin que sa partition reste
stable dès le premier redémarrage.

## Cohérence avec les files

Le journal des files reste responsable des opérations actives, de leur coût et
de leur échéance UTC. Le nouveau journal de progression est responsable du
résultat durable complet.

À la complétion, le résultat durable est écrit avant de marquer l’opération comme
réclamée. Si l’application s’arrête entre les deux écritures, la reprise applique
le même niveau ou le même effectif exact, sans double gain. Un ancien résultat de
file ne peut jamais diminuer un niveau ou un effectif déjà plus élevé. Les anciens
journaux complétés migrent donc naturellement lors de leur première lecture.

Cette politique monotone correspond aux mécaniques locales actuelles, qui
n’ajoutent que des niveaux et des unités. Les pertes, mutations concurrentes et
réconciliations ne seront jamais déduites par le téléphone; elles viendront d’un
snapshot serveur.

## Noyau serveur coordonné

L’Intégrateur a ajouté `HiveProgressionSnapshotFactory`. La projection contient
`PlayerId`, `HiveId`, `WorldId`, `GameServerId`, `BuildingRevision`,
`ArmyRevision`, `CatalogVersion`, tous les niveaux et tous les effectifs. Elle
rejette état nul, identités vides, révisions négatives, clés vides et valeurs
négatives, puis réalise des copies défensives.

`HiveProgressionSnapshot:Enabled=false` reste fermé par défaut et en Production.
Aucune route HTTP, activation, notification, candidat ou opération de déploiement
n’a été ajoutée. Le raccordement session/appartenance/transport et la preuve de
staging restent obligatoires.

Rapport serveur :
`Docs/ProductionIntegration/LivingHive_PersistentProgressionSnapshot_Core_2026-07-22.md`.

## Validation

- Compilation Unity jeu et éditeur : succès Tundra, `0 error CS`, `0 Compilation failed`.
- Suite F8 LivingHive : 54 contrôles, succès dans `Artifacts/LivingHiveHiveProgress_F8_Final.log`.
- Non-régression BEE-925–930 : succès dans `Artifacts/LivingHiveHiveProgress_BEE930.log`.
- Non-régression BEE-945–951 : succès dans `Artifacts/LivingHiveHiveProgress_BEE951.log`.
- Cas nouveaux : plusieurs bâtiments, quatre populations, deux redémarrages,
  migration de résultats de file, résultat ancien sans rollback, JSON corrompu,
  version inconnue, autre profil et borne de 32 bâtiments.
- Catalogues : `751/751` clés uniques et alignées en `fr-CA` et `en-US`.
- Serveur HiveOperations : `32/32`; build Release : `0 erreur`.
- Fin des validations : `Unity=0`, `dotnet=0`, `testhost=0`.

## Preuves visuelles

Les deux captures ont été générées depuis `Assets/Scenes/LivingHive.unity`, puis
inspectées à leur résolution native :

- `LivingHive_Progress_Army_FR_390x844.png`, `390x844`, SHA-256
  `aac548c7d97df230fd61ffa7feb52f1e8d384418519af96c756a535923d18ce2`;
- `LivingHive_Progress_Building_EN_1600x900.png`, `1600x900`, SHA-256
  `8a8b1e82c5f57274289e7d2a9ddd57d92d28f2d9dd418e1db097876b3e2145ab`.

Manifeste :
`Docs/Product/Evidence/LivingHiveProgress/LivingHiveProgress_CaptureManifest.md`.

Les fondations conservent exactement leurs empreintes de référence :

- scène canonique 50x50 : 7 776 octets, SHA-256
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène `LivingHive.unity` : 9 160 octets, SHA-256
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive : 7 489 785 octets, SHA-256
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Aucun fichier Communication n’a été touché.

## Synchronisation VM

La synchronisation normale de fin a échoué avant toute copie avec `Accès refusé`
sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun remappage, relâchement de bac à
sable ou accès direct à `Z:` n’a été tenté. Le dernier rapport valide demeure
daté de `2026-07-22T02:57:51Z`, avec `0` conflit et `4` suppressions historiques
en attente. Cette tranche reste donc sur la copie locale `C:` jusqu’à une
synchronisation lancée depuis la session utilisateur.

## Test manuel conseillé

1. Ouvrir `Assets/Scenes/LivingHive.unity`, passer en `Play/Game` et entrer dans
   la démo locale.
2. Sur l’introduction, cliquer une fois pour révéler le texte s’il défile encore,
   puis une seconde fois pour fermer le panneau.
3. Améliorer deux bâtiments différents en attendant chaque fin de file.
4. Ouvrir `Plus -> Armée`, former deux types d’unités successivement et noter les
   quatre effectifs.
5. Quitter puis relancer Play. Les deux niveaux et les deux gains d’effectifs
   doivent être restaurés ensemble; l’interface doit toujours préciser que le
   serveur reste l’autorité officielle.
