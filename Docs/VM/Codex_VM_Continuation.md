# Reprise Codex dans la VM - Bee Kingdom

## Jalon courant — réservation officielle d’escouade mobile

Le 23 juillet, `Armée -> Préparer` a reçu le raccord commit/release officiel de
la composition Gardiennes/Voltigeuses/Lanceuses. Le mobile lit le snapshot
partitionné, prépare toute mutation dans l’outbox protégée avant transport et
ne répète jamais automatiquement une commande incertaine. `Vérifier la
commande` est le seul geste de reprise et réemploie exactement route, charge,
révision et clé d’idempotence, même après reconstruction du contrôleur.

Appareil : rendu, brouillon volatil, cache GET protégé et commande préparée
chiffrée. Serveur : identité, ruche, roster, disponible, réservé, capacité,
identifiant de réservation, UTC, révisions, transaction et reçus publics
bornés à 128. Une réservation ne consomme aucun effectif et ne lance aucun
combat. Le panneau Sorties ne réserve plus directement : il renvoie à la
composition protégée.

Preuves hors Unity : client 24/24; présentation 10/10; assemblages jeu/éditeur
0 erreur; cœur serveur 3/3; HTTP 5/5; suite net10 346 réussis, 0 échec,
8 SQL ignorés; build Release 0 erreur. Catalogues : 1279/1279 clés uniques et
alignées, dont 17 `formation_readiness.reservation.*` par langue. Rapport :
`Docs/Product/LivingHive_OfficialSquadReservationMobileMilestone_2026-07-23.md`.

Prochain test utilisateur : ouvrir `Assets/Scenes/LivingHive.unity`, laisser la
compilation se terminer, lancer Play, fermer l’introduction puis ouvrir
`Armée -> Préparer`. Sans session officielle, vérifier l’absence de faux
snapshot, réservation, reçu ou succès. Exécuter ensuite F8, les tests Editor et
inspecter portrait 390x844 et paysage 1600x900. Unity n’a pas été lancé pendant
cette tranche.

`CombatSquadReservation:Enabled=false` reste fermé par défaut et en Production.
Android physique/Keystore, BaseUrl/HiveId staging, TLS, SQL natif,
multi-instance, activation, candidat et déploiement restent ouverts. Les
mutations Phase 5 `launch`, `claim` et `recall` n’ont pas encore l’outbox
protégée officielle. Communication reste gelé; terrain, scènes et image de
ruche sont inchangés.

La synchronisation finale tentée le `2026-07-23T09:14:57Z` a échoué avant
toute copie avec accès refusé à `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun
contournement ni accès direct à `Z:` n’a été tenté. Le dernier rapport valide
reste daté du `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers l’hôte et
4 suppressions historiques en attente. La tranche demeure sur `C:`.

## Jalon courant — recrutement doctrinal officiel mobile

Le 23 juillet, `Armée -> Préparer` a reçu le raccord officiel complet de la
Caserne. Avec session autorisée, le mobile lit les effectifs
Gardiennes/Voltigeuses/Lanceuses, soldes, offres et opération du serveur. Il
prépare START/CLAIM dans une outbox protégée avant transport et ne débite,
ne crédite, ne termine ni ne répète automatiquement aucune mutation.

L'appareil possède rendu, brouillon volatil, cache GET protégé, compte à rebours
monotone et reprise explicite de la même commande. Le serveur possède identité,
catalogue, coûts/lots/durées, soldes, roster, UTC, opération, révisions et reçus
bornés à 128. Un claim ne peut jamais dépasser un compte de 1 000 000 000.
Soldats et Éclaireuses restent hors doctrine, sans conversion implicite.

Preuves hors Unity : client 13/13; présentation 10/10; assemblages produit,
réseau et Editor à 0 erreur; cœur serveur 4/4; HTTP 3/3; suite net10
341 réussis, 0 échec, 8 SQL ignorés; build Release 0 erreur. Catalogues :
1262/1262 clés uniques, dont 24 officielles. Rapport :
`Docs/Product/LivingHive_OfficialDoctrineRecruitmentMobileMilestone_2026-07-23.md`.

Prochain test utilisateur : ouvrir `Assets/Scenes/LivingHive.unity`, attendre la
fin de compilation, lancer Play, fermer l'introduction puis ouvrir
`Armée -> Préparer`. Sans session officielle, vérifier la séparation explicite
avec l'aperçu local et l'absence de faux roster/solde/opération. Exécuter ensuite
F8, les tests Editor et les captures 390x844/1600x900. Unity n'a pas été lancé
cette nuit.

`CombatRecruitment:Enabled=false` et
`CombatFormationReadiness:Enabled=false` restent fermés par défaut et en
Production. Android physique/Keystore, BaseUrl/HiveId staging, réseau réel, TLS,
SQL natif, multi-instance, activation, candidat et déploiement restent ouverts.
Communication reste gelé. Terrain canonique, scène LivingHive et image de ruche
gardent leurs empreintes.

La synchronisation finale tentée le `2026-07-23T08:30:22Z` a échoué avant toute
copie avec accès refusé sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun
contournement ni accès direct à `Z:` n'a été tenté. Le dernier rapport valide
reste daté du `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers l'hôte et
4 suppressions historiques en attente. Toute la tranche demeure sur `C:`.

## Jalon courant — soin officiel du couvain mobile

Le 23 juillet, le détail de la Nurserie a reçu le contrat officiel unique
`living-hive-brood-vitality-v1`. Nourrir coûte 300 miel, dure 12 secondes et
ajoute 22 nutrition; stabiliser coûte 45 cire, dure 13 secondes et ajoute
7 stabilité. Le téléphone ne possède que le rendu, le cache protégé, le
minuteur monotone et l'outbox Keystore. Le serveur possède ressources, vitalité,
UTC, opération, révisions et reçus.

Une commande est protégée avant transport, jamais soumise automatiquement et
jamais appliquée localement. Un retry explicite réemploie exactement la même
clé. Les reçus `BroodCareReceipts`, bornés à 128, rejouent START et COMPLETE
sans second débit, gain ou changement de révision. Un démarrage frais marque la
Ronde seulement si son drapeau est actif.

Preuves hors Unity : mobile 23/23; cœur 9/9; HiveOperations 69/69; HTTP 4/4;
serveur net10 338 réussis, 0 échec, 8 SQL ignorés; build serveur 0 erreur;
catalogues 1238/1238 avec 39 clés de soin. Les quatre assemblages mobiles
concernés ont compilé sans erreur pendant la passe d'intégration. Rapport :
`Docs/Product/LivingHive_OfficialBroodCareMobileMilestone_2026-07-23.md`.

Prochain test utilisateur : ouvrir `Assets/Scenes/LivingHive.unity`, attendre la
fin de compilation, lancer Play, fermer l'introduction puis ouvrir la Nurserie.
Le parcours local doit rester clairement un aperçu; le parcours officiel exige
une session/HiveId et des drapeaux staging. Exécuter ensuite F8, les tests Editor
et inspecter 390x844/1600x900. Unity n'a pas été lancé cette nuit.

Communication reste gelé. Terrain canonique, scène LivingHive et image de ruche
gardent leurs empreintes. Android physique/Keystore, réseau réel, SQL natif,
TLS staging, activation, candidat et déploiement restent ouverts.

La synchronisation finale tentée le `2026-07-23T07:24:35Z` a échoué avant toute
copie avec accès refusé sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun
contournement ni accès direct à `Z:` n'a été tenté. Le dernier rapport valide
reste daté du `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers l'hôte et
4 suppressions historiques en attente. Toute la tranche demeure sur `C:`.

## Jalon courant — Ronde quotidienne officielle mobile

Le 23 juillet, `Quêtes -> Ronde` a reçu son contrat mobile officiel
`living-hive-daily-round-v1`. Le téléphone rend le panneau, protège le dernier
snapshot et prépare une commande idempotente; il ne crée aucun fait, crédit,
jour ou révision. La commande en attente survit à une interruption mais n'est
jamais soumise automatiquement. Le logout/changement de joueur la purge.

Le serveur marque atomiquement `collection_received`, `operation_launched` et
`snapshot_read` sur les routes officielles Production, Amélioration/Recherche et
Stocks. La récompense est exactement 120 miel + 60 pollen, une seule fois. Les
reçus dédiés sont bornés, migrés et rejoués exactement après reconstruction.
Une anomalie préexistante de replay Production a été corrigée : le reçu est
maintenant consulté avant tout accrual et la commande fraîche n'ajoute qu'une
révision globale.

Preuves hors Unity : boîte 8/8, client 10/10, présentation 9/9; quatre
assemblages à 0 erreur; HiveOperations 60/60; serveur complet net10 Release
336 réussis, 0 échec, 8 SQL ignorés; build serveur 0 erreur; catalogues
1199/1199 avec 25 clés Ronde. `HiveDailyRound:Enabled=false` par défaut et en
Production. Rapport :
`Docs/Product/LivingHive_OfficialDailyRoundMobileMilestone_2026-07-23.md`.

Prochain test utilisateur : ouvrir `Assets/Scenes/LivingHive.unity`, lancer Play,
terminer/fermer l'introduction, puis `Quêtes -> Ronde`. Sans session officielle,
vérifier l'état `Non configuré`, l'absence de fausses tâches/récompenses et le
bouton de rafraîchissement. Exécuter ensuite F8 et les deux captures du harnais
officiel. Android physique, TLS staging, SQL natif, activation, candidat et
déploiement restent ouverts.

Communication reste entièrement gelé; ne modifier aucun fichier chat pendant la
validation de cette tranche.

La synchronisation de fin du jalon, tentée le `2026-07-23T06:09:26Z`, a échoué
avant toute copie avec `Accès refusé` sur le partage hôte. Le dernier rapport
valide reste daté du `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers
l'hôte et 4 suppressions historiques en attente. Aucun accès direct à `Z:` ni
contournement n'a été tenté; le jalon demeure sur `C:`.

## Jalon courant — débrief de retour de sortie

Le 22 juillet, la réclamation Phase 5 a reçu un `claimReceipt` durable. Le
serveur expose les crédits réellement appliqués, les soldes/capacités après
opération, les identités joueur/ruche/sortie/signal/cycle, la révision et
`ServerTimeUtc`. Le plafonnement est calculé par ressource; même clé et même
commande rejouent la même preuve après reconstruction sans second crédit.

Le mobile refuse un reçu étranger, détaché, incomplet ou dont un crédit réduit
n'est pas expliqué par la capacité. Le panneau affiche le résultat exact et ne
conserve le reçu qu'en mémoire pendant la consultation. `Continuer` est une
fermeture locale, pas une mutation. Le contrôleur de production par défaut reste
`NotConfigured` tant que session et transport officiels ne sont pas injectés.

Preuves finales : serveur 266 réussis + 7 SQL ignorés; harnais mobile 21/21;
assemblages jeu et éditeur à 0 erreur; F8 final vert dans
`Artifacts/LivingHiveReturnDebrief_FinalF8.log`; six captures exactes dans
`Docs/Product/Evidence/LivingHivePerimeterSortie`. Une première série a été
rejetée pour chevauchement portrait, corrigée puis entièrement rejouée. Les
catalogues comptent 1022/1022 clés uniques. Rapport :
`Docs/Product/LivingHive_HivePerimeterReturnDebriefMilestone_2026-07-22.md`.

Communication reste entièrement gelé. Ne modifier aucun fichier chat pendant la
ratification de cette tranche.

Une nouvelle synchronisation finale a été tentée à `2026-07-22T18:56:44Z` et a
échoué avant toute copie avec `Accès refusé` sur le partage hôte. Le dernier rapport valide
reste celui du `2026-07-22T02:57:51Z` : 0 conflit et 4 suppressions historiques
en attente. Aucun accès direct à `Z:` ni remappage n'a été tenté.

## Jalon historique — aperçu local de recrutement doctrinal

Le jalon du 22 juillet a créé la file locale de démonstration et le roster
mobile v2 sans convertir Soldats ou Éclaireuses. Ses preuves F8 et visuelles
restent valides pour l'aperçu local. Le raccord officiel mobile du 23 juillet,
décrit au début de ce document, remplace cependant ses anciennes limites
« aucun raccordement » et « roster non enregistré ». Rapport historique :
`Docs/Product/LivingHive_DoctrineRecruitmentMilestone_2026-07-22.md`.

## Jalon courant — prévision de production mobile

Le 22 juillet, `Sac & stocks` a reçu une prévision lisible des trois productions
manuelles. Chaque ligne montre pending/capacité, taux horaire et temps avant
saturation; le panneau annonce aussi le premier stock qui sera plein. `Voir`
ouvre le bâtiment exact sans collecter, créditer le HUD ou compléter une tâche.

Le téléphone calcule seulement l’affichage. En production, UTC, catalogue,
taux, capacités et pending appartiennent au snapshot serveur existant. Aucun
nouveau contrat n’est requis, mais `HiveOfflineProduction:Enabled=false` reste
fermé et le pending durable par bâtiment demeure une porte explicite.

Unity 6000.5.3f1 compile sans erreur; F8 passe 70 contrôles; la compilation de
secours passe avec 0 erreur et 217 avertissements historiques. Les catalogues
comptent 780/780 clés. Les preuves exactes et inspectées 390x844 FR et 1600x900
EN sont sous `Docs/Product/Evidence/LivingHiveProductionForecast`. Rapport :
`Docs/Product/LivingHive_ProductionForecastMilestone_2026-07-22.md`.

La synchronisation officielle suivant ce jalon a échoué avant toute copie avec
`Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`; la tentative finale sur
l’état ratifié date de `2026-07-22T09:28:54Z`.
Aucun accès direct à `Z:` n’a été tenté. Le dernier rapport valide reste daté du
`2026-07-22T02:57:51Z` avec 0 conflit et 4 suppressions historiques en attente;
la tranche demeure donc uniquement sur la copie locale `C:`.

## Jalon courant — confort et performance sur mobile

Le 22 juillet, `Plus -> Confort mobile` a reçu deux réglages persistants propres
au terminal. `Mouvement réduit` fige les animations décoratives et les textes
progressifs; `Mode économie` réduit les abeilles d’ambiance de 5/8 à 3/5 et
supprime leurs traînées. Les abeilles affectées à une vraie tâche restent toutes
visibles. Aucune ressource, file, progression ou autorité serveur n’est modifiée.

Le document `MobileComfortPreferences v1` est versionné dans `PlayerPrefs`,
répare corruption/version inconnue et reste indépendant du joueur. L’Intégrateur
a ratifié qu’aucun contrat serveur actuel n’est requis. Les catalogues passent à
769/769 clés. La compilation C# jeu/éditeur incluant code, tests et harnais passe
avec 0 erreur dans `Artifacts/LivingHiveMobileComfortFallbackBuild.log`.

Unity 6000.5.3f1 compile avec 0 erreur C# dans
`Artifacts/LivingHiveMobileComfortCompile.log`; F8 passe 65 contrôles dans
`Artifacts/LivingHiveMobileComfortF8.log`. Les preuves exactes et inspectées
390x844 FR et 1600x900 EN sont sous
`Docs/Product/Evidence/LivingHiveMobileComfort`. Rapport :
`Docs/Product/LivingHive_MobileComfortMilestone_2026-07-22.md`.

La synchronisation officielle suivant ce jalon a échoué avant toute copie avec
`Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun accès direct à `Z:`
n’a été tenté; le rapport `.codex` reste daté du 2026-07-22T02:57:51Z (0 conflit,
4 suppressions en attente). Le jalon existe donc uniquement dans la copie locale
`C:` jusqu’à la prochaine synchronisation normale. La tentative finale suivant
F8 et les captures, à `2026-07-22T09:05:33Z`, a rencontré le même refus avant
toute copie.

