# LivingHive — recrutement doctrinal officiel mobile

Date : 2026-07-23  
Statut : intégré et validé hors Unity; drapeaux fermés; aucune activation ni
promotion.

## Résultat produit

`Armée -> Préparer` possède maintenant une frontière officielle complète pour
la Caserne. Avec une session et une ruche autorisées, l'écran lit les vrais
effectifs Gardiennes/Voltigeuses/Lanceuses, les soldes miel/pollen, les trois
offres serveur et l'opération active. Il peut démarrer une formation puis
réclamer son lot uniquement à partir d'un état confirmé par le serveur.

Sans runtime officiel, l'expérience existante reste un aperçu local clairement
identifié. Elle ne devient jamais une preuve de compte, de solde ou d'effectif
serveur. Soldats et Éclaireuses demeurent des rôles historiques hors doctrine;
aucune conversion implicite n'est effectuée.

Offres autoritaires actuelles :

- 4 Gardiennes pour 680 miel et 180 pollen, 14 secondes;
- 6 Voltigeuses pour 420 miel et 260 pollen, 14 secondes;
- 8 Lanceuses pour 500 miel et 120 pollen, 14 secondes.

## Frontière appareil / serveur

### Reste sur l'appareil

- rendu responsive, navigation et localisation;
- brouillon de composition et menace, volatils;
- dernier GET validé dans un cache protégé, partitionné joueur/ruche;
- représentation monotone d'un compte à rebours reçu du serveur;
- commande start/claim préparée dans une outbox protégée avant transport;
- reprise explicite de la même commande après réponse ambiguë.

L'appareil ne débite aucune ressource, ne crée aucun effectif, ne termine aucune
opération et ne soumet jamais automatiquement une mutation après redémarrage,
retour réseau ou ambiguïté.

### Appartient au serveur

- identité joueur/ruche et autorisation;
- catalogue, familles, coûts, lots et durées;
- soldes miel/pollen;
- roster doctrinal, rôles historiques et révision;
- heure UTC et statut de l'opération active;
- débit atomique, claim, révisions et reçus idempotents.

Le contrat public est `phase4-combat-recruitment-v1` avec le catalogue
`phase4-combat-v1`. Les routes authentifiées sont :

- `GET /game/v1/hives/{hiveId}/combat/recruitment`;
- `POST /game/v1/hives/{hiveId}/combat/recruitment/start`;
- `POST /game/v1/hives/{hiveId}/combat/recruitment/{operationId}/claim`.

START et CLAIM renvoient `{ receipt, snapshot }`. Le DTO public n'expose ni
`payloadHash`, ni clé interne, ni justificatif. La projection capture une seule
heure UTC. CLAIM refuse un compte supérieur à `1_000_000_000`; au plus 128
reçus sont conservés avec éviction déterministe.

## Défenses mobiles

- La porte de session est vérifiée avant toute consultation de justificatif ou
  tout appel réseau.
- Les versions, identités, trois offres, UTC, révisions, soldes, comptes,
  opération et reçu sont validés strictement.
- Une lecture réseau peut retomber sur le dernier snapshot protégé de la même
  partition; ce mode reste en lecture seule.
- Une mutation réseau n'est jamais répétée automatiquement.
- Un rejet d'autorisation permet un seul renouvellement, puis rejoue exactement
  la même requête.
- Une commande ambiguë reste visible et ne peut être reprise que par une action
  explicite avec la clé originale.
- Un claim frais est refusé tant que le serveur expose encore une opération,
  même si le minuteur local paraît écoulé.
- Logout ou changement de joueur ferme le contrôleur, annule la durée de vie et
  purge cache/outbox de l'ancienne partition.

## Interface

Les états `Non configuré`, `Chargement`, `Prêt`, `Hors ligne — lecture seule`,
`Envoi`, `Confirmation à vérifier` et `Erreur` sont intégrés. L'écran distingue
explicitement roster officiel et aperçu local, affiche les soldes et offres
serveur et propose rafraîchissement, démarrage, vérification et réclamation
selon l'état autoritaire.

Vingt-quatre clés `formation_readiness.official.*` existent en français et en
anglais. Les catalogues comptent chacun 1262 entrées uniques et sont alignés.

