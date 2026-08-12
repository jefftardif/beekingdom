# LivingHive — source des prérequis Recherche

## Résultat joueur

Une recherche bloquée par les ressources n’affiche plus un motif générique. Chaque
étude indique maintenant la quantité exacte manquante : miel d’abord, puis pollen
si le miel est suffisant. Son action principale devient `Source` et ouvre le
bâtiment qui permet au joueur de corriger lui-même la pénurie :

- miel vers la `Réserve de miel` (`honey_storage`);
- pollen vers l’`Entrepôt de pollen` (`warehouse_cells`).

La navigation ne lance pas la recherche, ne collecte pas la production du
bâtiment et ne valide aucune tâche de la ronde quotidienne. Le bouton conserve
une cible tactile minimale de 44 px. Dès que les deux ressources sont suffisantes,
le motif disparaît et l’action normale de lancement revient.

## Expérience mobile

- Portrait 390x844, français : `Miel manquant : 140` et `Miel manquant : 80`
  restent lisibles; la réserve de miel est la destination mise en évidence.
- Paysage 1600x900, anglais : `Missing pollen: 70` et `Missing pollen: 100`
  restent lisibles; l’entrepôt est la destination mise en évidence.
- Les deux études restent visibles simultanément; la ruche et le rail principal
  ne sont pas masqués.
- Les catalogues `fr-CA` et `en-US` comptent **740/740** clés uniques et alignées.

## Frontière appareil / serveur

### Appareil

L’appareil calcule uniquement un message et une destination de navigation depuis
le dernier solde reconnu. Dans la démonstration locale, ce solde vient de
l’aperçu persistant. Dans le produit raccordé, il viendra du dernier instantané
serveur reconnu et restera un cache d’affichage protégé, borné et partitionné par
joueur. Le bouton `Source` ne produit aucune commande économique : il change
seulement le panneau et le bâtiment ciblé.

### Serveur

Le serveur demeure propriétaire des soldes miel/pollen, du catalogue et des
coûts de recherche, de l’éligibilité, de la révision, de la transaction atomique,
de l’opération active et de sa complétion. Le noyau `LivingHiveResearch` déjà
livré reste fermé par `LivingHiveResearch:Enabled=false`; aucune nouvelle route,
mutation, notification, candidate de déploiement ou vérité serveur n’était
nécessaire pour cette tranche de navigation. Au raccordement, une réponse de
lancement refusée devra remplacer le cache local par les soldes et la révision
autoritaires avant de recalculer la pénurie.

## Validation

- `Assembly-CSharp-Editor.csproj` : **0 avertissement, 0 erreur** lors de la
  vérification finale silencieuse.
- F8 global Unity : sortie 0, marqueur
  `LivingHive manual collection checks passed.`, zéro `error CS`, journal
  `Artifacts/LivingHiveResearchSource_F8.log`.
- Le test couvre le déficit exact miel puis pollen, les deux destinations, le
  retour à l’état lançable et l’absence de recherche, collecte ou tâche
  quotidienne implicite.
- Capture Unity : sortie 0, marqueur `LivingHive Research proofs captured.`,
  journal `Artifacts/LivingHiveResearchSource_Capture.log`.
- Portrait :
  `LivingHive_ResearchSource_Honey_FR_390x844.png`, SHA-256
  `4932A0FCFD700F6B2F6AF502AA2A185595717C68796A45EBC79895DE65F7D951`.
- Paysage :
  `LivingHive_ResearchSource_Pollen_EN_1600x900.png`, SHA-256
  `CEF0AA29F9C225ED48EC86256330C94B81893DAE9BBC177B3077FA73F4E04411`.
- Manifeste :
  `Docs/Product/Evidence/LivingHiveResearch/LivingHiveResearch_CaptureManifest.md`.
- Fin de validation : Unity, dotnet et testhost à zéro.
- La synchronisation finale normale s’est arrêtée avant toute copie sur
  `Accès refusé` à `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun accès direct à `Z:`,
  remappage ou élargissement de droits n’a été tenté. Le dernier rapport lisible
  demeure daté de `2026-07-22T02:57:51Z`, avec 0 conflit et 4 suppressions
  historiques en attente.

La compilation globale inclut les deux fichiers propriétaires Communication
laissés à une frontière syntaxique sûre avant le gel. L’Architecte ne les a pas
modifiés. Après levée du gel, Communication a ratifié son pont de cycle de session
avec **145/145 réussis**, zéro échec, erreur ou test non exécuté; TRX
`LivingHiveChatSessionBridgeFinal145.trx`. Cette preuve reste attribuée au chantier
Communication et n’ajoute aucun appel de production fictif au shell mobile.

## Fondations protégées

- Scène canonique 50x50 : 7 776 octets, SHA-256
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Image LivingHive : 7 489 785 octets, SHA-256
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.
- Scène `LivingHive.unity` : 9 160 octets, SHA-256
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Aucun terrain, tuile, image de carte, image de ruche ou scène n’a été modifié.

## Fichiers client exacts

- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveResearchCapture.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`
- preuves et manifeste sous `Docs/Product/Evidence/LivingHiveResearch`

## Portes suivantes

- raccorder le shell de session mobile, l’adaptateur HTTP et le dernier instantané
  serveur reconnu;
- protéger et partitionner le cache par joueur;
- réconcilier révision et soldes après refus ou reprise réseau;
- tester changement de compte, hors-ligne, solde périmé et double toucher;
- conserver la collecte manuelle dans le bâtiment, jamais dans `Source`.