## Jalon courant — production manuelle après une absence

Le 22 juillet, LivingHive a reçu un journal local `v1` de production manuelle.
Il conserve miel/cire/pollen encore dans les bâtiments lors d’une fermeture,
d’une perte de focus ou d’un passage en arrière-plan, puis calcule une reprise
bornée au retour. Le bilan `PRODUCTION À TON RETOUR` n’accorde rien : `Voir`
ouvre seulement le bâtiment et le joueur doit toucher l’icône de production.

Le cache est partitionné par profil, borné à 16 entrées et 12 heures, tolère la
corruption et refuse de produire lors d’un recul d’horloge. Il est écrit toutes
les 30 secondes et sur les événements de cycle de vie. `PlayerPrefs` reste non
protégé et non officiel. L’Intégrateur a livré un snapshot serveur read-only
fermé par `HiveOfflineProduction:Enabled=false`; l’horloge, le catalogue, les
taux, capacités, pending et révisions officiels appartiennent au serveur. Le
modèle durable serveur par bâtiment et le raccordement session/HTTP restent
ouverts; aucune production officielle n’est simulée. Le noyau serveur borné à
64 bâtiments passe 35/35 tests et son build Release reste sans erreur.

Unity compile avec 0 `error CS`; F8 passe 60 contrôles dans
`Artifacts/LivingHiveOfflineProductionF8Final.log`; les catalogues comptent
758/758 clés. Les deux captures exactes et inspectées sont sous
`Docs/Product/Evidence/LivingHiveOfflineProduction`. Les empreintes de la scène
50x50, de `LivingHive.unity` et de l’image de ruche sont inchangées. Aucun fichier
Communication n’a été touché.

La synchronisation finale officielle a été tentée, mais le partage
`\\DESKTOP-D3D29K7\BeeKingdomHost` a refusé l’accès avant toute copie. Aucun
contournement de `Z:` n’a été utilisé. Le dernier rapport valide reste celui du
`2026-07-22T02:57:51Z`, avec 0 conflit et 4 suppressions historiques en attente;
le jalon est donc présent et validé dans la copie locale `C:` seulement.

Prochain test utilisateur : ouvrir `Assets/Scenes/LivingHive.unity`, fermer
l’introduction, noter les pending, quitter Play, attendre, relancer, vérifier le
bilan sans variation du HUD, cliquer `Voir`, puis récolter manuellement l’icône
du bâtiment. Rapport :
`Docs/Product/LivingHive_OfflineProductionMilestone_2026-07-22.md`.

## Jalon courant — progression persistante de la ruche

Le 22 juillet, le défaut de reprise multi-opérations a été corrigé. Le journal
de file ne conserve honnêtement que la dernière opération de chaque type; un
nouveau journal local `LocalPreviewHiveProgress` conserve désormais ensemble
plusieurs niveaux de bâtiments et les effectifs ouvrières/soldats/gardiennes/
éclaireuses. Il est versionné, borné à 32 bâtiments, associé au profil, tolérant
à la corruption et fusionne les résultats complétés de façon monotone et
idempotente. `PlayerPrefs` reste non protégé : il s’agit d’un cache de démo, pas
d’une autorité de production.

L’interface Armée et les détails de bâtiment montrent la frontière : sauvegarde
sur l’appareil, progression officielle côté serveur. Les actions touchées font
44 px. Unity compile avec 0 `error CS`; F8 passe 54 contrôles dans
`Artifacts/LivingHiveHiveProgress_F8_Final.log`; BEE-925–930 et BEE-945–951
passent aussi. Les catalogues comptent 751/751 clés. Deux captures exactes et
inspectées sont sous `Docs/Product/Evidence/LivingHiveProgress`.

L’Intégrateur a livré et renforcé `HiveProgressionSnapshotFactory` : partition
joueur/ruche/monde/serveur, révisions bâtiment/armée, catalogue et copies
complètes défensives. État nul, identité vide, révision/valeur négative et clé
vide sont rejetés. `HiveProgressionSnapshot:Enabled=false` reste fermé, sans
route ni candidat; HiveOperations passe 32/32 et le build Release 0 erreur.
Rapports : `Docs/Product/LivingHive_PersistentHiveProgressMilestone_2026-07-22.md`
et `Docs/ProductionIntegration/LivingHive_PersistentProgressionSnapshot_Core_2026-07-22.md`.

Prochain test utilisateur : ouvrir `Assets/Scenes/LivingHive.unity`, entrer dans
la démo, fermer l’introduction en deux clics si le texte défile, terminer deux
améliorations de bâtiments différents et deux formations différentes, puis
quitter/reprendre Play. Les quatre résultats doivent rester visibles ensemble.
Le chat reste hors périmètre de cette tranche.

La synchronisation normale de fin a échoué avant toute copie avec `Accès refusé`
sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun contournement n’a été tenté. Le
rapport `.codex` demeure daté de `2026-07-22T02:57:51Z`, avec 0 conflit et 4
suppressions historiques en attente; la tranche reste uniquement sur `C:`.

## Jalon courant — Recherche LivingHive

Le 22 juillet, la troisième file LivingHive a été rendue réelle avec deux études complémentaires : `foraging_routes_i` (240 miel, 90 pollen, 16 s, +2 % miel local) et `tempered_combs_i` (180 miel, 120 pollen, 16 s, +5 % capacité cire locale). Une seule étude est active à la fois; le journal local migre en v2, reprend l’opération et empêche toute double complétion. Le portrait passe par `Plus -> Recherche`; le paysage affiche la vraie progression et deux abeilles au nœud. Les catalogues comptent 683/683 clés alignées. F8 passe avec sortie 0 et zéro `error CS` dans `Artifacts/LivingHiveResearch_F8.log`; trois captures exactes et leur manifeste sont sous `Docs/Product/Evidence/LivingHiveResearch`.

L’Intégrateur a livré le noyau serveur autoritaire (2/2 ciblés, 26/26 HiveOperations, build sans erreur), fermé par `LivingHiveResearch:Enabled=false`, sans route, candidat ni déploiement. Rapport client : `Docs/Product/LivingHive_ResearchVerticalSliceMilestone_2026-07-22.md`; rapport serveur : `Docs/ProductionIntegration/LivingHive_ResearchQueue_ServerCore_2026-07-22.md`.

La première compilation globale a exposé deux erreurs dans la modification Communication laissée à une frontière sûre. Communication a corrigé uniquement `LivingHiveChatBootstrap.cs` en remplaçant la dépendance d’assemblage directe par `IChatAccountSessionReadiness`, puis s’est regelé. La compilation finale est verte. Ne pas modifier les fichiers Communication pendant la prochaine tranche sans nouveau signal explicite.

Prochain test utilisateur : ouvrir `Assets/Scenes/LivingHive.unity`, entrer dans la démo locale, fermer l’introduction, ouvrir `Plus -> Recherche`, lancer `Danse des routes I`, vérifier débit/file/abeilles, quitter Play avant la fin et confirmer la reprise au lancement suivant. Terminer ensuite les deux études et contrôler la troisième file en paysage. Le mode reste explicitement local et non officiel jusqu’au raccordement session/HTTP/révision serveur.

## Jalon courant — registre Sac & stocks

Après la Recherche, le bouton `Sac` a été rendu fonctionnel. Il sépare stock disponible, production encore à collecter manuellement et coûts engagés dans construction/formation/recherche; `Voir` ouvre le bâtiment sans collecter. Les cibles font 44 px et les preuves 390x844/1600x900 sont sous `Docs/Product/Evidence/LivingHiveLedger`. F8 passe avec sortie 0 et zéro `error CS` dans `Artifacts/LivingHiveLedger_F8.log`; les catalogues comptent 697/697 clés alignées.

L’Intégrateur a livré `HiveStockSnapshotFactory`, fermé par `HiveStockSnapshot:Enabled=false`: révision, ressources/capacités, recherches terminées et engagements sont autoritaires; population/capacité population restent `null` faute d’agrégat fiable. Validation serveur : 1/1 ciblé, 27/27 HiveOperations et build Release 0 erreur. Aucun HTTP, candidat ou déploiement. Rapports : `Docs/Product/LivingHive_HiveLedgerMilestone_2026-07-22.md` et `Docs/ProductionIntegration/LivingHive_HiveStockSnapshot_Core_2026-07-22.md`.

Prochain test utilisateur : dans `Assets/Scenes/LivingHive.unity`, entrer dans la démo, fermer l’introduction, ouvrir `Sac`, comparer disponible/en attente/engagé, puis utiliser chaque bouton `Voir`. Le bon bâtiment doit s’ouvrir et la ressource doit rester en attente jusqu’au clic manuel sur son icône.

## Jalon courant — Ronde quotidienne LivingHive

Après le registre, `Quêtes` contient maintenant l’onglet `Ronde`. Trois gestes réels sont reconnus sans attente artificielle : collecte manuelle depuis un bâtiment, lancement d’une opération construction/formation/recherche, puis `Sac -> Voir` vers un bâtiment. À 3/3, une réclamation explicite crédite 120 miel et 60 pollen dans la preview locale, une seule fois par jour local; aucun crédit automatique.

Chaque ligne incomplète possède un raccourci `Aller` de 44 px vers la destination exacte, sans validation implicite. Seul Quêtes reçoit `!` quand la récompense est réellement prête; les badges Mail/Alliance/Plus simulés ont été retirés. L’appareil conserve uniquement un journal de démonstration `v1`, non officiel. L’Intégrateur a livré le noyau `HiveDailyRound` autoritaire : jour UTC serveur, preuves collectée/lancée/snapshot vérifiées, révision, capacités et claim atomique idempotent. Une opération ancienne, inexistante ou collectée ne compte pas comme lancement du jour. `HiveDailyRound:Enabled=false` reste fermé par défaut et en Production; aucun HTTP, candidat ou déploiement. Validation : F8 global sortie 0 et zéro `error CS`, HiveOperations 28/28, build serveur 0 erreur, catalogues 726/726. Captures exactes sous `Docs/Product/Evidence/LivingHiveDailyRound`; rapports client et serveur : `Docs/Product/LivingHive_DailyRoundMilestone_2026-07-22.md` et `Docs/ProductionIntegration/LivingHive_HiveDailyRound_Core_2026-07-22.md`.