## Preuves acquises sans lancer Unity

- contrat et transport mobile : 13/13 scénarios;
- présentation et outbox : 10/10 scénarios;
- assemblages produit/réseau : génération réussie, 0 erreur,
  122 avertissements historiques ou de harnais;
- assemblage Editor, tests et capture : génération réussie, 0 erreur,
  230 avertissements historiques;
- serveur cœur `CombatRecruitmentTests` : 4/4;
- routes HTTP `CombatRecruitmentEndpointTests` : 3/3;
- suite serveur `net10.0` : 341 réussis, 0 échec, 8 SQL ignorés;
- build Release serveur : 0 erreur, 1 avertissement
  `Microsoft.Data.SqlClient` préexistant;
- fin de validation : aucun processus Unity, dotnet ou testhost.

Journaux :

- `Artifacts/DoctrineRecruitmentClientFinal.log`;
- `Artifacts/DoctrineRecruitmentPresentationFinal.log`;
- `Artifacts/DoctrineRecruitmentUnityStaticBuild.log`;
- `Artifacts/DoctrineRecruitmentUnityEditorStaticBuild.log`.

Rapport serveur :
`Docs/ProductionIntegration/LivingHive_Phase4_CombatRecruitment_HTTP_2026-07-23.md`.

## Validation Unity réservée au prochain créneau

Unity n'a volontairement pas été lancé pendant cette tranche. Le harnais
`BeeKingdom.Playground.Editor.SandboxLivingHiveOfficialDoctrineRecruitmentCapture.CaptureAndExit`
est prêt à produire exactement deux preuves honnêtes `NotConfigured` :

- FR, 390x844;
- EN, 1600x900.

Sortie réservée :
`Docs/Product/Evidence/LivingHiveOfficialDoctrineRecruitment`.
Le harnais n'invente ni effectif, offre, opération, reçu ou badge.

Le prochain créneau doit :

1. ouvrir `Assets/Scenes/LivingHive.unity` et attendre la fin de l'import;
2. confirmer la compilation C# normale;
3. exécuter F8 et les tests Editor ciblés;
4. produire puis inspecter les deux captures;
5. vérifier manuellement `Armée -> Préparer`, d'abord sans session officielle;
6. répéter sur appareil Android avec Keystore, réseau interrompu/repris et
   session officielle de staging.

## Portes restantes

- raccorder une configuration staging réelle : BaseUrl, HiveId et session;
- valider Android physique, stockage protégé, reprise réseau et arrière-plan;
- valider TLS, SQL natif, multi-instance et deux comptes isolés;
- coordonner l'activation de `CombatRecruitment` et
  `CombatFormationReadiness`, tous deux faux par défaut et en Production;
- reconstruire un candidat, déployer en staging et exécuter les smokes;
- raccorder ensuite la réservation officielle de composition et les sorties,
  sans inventer de combat local.

Aucun candidat, drapeau, activation ou déploiement n'a été créé dans ce jalon.

## Synchronisation

La synchronisation finale tentée le `2026-07-23T08:30:22Z` a échoué avant toute
copie : accès refusé sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun conflit n'a
été écrasé, aucun remappage et aucun accès direct à `Z:` n'ont été tentés. Le
dernier rapport valide reste daté du `2026-07-22T02:57:51Z` avec 0 conflit,
0 copie VM vers l'hôte et 4 suppressions historiques en attente. La tranche
demeure sur la copie locale `C:`.

## Fichiers produit

- `Assets/BeeKingdom/Networking/HiveDoctrineRecruitmentClient.cs`;
- `Assets/BeeKingdom/Playground/HiveFormationReadinessPresentation.cs`;
- `Assets/BeeKingdom/Playground/HiveOfficialDoctrineRecruitmentPresentation.cs`;
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Tests/Editor/HiveDoctrineRecruitmentClientTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialDoctrineRecruitmentTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialDoctrineRecruitmentCapture.cs`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`.

Le chantier Communication, la scène LivingHive, le terrain canonique et l'image
de ruche n'ont pas été modifiés. Leurs empreintes de référence restent :

- terrain : `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image `background_hive.png` :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.