Prochain test utilisateur : ouvrir `Assets/Scenes/LivingHive.unity`, entrer dans la démo locale, fermer l’introduction, collecter une ressource prête, lancer une vraie opération, ouvrir `Sac` puis `Voir`, et revenir à `Quêtes -> Ronde`. Vérifier `3 / 3`, réclamer une fois, puis confirmer que la seconde réclamation est refusée. Le mode reste local/non officiel jusqu’au raccordement session, HTTP, snapshot, outbox protégée et réconciliation serveur.

## Jalon courant — source des prérequis Recherche

Le 22 juillet, une recherche bloquée affiche désormais le déficit exact de miel
ou pollen. Son bouton `Source` de 44 px ouvre uniquement la réserve de miel ou
l’entrepôt de pollen; il ne lance pas l’étude, ne collecte pas et ne valide pas la
ronde quotidienne. L’action de lancement revient dès que les soldes suffisent.

L’appareil rend ce guidage depuis le dernier solde reconnu. Le serveur reste
propriétaire des soldes, coûts, catalogue, éligibilité, révision, transaction et
opération; aucun nouveau serveur n’est requis pour une simple navigation et
`LivingHiveResearch:Enabled=false` demeure fermé. Les catalogues comptent 740/740
clés alignées. Compilation Editor 0/0; F8 global vert dans
`Artifacts/LivingHiveResearchSource_F8.log`; deux preuves inspectées sous
`Docs/Product/Evidence/LivingHiveResearch`. Rapport :
`Docs/Product/LivingHive_ResearchPrerequisiteSourceMilestone_2026-07-22.md`.

Communication est resté gelé pendant la tranche. Ses deux fichiers propriétaires
déjà modifiés avant le gel compilent dans la passe globale. Après la levée du gel,
Communication a ratifié son pont de cycle de session : 145/145 réussis, zéro
échec/erreur/non exécuté, TRX `LivingHiveChatSessionBridgeFinal145.trx` et aucun
processus résiduel. Le shell officiel reste une porte honnêtement ouverte; aucun
appel de production n’est inventé.

La synchronisation normale suivant ce jalon a encore été refusée avant toute
copie sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun contournement n’a été tenté;
`.codex/vm-sync-last-report.txt` demeure daté de `2026-07-22T02:57:51Z`, avec
0 conflit et 4 suppressions historiques en attente. Les changements récents
restent donc seulement dans la copie locale `C:` jusqu’à la synchronisation
depuis la session utilisateur.

Prochain test utilisateur : ouvrir `Assets/Scenes/LivingHive.unity`, entrer dans
la démo locale, fermer l’introduction, ouvrir `Plus -> Recherche` et vérifier
qu’une pénurie affiche le nombre exact. `Source` doit ouvrir le bon bâtiment sans
collecter; revenir ensuite à Recherche et confirmer qu’aucune étude n’a démarré.

## Instruction de reprise

Ce document permet a Codex de reprendre Bee Kingdom dans la VM sans demander a
l'utilisateur de repeter le contexte. Au debut d'une nouvelle tache dans la VM:

1. verifier que le nom de l'ordinateur correspond a la VM;
2. travailler dans `C:\projets\beekingdomgame-master`, jamais directement dans `Z:`;
3. lire les documents de reference ci-dessous;
4. synchroniser avant et apres une serie de modifications;
5. reprendre le plan LivingHive sans toucher aux fondations protegees.

Langue de travail: francais. L'agent principal de la VM s'appelle `Architecte` et
agit comme architecte et coordinateur senior.
Lire le code et les rapports locaux avant de poser une question. Ne jamais demander
a l'utilisateur de reexpliquer l'historique du projet.

## Fondations protegees

- La carte terrain 50x50 et toutes ses images sont intouchables.
- Scene canonique:
  `Assets\Scenes\WorldMapWave6Wave5Method12288Preview.unity`
- Package terrain verrouille:
  `Assets\BeeKingdom\Playground\Resources\WorldMapWave6Runtime\UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview`
- L'image de base actuelle de la ruche LivingHive est conservee.
- Les ameliorations portent sur le runtime, les interfaces, les interactions,
  l'animation, la progression, le tutoriel et les services.
- Le chantier chat et messagerie est repris par l'agent `Communication` dans un
  perimetre isole. Il ne branche rien dans LivingHive sans un handoff valide avec
  l'agent `Architecte`.

## Vision produit active

- Ant Legion est une reference fonctionnelle, pas un modele a copier.
- Tout est transpose au monde des abeilles avec une qualite visuelle et une
  ergonomie superieures.
- La ruche doit etre vivante: abeilles animees, bannieres et transitions animees,
  retours d'action immediats.
- La collecte des ressources reste manuelle. Une future automatisation peut etre
  vendue comme confort, sans produire un avantage militaire impossible a rattraper.
- Bee Kingdom comporte des achats attractifs, mais ne devient pas pay-to-win.
- Le choix de classe sera explicite. Une seconde classe pourra etre debloquee plus
  tard, autour du niveau 100. Les relations de forces doivent rester equilibrees.
- Le jeu sera localise. Les textes d'interface, de tutoriel et de contenu doivent
  provenir de ressources de localisation.
- Les introductions de chapitre utilisent une grande page narrative illustree,
  un texte progressif, un fondu d'entree et de sortie, puis une future voix hors
  champ et des effets sonores.
- Les chapitres 1 a 5 sont encore trop courts pour l'Acte I final. Les allonger par
  des objectifs actifs et des consequences, jamais par des attentes artificielles.

## Etat fonctionnel important

- `LivingHive` est la scene de travail principale.
- Sept chapitres guides existent, avec transition Carte/Ruche et premier vol de
  butinage local.
- Le blocage du chapitre 4 et son lanceur d'apercu ont ete corriges.
- Une introduction plein ecran du chapitre 4 explique miel, cire et pollen.
- Le menu Quetes expose la continuite de l'Acte I.
- Les files d'attente doivent refleter les vraies operations; aucune ligne statique
  ou associee au mauvais batiment n'est acceptable.
- Les panneaux de batiment sont contextuels, compacts, fermables et se ferment en
  cliquant hors d'un batiment.
- Aucun pictogramme permanent ne doit masquer l'illustration d'un batiment; son
  contour indique la selection.

## Documents a lire en premier

1. `Docs\Product\BeeKingdom_LivingHive_ExecutionPlan.md`
2. `Docs\Benchmarks\AntLegion\AntLegion_BeeKingdom_FunctionalReference.md`
3. `Docs\Demos\LivingHive.md`
4. `Docs\Product\BeeKingdom_Localization.md`
5. `Docs\Audio\TutorialChapterNarration_Suno.md`
6. `Docs\VM\BeeKingdom_VM_Workstation_Setup.md`
7. `Docs\WorldMapCommunication\ChatMessaging_CommunicationRelaunch_Status_2026-07-16.md`

Le dernier document de chat est la base de reprise de l'agent `Communication`. Son
etat doit etre reconcilie avec les tests et les endpoints actuels avant toute
affirmation de disponibilite production.

## Environnement VM

- Copie locale de travail: `C:\projets\beekingdomgame-master`
- Partage de l'ordinateur principal mappe dans la VM: `Z:`
- Partage reseau: `\\DESKTOP-D3D29K7\BeeKingdomHost`
- Unity requis: `6000.5.3f1`, revision `c2eb47b3a2a9`
- Modules requis: Android Build Support, Android SDK/NDK et OpenJDK
- Ne jamais ouvrir le projet Unity directement depuis `Z:`.
- Ne pas utiliser Git dans la VM.

Avant et apres le travail, lancer:

`C:\projets\beekingdomgame-master\tools\vm-sync\Synchroniser-BeeKingdom.cmd`

## Derniere tranche LivingHive terminee - 2026-07-20

Les files locales d'amelioration et de formation survivent maintenant au redemarrage grace a un journal PlayerPrefs versionne. Une operation conserve son identifiant unique, sa cible, son cout et sa date de fin UTC. Le resultat final est idempotent: il est revendique dans le journal avant application et restaure a une valeur exacte apres un nouveau chargement. La suite complete LivingHive F8 passe dans `Temp/QueuePersistenceFinalF8.log`. Prochaine priorite: rendre la reprise visible et comprehensible dans l'interface sans la presenter comme sauvegarde officielle.

Cette priorite est maintenant livree: une carte de reprise localisee et fermable montre les operations restaurees avec leur cible et leur temps restant, ainsi que la limite `Sauvegarde locale · non officielle`. La suite F8 passe dans `Temp/QueueResumeUxFinalF8.log`; les preuves portrait et paysage sont `Artifacts/GuidedBroodIncubation/QueueResume_390x844.png` et `QueueResume_1600x900.png`. Prochaine priorite: persister et restaurer l'etape du tutoriel guide avec une politique de reprise explicite, sans accrediter silencieusement une recompense.

La reprise du tutoriel est maintenant transactionnelle. Le dernier objectif est memorise pour l'explication, tandis que la restauration revient au debut du chapitre avec ses valeurs economiques exactes et ses jalons precedents. Aucun cout, gain ou reward n'est applique pendant la restauration. Une banniere localisee explique cette reprise sure. F8 passe dans `Temp/TutorialCheckpointFinalF8.log`; preuves: `Artifacts/GuidedBroodIncubation/TutorialResume_390x844.png` et `TutorialResume_1600x900.png`. Prochaine priorite: profil local versionne pour les choix structurants inter-chapitres.

Le profil strategique local est passe en version 2. Il migre automatiquement la version 1, conserve aussi la charte d'ouverture, puis restaure six decisions durables: charte, developpement et doctrine du couvain, affectation ouvriere, specialisation et doctrine d'atelier. Les effets structurels sont appliques par delta et les certifications operationnelles restent separees, ce qui rend les redemarrages repetes idempotents. Le panneau joueur est accessible en paysage et par l'en-tete de ruche en portrait. La suite F8 passe dans `Temp/StrategicProfileCharterFinalF8.log`; preuves inspectees: `Artifacts/GuidedBroodIncubation/StrategicProfile_390x844.png` et `StrategicProfile_1600x900.png`. Prochaine priorite: choisir la prochaine extension active a partir du deficit de rythme mesure, sans ajouter d'attente artificielle.

Le chapitre 1 possede maintenant une mise en service post-charte. Le joueur choisit une charge de convoi, recolte manuellement 170 ou 210 miel, valide debit, etancheite et relais, puis signe un sceau durable: +1 % miel ou +2 stabilite. Le profil strategique migre en v3 et conserve ce septieme choix. Le chapitre passe a 12 objectifs, 20 interactions actives et 138-170 secondes chronometrees. F8: `Temp/OpeningCommissioningFinalF8.log`. Preuves inspectees: `Artifacts/GuidedOpeningInstallation/Chapter1_LoadValidation_390x844.png`, `Chapter1_CommissioningSeal_390x844.png`, `Chapter1_LoadValidation_1600x900.png`, `Chapter1_CommissioningSeal_1600x900.png`. Prochaine priorite de rythme: chapitre 4, nouveau plancher a 16 interactions actives.

Le chapitre 4 se termine maintenant par une certification sous contrainte apres ses deux commandes. Le joueur choisit un cycle thermique de 18 secondes avec trois batisseuses ou une epreuve de charge de 16 secondes avec deux batisseuses, puis valide une seule fois les joints, l'equilibre et la marge de securite. Le profil strategique migre en v4 et conserve ce huitieme choix: +1 % de production de cire ou +5 % de capacite. Le chapitre passe a 11 objectifs, 20 interactions actives et 130-148 secondes; l'Acte I atteint 685-834 secondes. F8: `Temp/WorkshopCertificationF8.log`. Preuves inspectees: `Artifacts/GuidedOpeningInstallation/Chapter4_CertificationChoice_390x844.png`, `Chapter4_CertificationChecks_390x844.png`, `Chapter4_CertificationChoice_1600x900.png`, `Chapter4_CertificationChecks_1600x900.png`. Prochaine priorite de rythme: chapitre 3, nouveau plancher a 18 interactions actives.

Le chapitre 3 transmet maintenant son ouvriere certifiee a l'atelier par un relais economique actif. Apres les deux rondes, le joueur choisit une logistique miel de 16 secondes produisant 160 miel et une remise reelle de 120 miel sur la premiere amelioration de l'atelier, ou un convoi de cire de 18 secondes produisant 100 cire et une remise reelle de 80 cire. La ressource reste dans le batiment jusqu'a sa collecte manuelle, puis trois controles uniques valident la route, l'inventaire et le signal de releve avant la recompense. Le profil strategique migre en v5 et conserve ce neuvieme choix. Le chapitre passe a 12 objectifs, 23 interactions actives et 130-152 secondes; l'Acte I atteint 701-852 secondes sur 58 objectifs. F8: `Temp/WorkerWorkshopHandoffFinalF8.log`. Preuves inspectees: `Artifacts/GuidedOpeningInstallation/Chapter3_WorkshopHandoffChoice_390x844.png`, `Chapter3_WorkshopHandoffChecks_390x844.png`, `Chapter3_WorkshopHandoffChoice_1600x900.png`, `Chapter3_WorkshopHandoffChecks_1600x900.png`. Prochaine priorite de rythme: chapitre 5, nouveau plancher a 19 interactions actives.

Le chapitre 5 se termine maintenant par un mandat durable vers la premiere expedition. Le corridor eclaireur mobilise trois eclaireuses pendant 14 secondes et ajoute 6 pollen au butin reel du chapitre 7; l'escorte mobilise deux gardiennes pendant 16 secondes, ajoute 4 pollen et 2 securite. Le cap, le signal de rappel et la releve doivent etre controles une seule fois avant la recompense. Le profil strategique migre en v6, restaure le mandat par delta et conserve la reclamation manuelle du pollen. Le chapitre passe a 12 objectifs, 23 interactions actives et 151-190 secondes; l'Acte I atteint 715-868 secondes sur 60 objectifs. F8: `Temp/DefenseExpeditionMandateFinalF8.log`. Preuves inspectees: `Artifacts/GuidedOpeningInstallation/Chapter5_ExpeditionMandateChoice_390x844.png`, `Chapter5_ExpeditionMandateChecks_390x844.png`, `Chapter5_ExpeditionMandateChoice_1600x900.png`, `Chapter5_ExpeditionMandateChecks_1600x900.png`. Prochaine priorite de rythme: chapitre 4, plancher partage a 20 interactions actives.

Le chapitre 4 se termine maintenant par une livraison defensive issue de l'atelier. Les boucliers de cire mobilisent trois batisseuses pendant 15 secondes, produisent 90 cire a collecter manuellement et reduisent de 60 cire le premier barrage du chapitre 5. La grille ventilee mobilise deux batisseuses pendant 17 secondes, produit 70 cire a collecter et ajoute immediatement 4 securite. Fixations, aeration et passage des gardiennes sont trois controles uniques requis avant la recompense. Le profil strategique migre en v7 et restaure ces effets par delta. Le chapitre atteint 13 objectifs, 25 interactions actives et 145-165 secondes; l'Acte I atteint 730-885 secondes sur 62 objectifs. F8: `Temp/WorkshopDefenseHandoffFinalF8.log`. Captures inspectees: `Artifacts/GuidedOpeningInstallation/Chapter4_DefenseHandoffChoice_390x844.png`, `Chapter4_DefenseHandoffChecks_390x844.png`, `Chapter4_DefenseHandoffChoice_1600x900.png`, `Chapter4_DefenseHandoffChecks_1600x900.png`. Prochaine priorite numerique: chapitre 1, 20 interactions actives, en conservant son role d'ouverture courte.

Le chapitre 1 transmet maintenant un ravitaillement actif au couvain avant sa recompense. La reserve de gelee royale mobilise deux nourrices pendant 14 secondes, produit 60 pollen a collecter manuellement et ajoute durablement +2 aux soins du couvain. L'escorte thermique mobilise trois ouvrieres pendant 12 secondes, produit 120 miel a collecter et ajoute durablement +3 stabilite. Fraicheur, quantite et passage vers la nurserie sont trois controles uniques. Le profil strategique migre en v8 et restaure ces effets par delta. Le chapitre atteint 14 objectifs, 25 interactions actives et 150-184 secondes; l'Acte I atteint 742-899 secondes sur 64 objectifs. F8: `Temp/OpeningBroodSupplyFinalF8.log`. Captures inspectees: `Artifacts/GuidedOpeningInstallation/Chapter1_BroodSupplyChoice_390x844.png`, `Chapter1_BroodSupplyChecks_390x844.png`, `Chapter1_BroodSupplyChoice_1600x900.png`, `Chapter1_BroodSupplyChecks_1600x900.png`. Prochaine priorite: chapitre 2, premier des planchers partages a 23 interactions actives.

Le chapitre 2 prepare maintenant materiellement l'emergence de la premiere ouvriere. La ration d'emergence mobilise trois nourrices pendant 14 secondes, produit 100 miel a collecter manuellement et reduit durablement de 60 miel le soin final du chapitre 3. L'opercule renforce mobilise deux batisseuses pendant 16 secondes, produit 60 cire a collecter et reduit durablement de 40 cire son renforcement, soit 30 cire au lieu de 70. Opercule, ration et signal d'emergence sont trois controles uniques. Le profil strategique migre en v9 et restaure les remises. Le chapitre atteint 15 objectifs, 28 interactions actives et 180-224 secondes; l'Acte I atteint 756-915 secondes sur 66 objectifs. F8: `Temp/BroodWorkerHandoffFinalF8.log`. Captures inspectees: `Artifacts/GuidedOpeningInstallation/Chapter2_WorkerHandoffChoice_390x844.png`, `Chapter2_WorkerHandoffChecks_390x844.png`, `Chapter2_WorkerHandoffChoice_1600x900.png`, `Chapter2_WorkerHandoffChecks_1600x900.png`. Prochaine priorite: chapitre 3, premier plancher restant a 23 interactions actives.

Le chapitre 3 certifie maintenant une passation technique vers l'atelier. Le gabarit de calibration mobilise deux ouvrieres pendant 15 secondes et ajoute 40 cire au lot temoin du chapitre 4; la trousse d'application mobilise trois ouvrieres pendant 18 secondes et reduit de 40 cire le premier chantier. Gabarit, outils et releve sont trois controles uniques. Le profil strategique migre en v10. Le chapitre atteint 13 objectifs, 27 interactions actives et 145-170 secondes. F8: `Artifacts/WorkerWorkshopCommissionFinalF8.log`. Captures inspectees dans `Artifacts/GuidedOpeningInstallation`: `Chapter3_WorkshopCommissionChoice_390x844.png`, `Chapter3_WorkshopCommissionChecks_390x844.png`, `Chapter3_WorkshopCommissionChoice_1600x900.png` et `Chapter3_WorkshopCommissionChecks_1600x900.png`.

Le chapitre 5 exige maintenant un briefing de sortie apres le mandat. La balise solaire mobilise trois eclaireuses pendant 15 secondes et transmet le repere C32_32 au chapitre 6; le retour garde mobilise deux gardiennes pendant 18 secondes et ajoute 3 securite. Deux decisions de simulation non punitives enseignent le recalage du cap et la conservation du groupe. Le profil strategique migre en v11. Le chapitre atteint 13 objectifs, 26 interactions actives et 166-208 secondes; l'Acte I atteint 68 objectifs et 786-951 secondes. Les preuves sont incluses dans `Artifacts/GuidedOpeningInstallation`.

La doctrine finale du chapitre 4 n'est plus appliquee automatiquement a la fin du minuteur. Le joueur doit valider separement le gabarit, la releve et la tracabilite; un doublon ne compte pas et aucun bonus ni choix persistant n'est inscrit avant le troisieme controle unique. Aucun temps d'attente n'est ajoute. Le chapitre conserve 13 objectifs et 145-165 secondes, mais atteint 28 interactions actives et 15 controles structurels. Compilation Unity globale et suite LivingHive: `Artifacts/WorkshopDoctrineValidationFinalF8.log`. Rapport regenere: `Artifacts/OpeningActPacing/OpeningActObjectivePacingDoctrineGeneration.log`. Le manifeste contient 42 captures sur 42; les nouvelles preuves inspectees sont `Chapter4_OperationsDoctrineChecks_390x844.png` et `Chapter4_OperationsDoctrineChecks_1600x900.png`. Les catalogues `fr-CA` et `en-US` contiennent chacun 419 cles uniques et alignees. Prochaine priorite: chapitre 1, seul plancher restant a 25 interactions actives.

La charte du chapitre 1 exige maintenant trois controles actifs apres sa preparation: registre, priorite de la nurserie et signal d'alerte. Un doublon ne compte pas et aucun gain durable ni choix de profil n'est inscrit avant le troisieme controle unique. Aucun temps d'attente n'est ajoute. Le chapitre conserve 14 objectifs et 150-184 secondes, mais atteint 28 interactions actives et 12 controles. Compilation Unity globale et suite LivingHive: `Artifacts/OpeningCharterRatificationFinalF8.log`. Rapport regenere: `Artifacts/OpeningActPacing/OpeningActObjectivePacingCharterGeneration.log`. Le manifeste contient 44 captures sur 44; les nouvelles preuves inspectees sont `Chapter1_CharterRatification_390x844.png` et `Chapter1_CharterRatification_1600x900.png`. Les catalogues `fr-CA` et `en-US` contiennent chacun 436 cles uniques et alignees. Prochaine priorite: chapitre 5, seul plancher restant a 26 interactions actives.

Le chapitre 5 impose maintenant un debrief tactique apres la premiere defense. Le joueur controle la breche, le passage du couvain et la ligne de ravitaillement; les doublons sont ignores. Interception recommande ensuite la releve et Barrage recommande le nettoyage, sans verrouiller l'autre choix. Aucun temps, cout ou gain artificiel n'est ajoute. Le chapitre conserve 13 objectifs et 166-208 secondes, mais atteint 29 interactions actives et 12 controles. Compilation Unity globale et suite LivingHive: `Artifacts/DefenseTacticalDebriefFinalF8.log`. Rapport regenere: `Artifacts/OpeningActPacing/OpeningActObjectivePacingDebriefGeneration.log`. Le manifeste contient 46 captures sur 46; les nouvelles preuves inspectees sont `Chapter5_TacticalDebrief_390x844.png` et `Chapter5_TacticalDebrief_1600x900.png`. Les catalogues `fr-CA` et `en-US` contiennent chacun 444 cles uniques et alignees. Prochaine priorite: chapitre 3, seul plancher restant a 27 interactions actives.

Le chapitre 3 exige maintenant trois observations actives apres l'arrivee de la premiere ouvriere: ailes, corbeilles a pollen et signal de releve. Les doublons sont ignores. L'operculation renforcee recommande l'essai a la reserve et l'emergence naturelle recommande la nurserie, sans verrouiller l'autre essai. Aucun temps, cout, gain ni choix persistant artificiel n'est ajoute. Le chapitre conserve 13 objectifs et 145-170 secondes, mais atteint 30 interactions actives et 15 controles. Compilation Unity globale et suite LivingHive sous Unity 6000.5.3f1: `Artifacts/WorkerOrientationFinalF8.log`. Rapport regenere: `Artifacts/OpeningActPacing/OpeningActObjectivePacingOrientationGeneration.log`. Le manifeste contient 48 captures sur 48, toutes aux dimensions attendues; les nouvelles preuves inspectees a resolution native sont `Chapter3_WorkerOrientation_390x844.png` et `Chapter3_WorkerOrientation_1600x900.png`. Les catalogues `fr-CA` et `en-US` contiennent chacun 452 cles uniques et alignees. Les deux anciennes sessions Unity sans fenetre ont ete fermees avant la validation; aucun incident `dotnet.exe` n'a recidive. Prochaine priorite: chapitre 1, premier des planchers partages a 28 interactions actives avec les chapitres 2 et 4.

Le chapitre 1 conclut maintenant son installation par une dotation fondatrice: 250 miel en reserve, ou 170 miel et 80 pollen en fondation mixte. Les deux choix valent 250 ressources, sont engages une seule fois et n'ajoutent ni minuterie ni avantage payant. Le profil local migre en v12, conserve `opening_reward` et migre v1-v11 sans dotation implicite. Le chapitre conserve 14 objectifs et 150-184 secondes, mais atteint 29 interactions actives et 11 decisions. Compilation Unity globale et suite LivingHive: `Artifacts/FoundationRewardFinalF8.log`, avec 0 `error CS` et marqueur de succes. Rapport regenere: `Artifacts/OpeningActPacing/OpeningActObjectivePacingRewardGeneration.log`. Le manifeste contient 50 captures sur 50, toutes aux dimensions attendues. La premiere inspection portrait a revele une ligne masquee; la hauteur du panneau mobile a ete corrigee, F8 et les 50 captures ont ete rejoues, puis `Chapter1_FoundationReward_390x844.png` et `Chapter1_FoundationReward_1600x900.png` ont ete ratifiees a resolution native. Les catalogues `fr-CA` et `en-US` contiennent chacun 466 cles uniques et alignees. La scene canonique reste a 7 776 octets, datee du 17 juillet 2026 17:11:05; aucun processus Unity ou `dotnet` ne reste actif.

Regle d'architecture ajoutee a la demande de l'utilisateur: chaque tranche distingue appareil, cache local et serveur. Pour la dotation, le mobile ne doit conserver que rendu, cache et cle d'idempotence; le serveur doit valider l'eligibilite, verrouiller le choix, attribuer atomiquement la recompense et renvoyer les soldes autoritaires. Le mandat a ete transmis au task `Integrateur de production` (`019f8535-c857-7bb3-bf04-034e12567644`) dans son perimetre `Server/`. Le gel Communication est leve apres la ratification; le task `Communication` (`019f855a-7f5a-70e2-a104-e633cd421a43`) a recu le mandat explicite d'integrer le chat au jeu avec interface mobile/paysage, transport, reprise, recus, traduction et persistance sous une frontiere client/serveur documentee. Prochaine tranche LivingHive: chapitre 2, premier des planchers partages avec le chapitre 4 a 28 interactions actives. Ne pas synchroniser tant que les deux conflits existants du rapport vm-sync ne sont pas resolus.

Etat du 21 juillet apres coordination: le contrat serveur de dotation est durci sous un drapeau ferme, avec modele v4, migration DurableJson, enveloppes `game.*`, tests HiveOperations 14/14 et endpoint 2/2. Aucune transition serveur autoritaire ne marque encore `InstallationComplete`; `FoundingFoundation:Enabled=false` et `DeploymentAuthorized=false` demeurent obligatoires. Le candidat local courant `BeeKingdom.Server.20260721T215236Z` contient 55 fichiers, passe son manifeste et son smoke Healthy. La suite serveur totale compte 253 reussites et 7 tests SQL differes. Le candidat precedent `212438Z` est revoque; aucun transfert ou deploiement n'a ete effectue.

Le noyau de certification tutorielle du chapitre 1 est ensuite passe au modele v5. Trois observations sont acceptees dans un ordre monotone et idempotent, avec preuve finale opaque; elles ne modifient aucune ressource et laissent toujours `InstallationComplete=false`, car elles ne prouvent ni economie ni operations temporisees. La migration neutralise tout ancien drapeau vrai provenant d'un modele inferieur a v5. Validations: HiveOperations 16/16, suite serveur 253 reussites et 7 SQL ignores, build Release 0 erreur et 2 avertissements SqlClient preexistants. Le rapport est `Docs/ProductionIntegration/Chapter1_TutorialCertification_ServerCore_2026-07-21.md`. Aucun nouveau candidat n'a ete construit: `BeeKingdom.Server.20260721T215236Z` precede donc ce noyau et reste strictement non autorise au deploiement.

La tranche verticale Chat est implantee mais reste gelee jusqu'a ratification globale. Preuves ciblees: 131/131 Communication, 18/18 DTO serveur, 477 cles de localisation par catalogue et aucune donnee fictive. Le mobile conserve rendu, brouillon, cache recent persistant/protege/borne et outbox protegee; le serveur conserve identite, appartenances, ordre, historique, moderation, recus, traduction et curseurs. Aucun shell auth mobile de production n'appelle encore le bootstrap, donc l'UI affiche `NotConfigured`.

Deux tentatives Unity 6000.5.3f1 ont ete arretees le 21 juillet avant compilation: le canal de licence `LicenseClient-tardi` refuse la connexion puis signale `com.unity.editor.headless not found`. Aucun F8 ni capture Chat ne doit etre declare acquis. Apres retablissement Unity Hub/licence, relancer `SandboxLivingHiveManualCollectionTests.RunAllForBatch`, puis `LivingHiveChatCaptureHarness.CaptureAndExit` et inspecter les PNG 390x844 et 1600x900. Les instances Unity et dotnet lancees pendant le diagnostic ont ete nettoyees; la scene canonique reste a 7 776 octets, datee du 17 juillet 2026 17:11:05.

La licence a ensuite ete retablie le 21 juillet. Le test de disposition Chat a ete deplace dans l'assemblage Editor compatible, puis la compilation globale et F8 ont reussi avec sortie 0 et 0 `error CS` dans `Artifacts/LivingHiveChatFinalF8.log`. Les trois tests cibles passent 3/3 dans `Docs/WorldMapCommunication/Evidence/LivingHiveChat/LivingHiveChat_LayoutTests.xml`. Le harnais a refuse honnêtement un portrait 390x823 puis une selection 3840x2160 avant correction de la taille fixe et du groupe Game View actif. Les preuves finales exactes 390x844 et 1600x900, sans faux contenu, sont dans `Docs/WorldMapCommunication/Evidence/LivingHiveChat`; leur manifeste est valide. La scene canonique reste a 7 776 octets et son horodatage est inchange; aucun processus Unity ou dotnet ne reste actif. Le gel global Communication est leve, mais toute modification future du presentateur LivingHive partage exige une nouvelle coordination.

Le cache recent du chat est ensuite devenu persistant, versionne, protege, partitionne et borne. Il conserve la conversation selectionnee parmi au plus 100 entrees et uniquement ses messages confirmes. Une quarantaine rotative courante/precedente utilise un staging relu avant toute suppression de source; deux corruptions successives et l'echec de verification sans perte sont couverts. Le harnais NUnit autonome passe 138/138, TRX coherent. La compilation Unity ciblee contient 0 `error CS`, mais le Test Runner s'est bloque dans PerformanceTesting sans XML: cette tentative reste non ratifiee et les PID Unity main/worker ont ete nettoyes uniquement apres coordination. Rapport: `Docs/WorldMapCommunication/LivingHiveChat_ProtectedRecentCache_2026-07-21.md`. Fin: Unity, dotnet et testhost a zero.

La tranche suivante rend la nurserie vivante pendant le chapitre 2 sans toucher l'image de base. Sept cellules larvaires pulsent selon la plus faible des valeurs nutrition/stabilite; une fiche compacte classe l'etat, affiche les deux mesures et le temps restant du soin, tandis qu'un flux discret rejoint la chambre pendant une operation. Le mouvement reduit fige la composition. Le telephone rend seulement le dernier instantane reconnu, conserve au plus un cache borne par joueur et n'accepte aucune mutation hors ligne; le serveur possede nutrition, stabilite, revision, date UTC et operation active. La preview actuelle est marquee locale non officielle. `Artifacts/BroodVitalityFinalF8.log` contient 0 `error CS` et le marqueur de succes LivingHive. `Artifacts/GuidedBroodIncubation` contient 20 captures sur 20; les deux preuves vitalite exactes 390x844 et 1600x900 ont ete inspectees avec l'observation figee a 42 % dans le harnais seulement. Les catalogues comptent 484 cles chacun, sans doublon. Rapport: `Docs/Product/LivingHive_BroodVitalityFeedbackMilestone_2026-07-21.md`. La scene canonique reste a 7 776 octets, horodatee `2026-07-17 17:11:05`. La lecture serveur v6 derriere `BroodVitality:Enabled=false` est ratifiee: HiveOperations 20/20, HTTP 2/2, suite serveur 255 reussis plus 7 SQL ignores et build Release 0 erreur. Le runtime natif .NET 8 x64, SQL, le shell mobile, l'adaptateur HTTP, le cache protege et un nouveau candidat restent des portes; aucun endpoint de mutation, transfert ou deploiement n'a ete ouvert.

La vitalite devient ensuite une decision active du chapitre 2. Apres chacune des deux observations, le joueur identifie la plus faible de nutrition ou stabilite avant d'acceder aux trois controles. Une erreur est sans cout, sans temps perdu et sans progression; une bonne lecture recommande gelee royale pour la nutrition ou rotation hygienique pour la stabilite, tout en conservant les deux soins. Le chapitre passe de 28 a 30 interactions actives sans changer ses 15 objectifs ni ses 180-224 secondes; le chapitre 4 devient seul plancher a 28. La preview mobile ne modifie aucune valeur et expose `local_preview_non_official`; le serveur reste proprietaire de la vitalite et devra persister progression tutorielle/revision. Unity 6000.5.3f1 compile sans `error CS` et la suite LivingHive passe dans `Artifacts/BroodVitalityInterpretationFinalF8.log`. Le harnais produit 22 captures sur 22 aux dimensions exactes; `Chapter2_VitalityAssessment_390x844.png` et `Chapter2_VitalityAssessment_1600x900.png` ont ete inspectees. Les catalogues comptent 500 cles chacun, sans doublon. Rapport: `Docs/Product/LivingHive_BroodVitalityInterpretationMilestone_2026-07-21.md`. La scene canonique et l'image de base gardent leurs hashes; Unity, dotnet, testhost et bee_backend sont a zero. Ne pas relancer Unity pendant le test manuel de l'utilisateur.

Apres fermeture du test manuel, le blocage global des clics a ete corrige: sur une Game View basse, le rail IMGUI recevait le toucher avant le bouton narratif superpose. Le HUD, les menus et les rails sont maintenant desactives sous toute etape guidee, avec exception stricte pour `WorldOpenMap` et `ForageOpenMap`. Le chapitre 4 demande ensuite de qualifier le lot temoin: chaleur pour Rendement, tenue sous charge pour Stockage. Une erreur ne coute rien et ne progresse pas; une reussite unique ouvre les deux applications avec une recommandation informative. Le chapitre passe a 29 interactions actives sans nouvelle attente. F8 global: `Artifacts/WorkshopBatchQualificationFinalF8.log`, 0 `error CS`. Campagne: 52/52 captures exactes; preuves inspectees `Chapter4_BatchQualification_390x844.png` et `Chapter4_BatchQualification_1600x900.png`. Catalogues: 509 cles alignees. Validateur exact-crop: 2500 tuiles/importeurs, 4900 voisinages, zero incoherence; scene, manifeste et image LivingHive conservent leurs SHA. Rapport: `Docs/Product/LivingHive_WorkshopBatchQualificationMilestone_2026-07-21.md`. Le noyau serveur de qualification passe 24/24 tests HiveOperations et le build Release sans erreur; route et drapeau restent fermes, les tests HTTP restent a rejouer sous .NET 8 natif et aucun candidat n'a ete construit. Rapport serveur: `Docs/ProductionIntegration/Chapter4_WorkshopBatchQualificationServerImplementation_2026-07-21.md`. Aucun module Communication n'a ete modifie. Prochain test utilisateur: ouvrir `Assets/Scenes/LivingHive.unity`, Play/Game, puis cliquer le bouton central de l'introduction (un clic revele le texte s'il defile encore, le suivant ferme l'ecran).

Pendant la nuit du 21 au 22 juillet, la surface du chapitre 1 a été localisée de la première collecte jusqu'au ravitaillement du couvain, avec cadre tutoriel commun. 71 entrées bilingues portent les catalogues à 580/580 clés uniques, sans doublon, asymétrie ou référence manquante dans le périmètre. Un test couvre les clés représentatives, la bascule et les formats dynamiques; le harnais visuel impose explicitement la langue de chaque capture. Les assemblages jeu et éditeur compilent avec 0 erreur via `Assembly-CSharp-Editor.csproj`; les 217 avertissements sont historiques. Deux tentatives Unity internes au bac à sable ont d'abord été refusées par `LicenseClient-tardi` et n'ont pas été comptées. La relance hors bac à sable, dans la session du Hub, passe ensuite F8 et la suite LivingHive avec sortie 0 dans `Artifacts/Chapter1LocalizationF8.log`. La campagne `Artifacts/Chapter1LocalizationCapture.log` produit 54/54 PNG: 27 en 390x844, 27 en 1600x900, zéro dimension incorrecte. Les deux preuves anglaises ont été inspectées sans coupure ni collision; la dotation française régénérée reste en français. Aucun éditeur Unity, dotnet ou testhost ne reste actif; Unity Hub et son client de licence restent ouverts. Rapport: `Docs/Product/LivingHive_Chapter1FirstSessionLocalizationMilestone_2026-07-22.md`. Le terrain, l'image LivingHive, `Server/` et Communication sont inchangés. Prochain test utilisateur: ouvrir `Assets/Scenes/LivingHive.unity`, entrer en Play/Game et vérifier que le bouton central de l'introduction répond avant de poursuivre la première récolte.

Les conflits et suppressions eventuels sont inscrits dans:

`C:\projets\beekingdomgame-master\.codex\vm-sync-last-report.txt`

Les suppressions ne sont jamais propagees automatiquement.

Le rapport courant du 22 juillet 2026 a 02:57:51 UTC indique 0 conflit et 4
suppressions en attente. La tentative de synchronisation suivant la tranche de
qualification a echoue en lecture sur `\\DESKTOP-D3D29K7\BeeKingdomHost` a cause
du bac a sable; aucun contournement, elargissement de droits ou acces direct a
`Z:` n'a ete utilise. Relancer `Synchroniser-BeeKingdom.cmd` depuis la session
utilisateur, puis relire le rapport avant tout nouvel echange de fichiers.

Une nouvelle tentative normale après la ratification de localisation, le 22 juillet à 04:30 UTC, a rencontré le même refus avant toute copie. Le rapport `.codex` demeure donc celui de 02:57:51 UTC; les fichiers de localisation, preuves et rapports plus récents ne sont présents que sur la copie locale C: jusqu'à la synchronisation utilisateur.

Le 22 juillet, l’entrée mobile LivingHive a reçu un sélecteur tactile `FR`/`EN` avant le tutoriel. Les cibles font au moins 44x44 px, la langue active est soulignée et la préférence versionnée reste exclusivement dans `PlayerPrefs`; la langue système sert uniquement de défaut. Les 40 clés `splash.*` portent les catalogues à 620/620 entrées uniques, sans doublon ni asymétrie. Unity 6000.5.3f1 passe F8 avec sortie 0 dans `Artifacts/LivingHiveLanguageSelectorF8.log`, puis produit quatre captures exactes dans `Docs/Product/Evidence/LivingHiveLanguageSelector`; les versions FR/EN en 390x844 et 1600x900 ont été inspectées. `Server/` et Communication sont inchangés. Rapport: `Docs/Product/LivingHive_MobileLanguageSelectorMilestone_2026-07-22.md`.

La tranche suivante remplace le faux formulaire officiel par une coquille de pré-authentification honnête. `Connexion` affiche l’état serveur fermé sans champ courriel ou mot de passe; `Créer` ne demande qu’un nom d’affichage pour un profil de démo local. La double porte `MobileAccountSessionGate` n’autorise une future collecte de justificatifs que si readiness serveur et transport mobile sécurisé sont prêts ensemble. Cette coquille ne persiste aucun mot de passe, access token ou refresh token. L’Intégrateur a en parallèle durci `/accounts`, `/auth/login` et `/auth/refresh`: sous Production et drapeaux fermés, les trois routes retournent `503 auth.unavailable` avant tout service métier. Le smoke Production confirme les refus et le build Release passe sans erreur; le nouveau test NUnit compile mais le testhost local découvre zéro test, donc il n’est pas ratifié comme exécuté. Aucun jeton, candidat ou déploiement n’est activé. F8 LivingHive et la capture passent avec sortie 0 dans `Artifacts/LivingHivePreAuthShellF8.log` et `Artifacts/LivingHivePreAuthShellCapture.log`; les catalogues comptent 649/649 clés sans doublon ni asymétrie et huit PNG exacts couvrent accueil, état fermé et profil démo. Aucun module Communication n’a été modifié. Rapport: `Docs/Product/LivingHive_PreAuthMobileShellMilestone_2026-07-22.md`.

La synchronisation officielle après cette tranche a encore échoué avant toute copie avec `Test-Path : Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. `.codex/vm-sync-last-report.txt` reste daté de 02:57:51 UTC, avec 0 conflit et 4 suppressions en attente. Le jalon pré-authentification existe donc seulement sur la copie locale `C:` jusqu’à la prochaine synchronisation lancée depuis la session utilisateur; aucun contournement par `Z:` n’a été tenté.

Prochain test utilisateur: ouvrir `Assets/Scenes/LivingHive.unity`, passer en Play/Game, choisir `EN`, vérifier `Home / Sign in / Create`, ouvrir `Sign in` et confirmer l’état serveur fermé sans champ secret. Ouvrir ensuite `Create`, essayer un nom de moins de trois caractères, puis un nom valide et entrer dans la ruche. Sur l’introduction, un clic révèle le texte s’il défile encore et le suivant ferme l’écran. Quitter Play, relancer et confirmer que la langue est conservée; revenir à `FR` si désiré. Le menu `Développement` reste visible uniquement à l’accueil dans l’éditeur ou un build de développement et n’est pas une surface joueur de production.

Le 22 juillet, la reprise mobile des files est devenue un vrai panneau `Retour à la ruche`. Le résumé est figé au chargement depuis les trois opérations locales bornées et distingue file active et opération terminée pendant l’absence; il ne devient plus vide après l’application idempotente du premier rafraîchissement. `Voir` ouvre bâtiment, Armée ou Recherche avec une cible de 44 px et ne collecte ni ne crédite rien. Les catalogues comptent 735/735 clés. F8 global vert dans `Artifacts/LivingHiveQueueReturn_F8.log`; deux preuves exactes et inspectées sous `Docs/Product/Evidence/LivingHiveQueueReturn`. Le serveur possède désormais une projection locale fermée `HiveOperationResumeSummaryFactory`, 29/29 HiveOperations, build sans erreur, aucun endpoint/push/candidat/déploiement. Rapport client : `Docs/Product/LivingHive_QueueReturnMilestone_2026-07-22.md`. Terrain, image LivingHive, scène canonique et Communication inchangés. La synchronisation finale a encore échoué avant toute copie sur le partage hôte inaccessible au bac à sable; le rapport `.codex` demeure daté de 02:57:51 UTC, avec 0 conflit et 4 suppressions historiques en attente.

Le jalon suivant rend les panneaux de bâtiments vivants sans toucher l’illustration de base. Réserve, atelier, entrepôt, nurserie et poste de garde exposent une activité propre et un état réel; mouvement réduit fige les signaux et mode économie n’en conserve qu’un. L’appareil possède uniquement le rendu et les préférences. Les stocks, opérations et UTC restent serveur; un sous-état non distingué par le snapshot est générique, jamais inventé. L’Intégrateur confirme qu’aucun nouveau contrat n’est requis. Unity compile sans erreur C#, F8 passe 75 contrôles, la compilation de secours donne 0 erreur/217 avertissements historiques et les catalogues comptent 793/793 clés. Deux preuves exactes et inspectées sont sous `Docs/Product/Evidence/LivingHiveBuildingActivity`. Rapport : `Docs/Product/LivingHive_BuildingActivityMilestone_2026-07-22.md`. Terrain, image LivingHive, scènes et Communication sont inchangés; Communication demeure explicitement en pause. La synchronisation officielle tentée à `2026-07-22T09:45:43Z` a échoué avant copie avec accès refusé au partage hôte; `.codex/vm-sync-last-report.txt` reste daté de 02:57:51 UTC, avec 0 conflit et 4 suppressions historiques en attente. Aucun accès direct à `Z:` n’a été tenté.

La tranche suivante enrichit la ruche vivante sans ajouter d’abeille. Les agents existants transportent du nectar, façonnent la cire, trient le pollen, accompagnent le couvain ou matérialisent une patrouille; la sélection renforce seulement la lecture. Mouvement réduit fige les accessoires et mode économie n’en garde qu’un, dans les budgets ambiants 5/8 puis 3/5. L’appareil possède le rendu; les faits de production et d’opération restent serveur, et un sous-état ambigu demeure générique ou caché. Unity compile sans erreur C#, F8 passe 80 contrôles, la compilation de secours donne 0 erreur/217 avertissements historiques et les catalogues restent à 793/793. Deux preuves exactes et inspectées sont sous `Docs/Product/Evidence/LivingHiveSpecializedBeeActivity`. Rapport : `Docs/Product/LivingHive_SpecializedBeeActivityMilestone_2026-07-22.md`. Terrain, image LivingHive, scènes et Communication sont inchangés; Communication demeure en pause. La synchronisation finale tentée à `2026-07-22T10:03:26Z` a échoué avant copie avec accès refusé au partage hôte; `.codex/vm-sync-last-report.txt` reste daté de 02:57:51 UTC, avec 0 conflit et 4 suppressions historiques en attente. Aucun accès direct à `Z:` n’a été tenté.

Le jalon suivant rend l'ambiance de la ruche réactive uniquement aux faits que le serveur sait distinguer honnêtement : amélioration active et production pleine. L'amélioration prend priorité; mouvement réduit fige la phase et mode économie garde un seul signal. La météo reste désactivée et les soins/couvain ou alertes défense restent génériques ou cachés faute de snapshot distinct. Aucune collecte automatique, ressource, file, progression ou abeille n'est créée. Unity compile sans erreur C#, F8 passe 85 contrôles, la compilation de secours donne 0 erreur/217 avertissements historiques et les catalogues restent à 793/793. Les preuves exactes et inspectées sont sous `Docs/Product/Evidence/LivingHiveReactiveAmbience`. Rapport : `Docs/Product/LivingHive_ReactiveAmbienceMilestone_2026-07-22.md`. Les hashes de la carte canonique, de la scène LivingHive et de l'image de base sont inchangés; Communication est restée gelée. L'audit serveur est `Docs/ProductionIntegration/LivingHive_ReactiveAmbience_DeviceServerBoundary_2026-07-22.md` et n'ajoute aucun contrat, endpoint, build ou déploiement. La synchronisation officielle tentée à 10:19:53 UTC a échoué avant copie avec accès refusé au partage hôte; `.codex/vm-sync-last-report.txt` reste daté de 02:57:51 UTC, avec 0 conflit et 4 suppressions historiques en attente. Aucun accès direct à `Z:` n'a été tenté.

Le 22 juillet, la Phase 4 a commencé par un aperçu mobile honnête des voies
stratégiques. Depuis le profil joueur de `LivingHive`, cinq cartes niveau 10
présentent Garde royale, Dard d’assaut, Nourricière, Éclaireuse et Alchimiste en
portrait ou paysage. Le choix affiché reste en mémoire seulement : aucun bonus,
classe, stock ou progrès n’est muté et l’interface indique serveur requis. Le
noyau persistant serveur `phase4-v1` est livré derrière
`StrategicPath:Enabled=false`; ses routes GET/POST authentifiées existent
maintenant mais ne sont pas raccordées au client mobile. Unity compile sans erreur C#,
F8 passe 90 contrôles, la compilation de secours donne 0 erreur/217
avertissements historiques, les catalogues comptent 833/833 clés et les tests
serveur passent 36/36. Les preuves inspectées sont sous
`Docs/Product/Evidence/LivingHiveStrategicPath`; rapport :
`Docs/Product/LivingHive_StrategicPathPreviewMilestone_2026-07-22.md`. Les hashes
de la carte, de la scène LivingHive et de l’image de base sont inchangés;
Communication est resté gelé. Prochaine porte : valider les routes par tests HTTP,
SQL/TLS/IIS et staging Android, puis brancher la coquille mobile avant toute
activation de bonus. La synchronisation officielle tentée à 10:53:14 UTC a
échoué avant copie avec accès refusé au partage hôte; le rapport `.codex` reste
celui de 02:57:51 UTC, avec 0 conflit et 4 suppressions historiques en attente.
Aucun accès direct à `Z:` n’a été tenté.

La tranche suivante permet d’essayer la logique des cinq voies avant tout choix
officiel. Chaque carte ouvre une situation abeille bornée avec deux réactions;
la réponse explique l’affinité ou le compromis, sans simuler de résultat de
combat. L’état reste uniquement en mémoire et disparaît à la fermeture : aucune
ressource, classe, statistique, progression, tâche ou préférence n’est modifiée.
En portrait 390x844 et paysage 1600x900, les cibles font au moins 44 px et les
quatre preuves ont été inspectées. Les routes serveur
`GET/POST /game/v1/hives/{hiveId}/strategic-path` restent fermées par
`StrategicPath:Enabled=false`; le client n’y est pas raccordé et aucun candidat
ou déploiement n’existe. F8 passe 94 contrôles, la compilation de secours donne
0 erreur/217 avertissements historiques, les catalogues comptent 868/868 clés et
les trois fondations protégées gardent leurs empreintes. Rapport :
`Docs/Product/LivingHive_StrategicPathTrialMilestone_2026-07-22.md`.
Communication est resté entièrement gelé. La synchronisation officielle tentée
à `2026-07-22T11:18:08Z` a échoué avant copie avec accès refusé au partage hôte;
le rapport `.codex` demeure daté de `2026-07-22T02:57:51Z`, avec 0 conflit et
4 suppressions historiques en attente. Aucun accès direct à `Z:` n’a été tenté.

La Phase 4 possède maintenant un laboratoire de doctrine accessible par
`Voie stratégique -> Contres`. Le joueur choisit notre essaim puis la menace et
observe avantage, exposition ou neutralité dans le cycle
`guardians > darters > wingrunners > guardians`. Les cinq voies restent des
identités de compte distinctes des trois familles de formation. Aucun combat,
coefficient, dégât, perte, récompense, puissance, voie ou famille officielle
n’est simulé ou modifié. L’appareil conserve seulement les deux choix volatils;
le serveur expose le catalogue `phase4-combat-v1` par
`GET /game/v1/combat/doctrine`, authentifié mais fermé avant lecture par
`CombatDoctrine:Enabled=false`. F8 passe 98 contrôles, les catalogues comptent
902/902 clés, le serveur passe 38/38 tests HiveOperations et les six captures
exactes ont été ratifiées. La carte, la scène LivingHive et l’image de base
gardent leurs empreintes; Communication est resté gelé. Rapport :
`Docs/Product/LivingHive_CombatDoctrineLabMilestone_2026-07-22.md`.
La synchronisation officielle tentée à `2026-07-22T11:37:22Z` a échoué avant
copie avec accès refusé au partage hôte. Le rapport `.codex` demeure daté de
`2026-07-22T02:57:51Z`, avec 0 conflit et 4 suppressions historiques en attente;
aucun accès direct à `Z:` n’a été tenté.

## Premiere verification dans la VM

1. Confirmer le nom de l'ordinateur et le dossier courant.
2. Confirmer l'acces a `Z:` et synchroniser.
3. Confirmer Unity `6000.5.3f1` et ses modules Android.
4. Compiler les assemblages jeu et editeur.
5. Ouvrir la copie locale de `LivingHive` et poursuivre le plan d'execution.
6. Ne jamais demarrer une regeneration d'image de carte ou de ruche sans demande
   explicite de l'utilisateur.

La Phase 4 relie maintenant le laboratoire de doctrine à une préparation
d’escouade honnête dans `Armée -> Préparer`. Seules les Gardiennes de l’aperçu
local sont préremplies; Soldats et Éclaireuses restent hors doctrine, et les
Voltigeuses/Lanceuses absentes sont `Non reconnue`. Aucun renommage implicite,
combat, coefficient, ordre ou résultat n’est créé. L’appareil possède le rendu,
la lecture locale et le brouillon volatil. Le serveur possède la route fermée
`GET /game/v1/hives/{hiveId}/combat/formation-readiness`; faute de roster
doctrinal durable, elle répond `not_recorded`, `families={}` et liste les rôles
historiques non classifiés. `CombatFormationReadiness:Enabled=false` demeure
fermé par défaut et en Production. Unity passe F8 avec 104 contrôles, les
assemblages jeu/éditeur compilent sans erreur, les catalogues comptent 935/935
clés, le serveur passe 40/40 tests HiveOperations et huit captures exactes sont
ratifiées sous `Docs/Product/Evidence/LivingHiveStrategicPath`. Rapport :
`Docs/Product/LivingHive_FormationReadinessMilestone_2026-07-22.md`. Carte,
scène LivingHive et image de base gardent leurs empreintes; Communication est
resté totalement gelé. Prochaine porte : roster doctrinal serveur durable et
sources d’entraînement réelles avant toute composition officielle.

La synchronisation officielle suivant ce jalon, tentée à
`2026-07-22T12:00:00Z`, a échoué avant toute copie avec `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport `.codex` demeure daté de
`2026-07-22T02:57:51Z`, avec 0 conflit et 4 suppressions historiques en attente.
Le jalon reste donc uniquement sur `C:` jusqu’à la synchronisation utilisateur;
aucun accès direct à `Z:` ni remappage n’a été tenté.

La tranche suivante ajoute la composition multi-famille au panneau
`Armée -> Préparer`. Le contrat commun appareil/serveur est
`phase4-combat-squad-reservation-v1`, capacité 12 et clés exactes
`guardians/wingrunners/darters`. Le mobile conserve uniquement un brouillon
volatil, la suggestion doctrinale et le rendu; il ne réserve, ne consomme et ne
simule aucun combat. Le serveur possède GET/commit/release atomiques,
idempotents et cloisonnés par joueur/ruche derrière
`CombatSquadReservation:Enabled=false` par défaut et en Production. L’Intégrateur
a ratifié 7/7 tests ciblés, 47/47 HiveOperations et le build Release sans erreur;
aucun candidat ni déploiement n’existe.

Une corruption antérieure du préfixe de
`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` a été révélée par
la première compilation. Le bloc a été restauré depuis la DLL Unity saine de
08:15, raccordé sur l’unique frontière `DrawSplashLanguageButton`, puis les
trois champs de composition postérieurs ont été réinsérés. Les assemblages jeu
et Éditeur compilent maintenant avec 0 erreur. Le harnais autonome passe 64/64,
les catalogues comptent 949/949 clés alignées et les trois fondations protégées
gardent leurs empreintes antérieures. Rapport :
`Docs/Product/LivingHive_SquadCompositionMilestone_2026-07-22.md`.

Les deux refus de licence initiaux sont désormais historiques. Après
stabilisation de Hub, le F8 global contemporain repasse avec marqueur de succès
et zéro `error CS`. `SandboxLivingHiveStrategicPathCapture.CaptureAndExit`
produit 12/12 images exactes avec manifeste SHA-256. Les deux preuves
`LivingHive_SquadComposition_Mixed_*` en 390x844 et 1600x900 ont été inspectées
à résolution native sans collision. Tous les processus Unity, dotnet et testhost
sont arrêtés. Communication reste entièrement gelé.

La tentative normale de synchronisation au début de cette tranche a encore
échoué avant toute copie avec accès refusé à
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Le travail reste uniquement sur `C:` jusqu’à
une synchronisation lancée depuis la session utilisateur; aucun accès direct à
`Z:` ou remappage n’a été tenté.

La fondation serveur suivante est ratifiée sous
`phase5-hive-perimeter-sortie-v1`. Deux signaux non-combat utilisent une instance
liée au joueur, à la ruche et au cycle UTC de huit heures. Launch exige la
réservation Phase 4, claim crédite une seule fois et libère, recall libère sans
récompense. Les preuves passent à 5/5 DurableJson, 52/52 HiveOperations, 5/5
HTTP et 265 réussis + 7 SQL ignorés sur `BeeKingdom.Tests` net10; le build
Release reste sans erreur. Les flags par défaut et Production restent fermés,
la carte mondiale demeure `PreparationOnly` et aucun déploiement n’est autorisé.

Le panneau mobile est maintenant raccordé au read model de sortie par un
contrôleur injectable. La composition, puis les sorties, sont ratifiées en
portrait et paysage. Le contrôleur officiel du cycle de session reste la porte
suivante : par défaut l'écran affiche `NotConfigured` et ne simule aucune
mutation. L’appareil ne doit garder que cache, brouillon, rendu et notifications;
serveur pour horloge, signal, réservation, sortie et récompense. Rapport :
`Docs/Product/LivingHive_HivePerimeterSortieServerMilestone_2026-07-22.md`.

La synchronisation de fin tentée à `2026-07-22T15:20:11Z` a encore échoué
avant toute copie avec `Accès refusé` sur le partage hôte. Le rapport `.codex`
reste daté de `2026-07-22T02:57:51Z`, 0 conflit et 4 suppressions historiques
en attente. Tous les changements de cette tranche restent sur `C:`; ne pas
écrire directement sur `Z:` et ne pas remapper le partage.

Le pont mobile de la sortie Phase 5 est livré dans `BeeKingdom.Networking` et
raccordé au présentateur par injection. Il couvre GET/commit/release
de la réservation et GET/launch/claim/recall de la sortie, derrière
`MobileAccountSessionGate`. Aucun jeton ou snapshot n'est persisté; temps,
révisions, signaux, réservations et récompenses restent serveur. Le validateur
refuse toute réponse étrangère ou altérée et recalcule les instances liées au
joueur, à la ruche et au cycle. L'audit a fait ajouter `Revision` au snapshot
serveur; service et HTTP prouvent rev0 → rev1 → rev2, nouvelle réservation puis
second lancement rev3. Preuves client : 18/18 et compilation `netstandard2.1`
0 erreur/0 avertissement. Rapport :
`Docs/Product/LivingHive_HivePerimeterSortieMobileContractMilestone_2026-07-22.md`.

Le snapshot Phase 5 expose aussi `ServerTimeUtc`, prouvé contre l'horloge
injectable côté service et HTTP. La présentation isolée dérive le temps restant
de cette référence puis d'un elapsed monotone local; elle ne consulte pas
l'horloge murale du téléphone pour décider qu'une récompense est prête. Les six
états de présentation portent le harnais client à 18/18. Le présentateur partagé
est désormais compilé, F8 vert et inspecté dans quatre preuves exactes.

Le vrai transport authentifié, le cache protégé, Android/staging et l'activation
des flags sont encore des portes ouvertes. Le contrôleur par défaut reste
`NotConfigured` jusqu'à cette injection officielle. Communication demeure gelé.

Le transport de session officielle existe maintenant dans
`BeeKingdom.Networking` avec HTTPS obligatoire, readiness publique, login,
refresh `/auth/refresh`, logout bearer, access token mémoire et refresh Android
Keystore AES-GCM. Le bootstrap ne s'active que par une ressource runtime absente;
l'état produit reste donc `NotConfigured`. L'interface ne collecte aucun
justificatif tant que serveur, transport et coffre ne sont pas tous prêts.

Preuves locales : harnais 34/34, F8 final vert dans
`Artifacts/LivingHiveMobileAccountSession_ClosureF8.log`, quatre
captures exactes sous `Docs/Product/Evidence/MobileAccountSession`, catalogues
1054/1054. Le premier F8 a refusé un chevauchement paysage; deux inspections ont
refusé des messages d'état incohérents puis un en-tête portrait trop long. Les
preuves finales corrigées sont ratifiées. Rapport :
`Docs/Product/LivingHive_OfficialMobileAccountSessionMilestone_2026-07-22.md`.

Android Keystore doit encore être prouvé sur appareil physique. Aucune ressource
staging, aucun compte réel, flag Production, candidat ou déploiement n'existe.
Le serveur ignore maintenant l'IP JSON et stocke uniquement l'adresse de la
connexion, sans accepter implicitement de header forwarded. La prochaine tranche
doit configurer le proxy de confiance, tester deux joueurs sur HTTPS/SQL staging
et ajouter un cache de gameplay protégé en lecture offline.
Communication demeure gelé.

La synchronisation finale de la tranche session mobile, tentée à
`2026-07-22T19:56:36Z`, a échoué avant toute copie avec `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport `.codex` reste daté de
`2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers hôte et 4 suppressions
historiques en attente. Le jalon reste sur `C:`; aucun remappage ou accès direct
à `Z:` n'a été tenté.

Les hashes de la scène canonique, de LivingHive et de son image de base restent
respectivement `927FA2...B1B3`, `ECCFE9...8E7` et `3C0E3...467F6`.

Le jalon suivant raccorde la session mobile aux routes officielles de sortie.
`UnityAuthenticatedGameRestTransport` exige HTTPS, borne requêtes/réponses et
décode les identifiants serveur, dictionnaires, UTC et durées. Le renouvellement
d'accès est dédupliqué; après `401`, une seule répétition conserve la même
commande et la même clé d'idempotence. Un second `401` purge la session. Une
panne réseau ne répète jamais POST.

Les derniers GET validés peuvent être conservés dans un document AndroidKeyStore
AES-GCM distinct, partitionné joueur/ruche/contrat/route, 12 entrées maximum,
512 Kio par lecture, 1 Mio total et 7 jours. Corruption ou version inconnue
supprime le cache. L'UI affiche `CONSULTATION HORS LIGNE`, enlève toutes les
actions serveur et ne crédite jamais ressource, récompense ou progression.

Le runtime reste opt-in : aucun
`Assets/Resources/BeeKingdom/MobileAccountSessionRuntime.asset` n'est livré.
L'online exige l'autorité serveur; l'offline exige une identité connue dans le
coffre protégé Android. Le serveur impose maintenant `private, no-store` et
`Pragma: no-cache` sur les GET game; ses tests ciblés passent 2/2 et son build est
sans erreur. Aucun candidat ni déploiement.

Preuves : harnais 43/43, F8 global de clôture vert dans
`Artifacts/LivingHiveAuthenticatedGameBridge_ClosureF8.log`, huit captures exactes dans
`Docs/Product/Evidence/LivingHivePerimeterSortie`; les états offline FR 390x844
et EN 1600x900 ont été inspectés. Catalogues 1056/1056 sans doublon. Rapport :
`Docs/Product/LivingHive_AuthenticatedGameRestAndProtectedReadCacheMilestone_2026-07-22.md`.

Restent ouverts : Android physique/Keystore, build IL2CPP/AOT, TLS et SQL
staging, asset/HiveId d'environnement, flags/comptes réels et déploiement. Les
trois fondations protégées gardent leurs empreintes. Communication est resté
gelé.

La synchronisation officielle finale de cette tranche, tentée à
`2026-07-22T20:36:25Z`, a échoué avant toute copie avec le même accès refusé au
partage hôte. `.codex/vm-sync-last-report.txt` demeure daté de
`2026-07-22T02:57:51Z`, avec 0 conflit et 4 suppressions historiques en attente.
Aucun remappage, accès direct à `Z:` ou relâchement du bac à sable n'a été tenté.

Audit Android IL2CPP du 22 juillet : le harnais
`SandboxLivingHiveAndroidAotProofBuilder.BuildAndExit` cible uniquement
`Assets/Scenes/LivingHive.unity`, Android IL2CPP ARM64 et un APK Development. Il
refuse tout asset runtime compte/serveur et n'installe ou ne déploie rien. La
passe a atteint les sorties IL2CPP, `libil2cpp.so` et CMake `arm64-v8a`, puis
Gradle a échoué à `:launcher:compressDebugAssets` avec `Espace insuffisant sur
le disque`. Le Player rapporté pèse 2,02 Gio compressés / 2,25 Gio non
compressés. Aucun APK ni manifeste de succès n'existe. Journal :
`Artifacts/LivingHiveAndroidAotProof_Build.log`.

Les caches Android/artifacts de cette passe ont été nettoyés après fermeture;
environ 15 Gio sont libres et aucun processus Unity/dotnet/testhost/Java/Bee/
clang ne reste. Le harnais a été corrigé pour restaurer et sauvegarder les
réglages avant `EditorApplication.Exit`; `ProjectSettings` est revenu à Mono et
architectures 3. Aucun asset
`Assets/Resources/BeeKingdom/MobileAccountSessionRuntime.asset` n'existe.

La passe Windows de régression
`Artifacts/LivingHiveAndroidAotProof_ClosureF8.log` a rechargé les assemblages
avec 0 `error CS`, mais n'a pas exécuté F8 : trois reconnexions ont refusé
`com.unity.editor.headless`. L'unique PID Unity a été arrêté de façon ciblée.
Ne pas déclarer cette passe verte ou rouge. Rétablir Hub/licence, relancer F8,
puis auditer et réduire/externaliser les 2,25 Gio avant une nouvelle tentative
de package Android. Rapport :
`Docs/Product/LivingHive_AndroidIl2CppPackagingAudit_2026-07-22.md`.

Cause racine mobile ensuite mesurée sans modifier les fondations : `Resources`
totalise 995,1 Mio / 5 273 fichiers. `WorldMapWave6Runtime` porte 922,8 Mio /
5 004 fichiers, partagés entre le terrain canonique exact-crop (825,0 Mio) et
l'ancien `v1` (97,8 Mio). Les 2 500 métadonnées Android canoniques sont toutes
non compressées; le garde d'import le force. `LivingHive` n'utilise pas la carte,
mais Unity inclut tout `Resources` dans son Player.

Aucun terrain, PNG, manifeste, `.meta`, scène ou chargeur n'a été touché. La
cible proposée garde le shell/LivingHive dans l'installation, télécharge le seul
terrain canonique en bundles vérifiés et laisse au serveur/CDN le manifeste et
les objets immuables. L'Intégrateur a livré le contrat Server/tests/docs fermé
`GET /runtime/world-map-content-manifest`, public et statique : HTTPS seulement,
URI sans secrets, tokens/IDs uniques, 512 Mio par bundle, 2 Gio cumulés,
ETag/304 et `no-store` sur 503. Le flag est faux par défaut et en Production.
L'Architecte a rejoué 7/7 tests sous net10.0; build sans erreur, warning SqlClient
préexistant. Aucun binaire ni déploiement. Rapports :
`Docs/Product/LivingHive_MobileContentOwnershipAndBudget_2026-07-22.md`.
`Docs/ProductionIntegration/WorldMap_ContentManifestBoundary_2026-07-22.md`.

La synchronisation finale de la série Android/contenu, tentée à
`2026-07-22T21:52:53Z`, a échoué avant toute copie : accès refusé sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport
`.codex/vm-sync-last-report.txt` est resté daté du
`2026-07-22T02:57:51Z` avec 0 conflit, 0 copie VM vers hôte et 4 suppressions
historiques en attente. Aucun remappage, accès direct `Z:` ou relâchement du bac
à sable n'a été tenté. Tous les changements de cette série restent sur `C:`.

La tranche suivante livre la production après absence sous contrat
`living-hive-offline-production-v1`. Le serveur est seul auteur de l’UTC, du
catalogue, des taux, capacités, pending, soldes, révisions et reçus. GET et POST
authentifiés sont persistants et idempotents; les flags restent faux par défaut
et en Production. Preuves serveur : 315 réussis, 0 échec, 8 SQL ignorés;
25/25 service et 8/8 HTTP; build Release sans erreur.

Le client mobile strict, le cache GET AndroidKeyStore partitionné, le contrôleur
de panneau et sa composition au cycle de session sont en place. L’appareil ne
crédite jamais localement et ne répète pas un POST après panne réseau. Sans asset
d’environnement, le parcours officiel s’arrête au shell; la démo de ruche reste
locale. Le harnais injecte séparément l’état de panneau `Production officielle
non configurée`, masque toute quantité absente et désactive `Serveur requis`.
Cette preuve de mise en page ne représente pas une session live.

Validation de clôture : harnais 60/60, `Assembly-CSharp` 0 erreur, F8 final vert
dans `Artifacts/LivingHiveOfflineProductionF8Final.log`, catalogues 1080/1080
sans doublon et captures finales exactes/inspectées en 390x844 et 1600x900 sous
`Docs/Product/Evidence/LivingHiveOfflineProduction`. Une passe sans périphérique
graphique et une paire montrant encore `simulation locale` ont été refusées;
seules les preuves finales sans valeur serveur inventée sont ratifiées. Aucun
terrain, PNG, scène ni image LivingHive n’a changé. Communication est resté gelé.

Rapports :
`Docs/ProductionIntegration/LivingHive_OfflineProduction_ServerAuthority_2026-07-22.md` et
`Docs/Product/LivingHive_OfflineProductionMobileMilestone_2026-07-22.md`.

Restent ouverts : catalogue économique réel, SQL/TLS staging, proxy de confiance,
asset/HiveId, deux joueurs, Android Keystore physique, paquet IL2CPP après budget
de contenu, flags, candidat et déploiement. Les taux 10/5/8 sont uniquement des
valeurs de test et ne doivent jamais être annoncés comme équilibre produit.

La synchronisation officielle de clôture, tentée à `2026-07-23T01:25Z`, a
échoué avant toute copie avec `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. `.codex/vm-sync-last-report.txt` demeure
daté de `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers hôte et 4
suppressions historiques en attente. Aucun remappage, accès direct `Z:` ou
relâchement du bac à sable n’a été tenté. Tous les changements restent sur `C:`.
État final des processus : Unity=0, dotnet=0, testhost=0, bee_backend=0, Java=0,
clang=0.

La tranche suivante implémente la file de construction persistante sous
`living-hive-building-upgrade-v1`. L'Intégrateur possède le serveur : GET,
start et complete authentifiés; catalogue, coût, solde, UTC, durée, niveau,
révision, slot et reçus sont autoritaires et persistants. Le flag est faux par
défaut et en Production, et le catalogue live est vide.

Le client mobile strict, le cache GET protégé/partitionné, le contrôleur de
panneau, le cycle de session et les ancrages LivingHive hors chat sont en place.
La file latérale et le détail `wax_workshop` basculent sur l'état officiel quand
le contrôleur est injecté. Hors configuration, aucun montant, niveau, succès ou
reçu n'est inventé. Le mobile ne répète pas une mutation réseau aveuglément et
ne peut compléter que sur le statut serveur `awaiting_completion`.

Preuves acquises : serveur net10.0 321 réussis, 0 échec, 8 SQL ignorés; harnais
client 13/13; `BeeKingdom.Networking`, `Assembly-CSharp`, `BeeKingdom.Tests` et
`Assembly-CSharp-Editor` compilent à 0 erreur avec les nouveaux fichiers;
catalogues 1115/1115, 35 clés construction, aucun doublon. Les SHA-256 de la
scène canonique, de LivingHive et de l'image de base sont inchangés.

Unity est resté fermé. Le lancement automatisé a été refusé par la limite de
l'outil, pas par le projet. F8 et les captures honnêtes `NotConfigured`
390x844 FR / 1600x900 EN restent donc à ratifier demain. Ne pas les annoncer
comme acquis. Puis restent catalogue économique, SQL/TLS staging,
environnement/HiveId, deux joueurs, Android Keystore physique, paquet IL2CPP,
flags, candidat et déploiement. Rapport :
`Docs/Product/LivingHive_BuildingUpgradeMobileMilestone_2026-07-22.md`.

La synchronisation de début de tranche a échoué avant toute copie parce que le
partage hôte était inaccessible. Aucun conflit n'a été écrasé et tous les
changements restent sur `C:`. Communication est resté intégralement gelé.

La tranche suivante raccorde la Recherche officielle sous
`living-hive-research-v1`. Le client mobile valide identité, ruche, contrat,
catalogue, UTC, soldes, coûts, durées, effets structurés, opération, révision et
reçus; un GET validé peut alimenter le cache protégé, mais aucune mutation ne
fonctionne hors ligne. Une seule rotation est permise après 401 et une panne
réseau conserve la même clé d’idempotence sans répétition aveugle. Le cycle de
session ferme le contrôleur au logout ou au changement de joueur.

Le panneau Recherche et la file latérale utilisent l’état serveur quand il est
injecté. Le téléphone projette le temps monotone uniquement pour l’affichage;
seul `awaiting_completion` autorise la complétion. Sans configuration,
`Serveur requis` ne montre aucun coût, effet, solde, réussite ou reçu. La
prévisualisation locale demeure `local_preview_non_official`.

L’Intégrateur a livré GET/start/complete authentifiés, `effects` structuré, le
calcul de production depuis les bps persistés et le filtrage par
`LivingHiveResearch:Catalog`. Le catalogue est vide par défaut et en Production
et le flag reste faux. L’Architecte a rejoué les binaires Release : HTTP 4/4,
suite serveur 325 réussis/0 échec/8 SQL ignorés. Client 13/13, présentation 6/6,
quatre assemblages à 0 erreur, catalogues 1153/1153 avec 38 clés officielles.
Les hashes de la scène canonique, de LivingHive et de l’image de base sont
inchangés. Une tentative Debug a été ignorée après refus d’écriture d’un cache
`obj`; seuls les replays Release verts sont comptés.

Unity est resté fermé : F8, assertions agrégées et captures honnêtes Recherche
390x844 FR / 1600x900 EN restent à ratifier avec les captures Construction déjà
en attente. Avant staging restent aussi les scénarios HTTP complets de
start/complete/rejeu/effets historiques, SQL/TLS, catalogue économique,
environnement/HiveId, Android physique, paquet IL2CPP, flags, candidat et
déploiement. Rapport :
`Docs/Product/LivingHive_OfficialResearchMobileMilestone_2026-07-22.md`.
Communication est resté intégralement gelé.

La synchronisation de fin Recherche, tentée à `2026-07-23T03:23:08Z`, a échoué
avant toute copie avec `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. `.codex/vm-sync-last-report.txt` demeure
daté du `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers hôte et 4
suppressions historiques en attente. Aucun remappage, accès direct `Z:` ou
relâchement du bac à sable n’a été tenté. Tous les changements restent sur
`C:`. État final : Unity=0, dotnet=0, testhost=0, bee_backend=0, Java=0 et
clang=0.

La tranche suivante raccorde `Sac & stocks` à la lecture autoritaire
`living-hive-stock-v1`. Le serveur possède l’identité, la ruche, le catalogue,
la révision, l’UTC, les montants et capacités, la population optionnelle, les
recherches terminées et les opérations actives. Le téléphone possède seulement
le rendu, l’état transitoire et le dernier GET protégé/partitionné; il ne
fabrique, ne collecte, ne débite ou ne termine rien.

Le cycle de session officiel crée et ferme le contrôleur Stock avec les autres
contrôleurs. Le panneau LivingHive n’affiche jamais les montants locaux dans le
mode officiel et les boutons `Voir` naviguent sans mutation. Vingt-et-une clés
localisées portent les états fermé, chargement, prêt, lecture hors ligne et
erreur.

Preuves acquises hors Unity : client 10/10, présentation 5/5,
`BeeKingdom.Networking`, `Assembly-CSharp`, `BeeKingdom.Tests` et
`Assembly-CSharp-Editor` à 0 erreur, catalogues 1174/1174 sans divergence,
HiveOperations 51/51. Sept tests Editor et un harnais de deux captures honnêtes
ont été ajoutés mais pas exécutés.

Le premier replay global a révélé une fixture HTTP activée incomplète
(327 réussis, 8 SQL ignorés, 1 échec), qui a été corrigée sans la masquer. Les
replays finaux passent : Stock noyau 2/2, HTTP 3/3 avec deux lectures non
mutantes, serveur global 328 réussis/0 échec/8 SQL ignorés. Le nouvel
avertissement CS8602 est supprimé.

Restent la preuve de projection après une mutation Ronde quotidienne, F8, tests
Editor, captures 390x844 FR / 1600x900 EN, SQL/TLS staging,
environnement/HiveId, Android physique, paquet IL2CPP, flags, candidat et
déploiement. La Ronde quotidienne officielle complète vient après ratification
Unity du snapshot Stock. Rapport :
`Docs/Product/LivingHive_OfficialStockMobileMilestone_2026-07-22.md`.
Communication est resté intégralement gelé.

La synchronisation de fin Stock, tentée à `2026-07-23T04:14:35Z`, a échoué
avant toute copie avec `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. `.codex/vm-sync-last-report.txt` demeure
daté du `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers hôte et 4
suppressions historiques en attente. Aucun remappage, accès direct `Z:` ou
relâchement du bac à sable n’a été tenté. Tous les changements restent sur
`C:`. État final : Unity=0, dotnet=0, testhost=0, bee_backend=0, Java=0 et
clang=0.
