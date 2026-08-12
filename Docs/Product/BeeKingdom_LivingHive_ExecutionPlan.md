# Bee Kingdom - Plan d'execution LivingHive

## Jalon courant — réservation officielle d’escouade mobile

- `Armée -> Préparer` réserve et libère maintenant la composition officielle
  via le contrat `phase4-combat-squad-reservation-v1`.
- Appareil : rendu, brouillon volatil, GET protégé et commande commit/release
  chiffrée avant transport. Aucun auto-submit, débit, combat ou succès local.
- Serveur : joueur/ruche, roster, disponible, réservé, capacité, identifiant,
  UTC, révisions, transaction et reçus publics idempotents bornés à 128.
- Un résultat ambigu conserve la commande; seul `Vérifier la commande` rejoue
  exactement sa clé, sa charge et sa révision. La reconstruction du contrôleur
  n’envoie rien automatiquement.
- Preuves hors Unity : client 24/24; présentation 10/10; assemblages jeu/éditeur
  0 erreur; serveur 3/3 cœur, 5/5 HTTP et 346 réussis, 0 échec,
  8 SQL ignorés; catalogues 1279/1279 sans doublon ni écart.
- `CombatSquadReservation:Enabled=false` reste fermé par défaut et en
  Production. Aucun candidat, activation ou déploiement.
- Rapport :
  `Docs/Product/LivingHive_OfficialSquadReservationMobileMilestone_2026-07-23.md`.
- Prochaine porte : Unity/F8/tests Editor et inspection 390x844/1600x900, puis
  Android/Keystore/TLS/SQL staging. Les mutations `launch`, `claim` et `recall`
  de la Phase 5 doivent encore recevoir la même outbox protégée.
- La synchronisation finale du `2026-07-23T09:14:57Z` a échoué avant copie :
  accès refusé au partage hôte. Aucun contournement n’a été tenté; la tranche
  demeure sur `C:`.

## Jalon courant — recrutement doctrinal officiel mobile

- `Armée -> Préparer` lit maintenant le roster, les soldes, les offres et
  l'opération de Caserne officiels quand une session/ruche est autorisée.
- Appareil : rendu, brouillon volatil, cache GET protégé, minuteur monotone et
  outbox protégée. Aucun débit, effectif, claim ou retry automatique local.
- Serveur : identité, trois offres, soldes miel/pollen, roster, rôles legacy,
  UTC, opération, révisions et reçus idempotents bornés à 128.
- États prêts, hors ligne lecture seule et confirmation ambiguë sont intégrés.
  Une reprise explicite réemploie exactement la commande protégée.
- Preuves hors Unity : client 13/13; présentation 10/10; assemblages produit,
  réseau et Editor 0 erreur; serveur 4/4 cœur, 3/3 HTTP, 341 réussis,
  0 échec, 8 SQL ignorés; build Release 0 erreur; catalogues 1262/1262.
- `CombatRecruitment:Enabled=false` et
  `CombatFormationReadiness:Enabled=false` par défaut et en Production. Aucun
  candidat, activation ou déploiement.
- Rapport :
  `Docs/Product/LivingHive_OfficialDoctrineRecruitmentMobileMilestone_2026-07-23.md`.
- Prochaine porte : compilation Unity normale, F8, tests Editor, inspection
  390x844/1600x900 et parcours manuel dans `Assets/Scenes/LivingHive.unity`,
  puis Android/Keystore/TLS/SQL staging.
- La synchronisation finale du `2026-07-23T08:30:22Z` a échoué avant copie sur
  le partage hôte inaccessible. Aucun contournement n'a été tenté; le jalon
  demeure sur `C:`.

## Jalon courant — soin officiel du couvain mobile

- La Nurserie possède maintenant deux commandes officielles : nourrir
  (300 miel, 12 s, +22 nutrition) et stabiliser
  (45 cire, 13 s, +7 stabilité).
- L'appareil rend, met en cache le dernier GET et protège une commande avant
  transport. Il ne débite, ne crédite et ne soumet jamais automatiquement.
- Le serveur possède ressources, vitalité, UTC, opération, révisions et reçus
  persistants bornés à 128. START et COMPLETE se rejouent exactement sans
  seconde mutation.
- UI responsive, états hors ligne/incertains, minuteur monotone, cibles 44 px et
  39 clés FR/EN sont intégrés dans le détail de la Nurserie.
- Preuves hors Unity : mobile 23/23; cœur serveur 9/9; HiveOperations 69/69;
  HTTP 4/4; serveur net10 338 réussis, 0 échec, 8 SQL ignorés; build 0 erreur;
  catalogues 1238/1238.
- `BroodVitality:Enabled=false` et `HiveDailyRound:Enabled=false` par défaut et
  en Production. Aucun candidat, activation ou déploiement.
- Rapport :
  `Docs/Product/LivingHive_OfficialBroodCareMobileMilestone_2026-07-23.md`.
- Prochaine porte : F8, tests Editor, inspection 390x844/1600x900 et parcours
  manuel dans `Assets/Scenes/LivingHive.unity`, puis Android/TLS/SQL staging.

## Jalon courant — Ronde quotidienne officielle mobile

- `Quêtes -> Ronde` lit maintenant trois faits serveur réels : collecte reçue,
  opération lancée et snapshot Stocks lu.
- L'appareil affiche, protège le dernier GET et prépare la mutation. Le serveur
  possède le jour UTC, les faits, la révision, le reçu et les crédits
  120 miel/60 pollen.
- La boîte protégée ne soumet jamais automatiquement après redémarrage ou
  ambiguïté; un retry explicite réemploie exactement la même commande.
- Les sources officielles Stocks, Amélioration, Recherche et Production marquent
  leur fait atomiquement. Un replay ne modifie plus révision, pending ou solde.
- Preuves hors Unity : mobile 8/8 + 10/10 + 9/9; quatre assemblages à 0 erreur;
  serveur 336 réussis, 0 échec, 8 SQL ignorés; catalogues 1199/1199.
- Drapeau `HiveDailyRound:Enabled=false` par défaut et en Production. Aucun
  candidat, activation ou déploiement.
- Rapport :
  `Docs/Product/LivingHive_OfficialDailyRoundMobileMilestone_2026-07-23.md`.
- Prochaine porte : F8, tests Editor, captures exactes et parcours manuel dans
  `Assets/Scenes/LivingHive.unity`, puis appareil Android/TLS staging.

## Jalon courant — débrief de retour de sortie

- Le claim Phase 5 renvoie maintenant une preuve durable des crédits réellement
  appliqués, des soldes/capacités résultants, des identités, de la révision et de
  l'heure serveur.
- Le crédit miel/pollen est plafonné séparément : intégral, partiel ou nul si la
  capacité est pleine. Un rejeu idempotent rend exactement la même preuve sans
  second crédit.
- Le mobile valide strictement le reçu, puis affiche `Retour confirmé`, les deux
  crédits exacts, les stocks/capacités et la cause d'un plafonnement. `Continuer`
  masque seulement la preuve volatile; aucune mutation locale n'est créée.
- Sans shell officiel, l'état réel reste `NotConfigured`; les aperçus de débrief
  sont explicitement marqués QA et ne contactent aucun serveur.
- Preuves : serveur 266 réussis + 7 SQL ignorés; harnais mobile 21/21;
  assemblages jeu/éditeur à 0 erreur; F8 final vert; six captures exactes et
  inspectées en 390x844/1600x900.
- Catalogues : 1022/1022 clés uniques, dont 73 `perimeter_sortie.*` et 16
  `perimeter_sortie.debrief.*` par langue.
- Rapport :
  `Docs/Product/LivingHive_HivePerimeterReturnDebriefMilestone_2026-07-22.md`.

## Jalon historique — aperçu local du recrutement doctrinal

- La file locale de Caserne et le roster mobile v2 du 22 juillet restent le
  parcours de démonstration quand aucun runtime officiel n'est injecté.
- Ce jalon ne convertit jamais Soldats ou Éclaireuses et ses preuves F8/visuelles
  demeurent valides.
- Le raccord officiel courant décrit au début de ce document remplace ses
  anciennes limites « aucun raccordement mobile » et « HTTP non prouvé ».
- Rapport historique :
  `Docs/Product/LivingHive_DoctrineRecruitmentMilestone_2026-07-22.md`.

## Jalon courant — laboratoire de doctrine de combat

- `Voie stratégique -> Contres` enseigne le triangle Gardiennes, Voltigeuses et
  Lanceuses par deux choix tactiles : notre essaim, puis la menace observée.
- Le résultat distingue avantage, exposition et neutralité, tout en rappelant
  qu’un contre ne garantit jamais la victoire. Aucun coefficient, dégât, perte,
  butin, puissance ou résultat de combat n’est calculé.
- Les cinq voies restent des identités de compte; les trois familles sont des
  rôles de formation. Le laboratoire ne sélectionne ni voie ni famille.
- Appareil : langue et deux choix volatils en mémoire. Serveur : catalogue
  `phase4-combat-v1`, identifiants et cycle autoritaires; futurs coefficients,
  formations et combats officiels.
- `GET /game/v1/combat/doctrine` existe, authentifié et fermé par
  `CombatDoctrine:Enabled=false` avant toute lecture. Le client n’y est pas
  raccordé; aucun candidat, activation ou déploiement.
- F8 passe 98 contrôles; compilation jeu+éditeur 0 erreur/217 avertissements
  historiques; serveur 38/38; catalogues 902/902. Six preuves exactes, dont deux
  nouvelles inspectées, sont sous
  `Docs/Product/Evidence/LivingHiveStrategicPath`.
- Rapport :
  `Docs/Product/LivingHive_CombatDoctrineLabMilestone_2026-07-22.md`.
- La synchronisation officielle de `2026-07-22T11:37:22Z` a échoué avant copie
  sur le partage hôte inaccessible; le jalon reste local sur `C:` sans
  contournement.

## Jalon courant — essais tactiques des voies stratégiques

- Chaque voie niveau 10 possède maintenant une mise en situation abeille avec
  deux réactions réversibles. Le résultat révèle l’affinité ou le compromis sans
  simuler un combat ni attribuer un résultat de jeu.
- L’essai est entièrement volatile : aucune ressource, classe, statistique,
  progression, tâche, préférence ou sélection officielle n’est modifiée.
- Portrait 390x844 et paysage 1600x900 restent lisibles sans défilement; les
  cibles font au moins 44 px et la fermeture efface la réponse courante.
- Appareil : langue, rendu, voie et réponse courante en mémoire. Serveur :
  appartenance, niveau 10, catalogue `phase4-v1`, révision, verrouillage,
  idempotence et UTC.
- Les routes `GET/POST /game/v1/hives/{hiveId}/strategic-path` existent, mais
  restent fermées par `StrategicPath:Enabled=false` et ne sont pas raccordées au
  client mobile. Aucun candidat, activation ou déploiement.
- F8 passe 94 contrôles; compilation de secours 0 erreur/217 avertissements
  historiques; catalogues 868/868; quatre preuves exactes et inspectées sous
  `Docs/Product/Evidence/LivingHiveStrategicPath`.
- Rapport :
  `Docs/Product/LivingHive_StrategicPathTrialMilestone_2026-07-22.md`.
- La synchronisation officielle de `2026-07-22T11:18:08Z` a échoué avant copie
  sur le partage hôte inaccessible; le jalon reste local sur `C:` sans
  contournement.

## Jalon courant — aperçu des voies stratégiques

- Le profil joueur ouvre cinq identités de colonie niveau 10 : Garde royale,
  Dard d’assaut, Nourricière, Éclaireuse et Alchimiste, avec rôle, forces et
  compromis lisibles en portrait et en paysage.
- Le toucher change uniquement l’aperçu en mémoire. Aucun bonus, classe,
  progression ou ressource n’est modifié; la sélection officielle reste fermée
  et l’interface l’indique explicitement.
- Appareil : rendu, langue et option inspectée. Serveur : éligibilité, catalogue,
  choix verrouillé, révision, idempotence, isolation et UTC. Le noyau `phase4-v1`
  et ses routes GET/POST existent derrière `StrategicPath:Enabled=false`, sans
  raccordement au client mobile.
- Unity compile sans erreur C#; F8 passe 90 contrôles; compilation de secours :
  0 erreur/217 avertissements historiques; catalogues 833/833.
- Les preuves exactes et inspectées 390x844 FR et 1600x900 EN sont sous
  `Docs/Product/Evidence/LivingHiveStrategicPath`.
- Rapport :
  `Docs/Product/LivingHive_StrategicPathPreviewMilestone_2026-07-22.md`.
- La synchronisation officielle de 10:53:14 UTC a échoué avant copie sur le
  partage hôte inaccessible; le jalon reste local sur `C:` sans contournement.

## Jalon courant — ambiance réactive de la ruche

- La ruche réagit désormais à deux faits fiables : amélioration active et
  production pleine. L'amélioration a priorité et la collecte reste manuelle.
- Mouvement réduit fige les halos; mode économie limite la couche à un signal.
  Aucun agent, timer, stock, niveau ou résultat n'est modifié.
- Appareil : rendu et préférences. Serveur : opération reconnue, bâtiment,
  pending, capacité, taux et UTC. Météo, soin distinct et alerte défense restent
  génériques ou cachés faute de snapshots suffisamment précis.
- Unity compile sans erreur C#; F8 passe 85 contrôles; compilation de secours :
  0 erreur/217 avertissements historiques; catalogues inchangés à 793/793.
- Les preuves exactes et inspectées 390x844 FR et 1600x900 EN sont sous
  `Docs/Product/Evidence/LivingHiveReactiveAmbience`.
- Rapport : `Docs/Product/LivingHive_ReactiveAmbienceMilestone_2026-07-22.md`.
- La synchronisation officielle de 10:19:53 UTC a échoué avant copie sur le
  partage hôte inaccessible; le jalon reste local sur `C:` sans contournement.

## Jalon courant — comportements spécialisés des abeilles

- Les abeilles déjà budgétées rendent maintenant leur rôle : transport de nectar,
  façonnage de cire, tri du pollen, soin générique du couvain et patrouille.
- La sélection renforce la boucle sans lancer d’opération. Mouvement réduit la
  fige; mode économie garde un accessoire. Aucune abeille supplémentaire n’est
  créée et les budgets restent 5/8, puis 3/5 en économie.
- Appareil : rendu et préférences visuelles. Serveur : bâtiment, ressource,
  pending, opération et UTC. Un sous-état non distingué reste générique ou caché.
- Unity compile sans erreur C#; F8 passe 80 contrôles; catalogues inchangés à
  793/793. Les preuves exactes 390x844 FR et 1600x900 EN sont sous
  `Docs/Product/Evidence/LivingHiveSpecializedBeeActivity`.
- Rapport : `Docs/Product/LivingHive_SpecializedBeeActivityMilestone_2026-07-22.md`.

## Jalon courant — activité contextuelle des bâtiments

- Les fiches de la réserve, de l’atelier, de l’entrepôt, de la nurserie et du
  poste de garde affichent maintenant une activité propre et un état issu des
  faits connus : production, nutrition, sécurité, amélioration ou formation.
- Le ruban vivant fonctionne en portrait et en paysage. Mouvement réduit le fige;
  mode économie le ramène à un seul signal, sans changer économie, files,
  progression ou abeilles affectées.
- Appareil : rendu et préférences visuelles. Serveur : stocks, pending, UTC,
  opérations et statuts officiels. Les snapshots existants suffisent; un sous-état
  ambigu reste générique plutôt que simulé.
- Unity compile sans erreur C#; F8 passe 75 contrôles; catalogues 793/793. Les
  preuves exactes et inspectées 390x844 FR et 1600x900 EN sont sous
  `Docs/Product/Evidence/LivingHiveBuildingActivity`.
- Rapport : `Docs/Product/LivingHive_BuildingActivityMilestone_2026-07-22.md`.

## Jalon courant — prévoir le prochain retour de production

- `Sac & stocks` affiche maintenant pending/capacité, taux horaire et temps avant
  saturation pour miel, cire et pollen, puis résume le premier stock qui sera
  plein.
- `Voir` conserve une cible de 44 px et ouvre uniquement le bâtiment : aucune
  collecte, aucun crédit HUD, aucune progression ou tâche complétée.
- Appareil : affichage et décompte dérivé. Serveur : UTC, catalogue, taux,
  capacités, pending et bâtiment autoritaires. Le snapshot existant suffit;
  aucune nouvelle route ni mutation n’est ajoutée.
- `HiveOfflineProduction:Enabled=false` reste fermé. Le pending durable par
  bâtiment reste une porte serveur explicite; le snapshot actuel est une
  projection bornée.
- Unity compile sans erreur C#; F8 passe 70 contrôles; catalogues 780/780. Les
  captures exactes et inspectées 390x844 FR et 1600x900 EN sont sous
  `Docs/Product/Evidence/LivingHiveProductionForecast`.
- Rapport : `Docs/Product/LivingHive_ProductionForecastMilestone_2026-07-22.md`.

## Jalon courant — confort et performance sur mobile

- `Plus -> Confort mobile` expose deux préférences appareil versionnées :
  mouvement réduit et mode économie.
- Le mouvement réduit fige les animations décoratives et révèle immédiatement
  les textes animés. Le mode économie réduit les abeilles d’ambiance de 5/8 à
  3/5 et supprime leurs traînées, sans jamais masquer les abeilles affectées.
- Appareil : rendu et `PlayerPrefs` local. Serveur : aucune autorité ni contrat;
  l’Intégrateur a ratifié l’absence d’endpoint ou de mutation nécessaire.
- Compilation Unity : 0 erreur C#. F8 : 65 contrôles réussis. Catalogues :
  769/769 clés. Les captures exactes et inspectées 390x844 FR et 1600x900 EN
  sont sous `Docs/Product/Evidence/LivingHiveMobileComfort`.
- Rapport : `Docs/Product/LivingHive_MobileComfortMilestone_2026-07-22.md`.

## Jalon courant — production manuelle après une absence

- La fermeture ou la mise en arrière-plan ne perd plus la production : un
  journal local `v1` restaure les pending miel/cire/pollen et accrédite uniquement
  les caches de bâtiments.
- Au retour, un bilan localisé montre les quantités reconnues. `Voir` ouvre le
  bâtiment sans collecter; le HUD ne change qu’après la collecte manuelle.
- Appareil : cache `PlayerPrefs` non protégé, partitionné par profil, 16 entrées,
  écriture toutes les 30 s et sur le cycle de vie, durée bornée à 12 h. Serveur :
  UTC, révision, catalogue, taux, capacités et quantités officiels.
- Recul d’horloge : zéro production et marqueur non régressif. Saut futur : durée
  bornée, puis capacité propre à chaque bâtiment.
- `HiveOfflineProduction:Enabled=false` reste fermé par défaut et en Production;
  aucune route, activation, notification, candidature ou opération de
  déploiement. Le noyau serveur passe 35/35; l’état durable par bâtiment reste
  une porte ouverte.
- Unity compile sans erreur C#; F8 passe 60 contrôles; catalogues 758/758.
  Captures exactes et inspectées 390x844 FR et 1600x900 EN sous
  `Docs/Product/Evidence/LivingHiveOfflineProduction`.
- Rapport : `Docs/Product/LivingHive_OfflineProductionMilestone_2026-07-22.md`.

## Jalon courant — progression persistante de la ruche

- Les résultats durables ne sont plus déduits du seul dernier élément de file :
  un journal local `v1` conserve ensemble plusieurs niveaux de bâtiments et les
  quatre effectifs de la preview.
- La reprise fusionne les anciens résultats complétés de façon monotone et
  idempotente. Une coupure entre l’écriture du résultat et le reçu de file ne
  double rien, et un ancien reçu ne peut pas diminuer un état plus récent.
- Appareil : cache borné, versionné, associé au profil et non protégé dans cette
  démo. Serveur : snapshot officiel joueur/ruche/monde/serveur, catalogue,
  révisions bâtiment/armée et valeurs complètes.
- `HiveProgressionSnapshot:Enabled=false` reste fermé par défaut et en
  Production; aucune route, activation, notification ou candidature de
  déploiement. Le noyau serveur passe 32/32 tests et le build sans erreur.
- Unity compile sans erreur C#; F8 passe 54 contrôles; BEE-925–930 et BEE-945–951
  restent verts. Les catalogues comptent 751/751 clés uniques et alignées.
- Deux preuves exactes et inspectées, 390x844 FR et 1600x900 EN, sont sous
  `Docs/Product/Evidence/LivingHiveProgress` avec cibles tactiles de 44 px.
- Rapport : `Docs/Product/LivingHive_PersistentHiveProgressMilestone_2026-07-22.md`.

## Jalon courant — boucle quotidienne mobile et reprise

- La troisième file n’est plus un faux état statique : `Danse des routes I` et `Rayons tempérés I` débitent miel/pollen une seule fois, occupent une file dédiée de 16 secondes, affectent deux abeilles, reprennent après redémarrage et se terminent exactement une fois.
- Les effets de preview sont complémentaires et non exclusifs : +2 % de production de miel et +5 % de capacité de cire. Aucun achat, raccourci premium, automatisation de collecte ou puissance militaire exclusive n’est ajouté.
- Portrait : entrée `Plus -> Recherche`, deux choix et cibles d’au moins 44 px en 390x844. Paysage : menu resserré, abeilles actives et troisième file réelle en 1600x900.
- Appareil : rendu, journal local `v2`, cache de complétion et future clé d’idempotence en attente. Serveur : catalogue autorisé, soldes, transaction, UTC, opération active, révision, idempotence, complétion et effets officiels.
- L’Intégrateur a livré le noyau serveur sous `LivingHiveResearch:Enabled=false`, sans route HTTP, candidat ni déploiement : 2/2 tests ciblés, 26/26 HiveOperations et build Release sans erreur.
- F8 Unity passe avec sortie 0 et zéro `error CS` dans `Artifacts/LivingHiveResearch_F8.log`. Trois preuves exactes sont ratifiées sous `Docs/Product/Evidence/LivingHiveResearch`.
- Les catalogues `fr-CA` et `en-US` comptent 683/683 clés uniques et alignées. La scène canonique, le terrain et l’image LivingHive conservent leurs empreintes.
- Rapport : `Docs/Product/LivingHive_ResearchVerticalSliceMilestone_2026-07-22.md`.

### Registre mobile Sac & stocks — réalisé

- Le bouton `Sac` ouvre maintenant un registre des stocks disponibles, productions en attente de collecte manuelle et coûts engagés dans les trois files.
- Les boutons `Voir` de 44 px naviguent vers réserve, atelier ou entrepôt sans collecter depuis le panneau; la collecte gratuite reste manuelle dans le bâtiment.
- L’appareil rend un aperçu local et pourra conserver au plus un dernier snapshot par joueur. Le serveur possède révision, ressources/capacités, recherches terminées et engagements; population/capacité population restent honnêtement `null` tant qu’un agrégat fiable manque.
- `HiveStockSnapshot:Enabled=false` reste fermé et aucune route HTTP n’est ouverte. Le noyau serveur passe 1/1 test ciblé, 27/27 HiveOperations et le build Release sans erreur.
- F8 et captures passent avec sortie 0 et zéro `error CS`; preuves exactes 390x844 et 1600x900 sous `Docs/Product/Evidence/LivingHiveLedger`.
- Les catalogues comptent 697/697 clés uniques et alignées. Rapport : `Docs/Product/LivingHive_HiveLedgerMilestone_2026-07-22.md`.

### Ronde quotidienne de la ruche — réalisée

- `Quêtes -> Ronde` regroupe trois gestes utiles sans attente artificielle : collecte manuelle réelle, lancement d’une opération réelle et consultation de `Sac -> Voir`.
- Les trois événements sont persistés une seule fois dans l’aperçu local; le joueur réclame ensuite explicitement 120 miel + 60 pollen. Aucun crédit automatique et aucun achat ne sont introduits.
- Le portrait 390x844 et le paysage 1600x900 affichent toute la ronde sans défilement, avec cibles de 44 px, raccourcis `Aller` vers les trois destinations exactes et séparation visible entre preview locale et autorité serveur UTC. Un raccourci ne valide jamais sa tâche; seul Quêtes reçoit `!` quand la récompense est réellement prête, sans badges Mail/Alliance/Plus simulés.
- Appareil : rendu, journal local `v1`, futur snapshot reconnu et outbox protégée. Serveur : jour UTC, faits vérifiés, révision, capacités, transaction atomique et reçu idempotent.
- `HiveDailyRound:Enabled=false` reste fermé par défaut et en Production; aucune route HTTP n’est ouverte. Le noyau serveur refuse une opération ancienne ou collectée comme preuve de lancement, passe 28/28 HiveOperations et le build Release sans erreur.
- F8 global et captures passent avec sortie 0 et zéro `error CS`; preuves exactes sous `Docs/Product/Evidence/LivingHiveDailyRound`.
- Les catalogues comptent 726/726 clés uniques et alignées. Rapport : `Docs/Product/LivingHive_DailyRoundMilestone_2026-07-22.md`.

### Retour à la ruche — réalisé

- Les trois files persistantes produisent maintenant un résumé de retour borné et non vide : opérations encore actives, ou terminées pendant l’absence avant leur application idempotente locale.
- `Voir` ouvre la destination exacte avec une cible de 44 px; aucune collecte, ressource, récompense ou tâche quotidienne n’est attribuée automatiquement.
- Appareil : rendu et dernier résumé reconnu. Serveur : identifiant, type, destination, statut, résultat, révision et UTC autoritaires.
- `HiveOperationResume:Enabled=false` reste fermé; aucune route HTTP ni notification push. Le noyau serveur passe 29/29 HiveOperations et le build Release sans erreur.
- F8 global et captures passent avec sortie 0 et zéro `error CS`; preuves exactes 390x844 FR et 1600x900 EN sous `Docs/Product/Evidence/LivingHiveQueueReturn`.
- Les catalogues comptent 735/735 clés uniques et alignées. Rapport : `Docs/Product/LivingHive_QueueReturnMilestone_2026-07-22.md`.

### Source des prérequis Recherche — réalisée

- Une étude bloquée indique maintenant la quantité exacte de miel ou pollen manquante; son bouton `Source` de 44 px ouvre la réserve de miel ou l’entrepôt de pollen correspondant.
- Cette navigation ne lance aucune étude, ne collecte aucune production et ne valide aucune tâche quotidienne; l’action normale revient dès que les ressources suffisent.
- Appareil : message et navigation depuis le dernier solde reconnu. Serveur : soldes, coûts, catalogue, éligibilité, transaction, révision et opération restent autoritaires.
- Aucun nouveau contrat serveur n’est requis pour cette navigation; le noyau `LivingHiveResearch:Enabled=false` reste fermé jusqu’au raccordement session/HTTP/cache protégé.
- F8 global et capture passent avec sortie 0 et zéro `error CS`; preuves inspectées en 390x844 FR et 1600x900 EN sous `Docs/Product/Evidence/LivingHiveResearch`.
- Les catalogues comptent 740/740 clés uniques et alignées. Rapport : `Docs/Product/LivingHive_ResearchPrerequisiteSourceMilestone_2026-07-22.md`.

## Cap produit

Faire de `LivingHive` un ecran d'accueil vivant, clair et tactile qui donne envie de revenir plusieurs fois par jour. La reference Ant Legion sert a identifier les boucles qui fonctionnent et les irritants a depasser. Bee Kingdom conserve sa propre direction artistique, son vocabulaire, son economie et ses decisions de conception.

## Fondations intouchables

* Conserver exactement la carte 50x50 et toutes ses images de terrain.
* Conserver l'image de base actuelle de la ruche.
* Construire la qualite par-dessus ces bases: animation, interfaces, retours d'action, simulation, son et progression.
* Le chantier chat et messagerie est repris par l'agent `Communication` dans un
  perimetre isole. Aucun branchement dans LivingHive n'est effectue sans handoff
  verifie avec l'agent `Architecte`.

## Principes de conception

1. La ruche demeure l'element principal. Les donnees detaillees n'apparaissent qu'apres une action volontaire.
2. Un batiment se reconnait par son illustration et son contour. Il ne porte pas d'icone d'identite permanente.
3. Une ressource prete se signale par une grande icone premium sans texte permanent.
4. Chaque action importante produit un resultat visuel immediat dans la ruche.
5. Le tutoriel guide une seule decision a la fois, raconte une consequence et remet toujours le joueur en controle.
6. Le tutoriel conserve les couleurs de la ruche: le guidage repose sur un halo, une abeille-guide et un voile chaud tres leger, jamais sur un ecran presque noir.
7. Les zones tactiles sont bornees aux illustrations visibles. Un panneau masque ne conserve jamais de zone active.
8. Les menus se recomposent selon le format; aucun texte ne se chevauche ou ne se fait couper.
9. Les achats vendent du confort, du temps et de l'esthetique. Ils ne vendent pas une puissance militaire impossible a rattraper.
10. Chaque tranche distingue explicitement ce qui reste sur l'appareil, ce qui n'est qu'un cache hors ligne et ce qui appartient a l'autorite serveur. Le mobile ne s'accorde jamais lui-meme une ressource, une progression ou une recompense de production.
11. Le chat est integre comme service mobile de premiere classe par l'agent `Communication`: interface responsive, transport, reprise, recus, traduction et persistance sont livres ensemble sous un contrat client/serveur explicite.

## Ordre de construction

### Phase 1 - Boucle quotidienne de ruche

Objectif: rendre satisfaisante la visite de quelques minutes.

* production locale de miel, pollen et cire
* collecte manuelle par clic sur le batiment
* capacite locale et stockage global
* retour anime de collecte et activite d'abeilles
* amelioration, entrainement et files persistantes
* panneaux contextuels compacts et fiables

Etat: la collecte manuelle, le soin du couvain, la premiere formation d'ouvriere, la premiere amelioration et une premiere intervention defensive sont implantes et couverts par des controles Unity. La nurserie consomme 300 miel par soin et gagne 22 points de nutrition de base, auxquels les preparations et affectations peuvent ajouter un bonus. Elle possede maintenant une stabilite locale mesurable. A 100 % de nutrition, elle permet de former une ouvriere pour 420 miel et 180 pollen dans une file de 14 secondes, tout en conservant l'amelioration du batiment. L'ouvriere n'est ajoutee qu'a la fin; la nutrition redescend alors de 40 points. L'atelier de cire passe du niveau 22 au niveau 23 pour 2 484 miel et 902 cire dans la vraie file de 18 secondes; sa production augmente alors de 4 %, de 1 180 a 1 227 par heure. La defense mobilise cinq gardiennes pendant 10 secondes sans cout ni consommation d'unite, puis fait passer la securite locale de 72 a 82. Les files restent locales mais leurs operations actives survivent maintenant au redemarrage.

### Phase 2 - Tutoriel scenarise

Objectif: enseigner par une courte crise de ruche, pas par une succession de fenetres explicatives.

* accueil de la reine et premier besoin de la colonie
* premiere collecte de miel
* nourrissage du couvain et formation d'ouvrier
* amelioration d'une chambre
* defense contre une menace naturelle
* ouverture guidee de la carte sans modifier son terrain
* premier vol de butinage et reclamation manuelle au retour
* objectifs individuels, puis recompense de chapitre

Chaque etape aura une cible unique, un guidage lumineux non interactif qui preserve les couleurs, une camera controlee et une reprise sure apres interruption.

Mise a jour prioritaire du chapitre 1: la synthese historique exposee ci-dessous est remplacee par l'etat mesure de la passe de rythme. La charte est maintenant suivie d'une mise en service sous charge, d'une collecte manuelle, de trois controles et d'un sceau durable; les chiffres du rapport reproductible font foi.

Etat: les sept premiers chapitres sont implantes et verifies de bout en bout. Le premier comporte maintenant dix objectifs lies, sept decisions, trois controles actifs et quatre collectes manuelles. Apres la premiere collecte, le joueur choisit une cadence reguliere avec trois ouvrieres pendant 22 secondes, 360 miel et +4 de stabilite du couvain, ou un renfort avec cinq ouvrieres pendant 14 secondes et 480 miel. La seconde recolte reste dans le batiment jusqu'au clic du joueur. Il faut ensuite repartir le miel: investir 120 miel dans le couvain pour conserver +8 de stabilite au chapitre 2, ou garder toute la reserve sans bonus. Deux circuits d'installation sont ensuite obligatoires. Chaque circuit combine une liaison vers la reserve avec trois porteuses pendant 18 secondes et 180 miel en attente, ou un relais de nurserie avec trois porteuses pendant 22 secondes, 140 miel en attente et +3 de stabilite; le miel doit etre recolte manuellement. Le joueur choisit ensuite une veille de ventilation gratuite avec deux ouvrieres pendant 16 secondes et +3 de stabilite, ou un scellement de cire avec deux batisseuses pendant 20 secondes, 45 cire et +1 % de production locale de miel. Apres les deux circuits, il valide separement le niveau de reserve, la circulation d'air et l'acces au couvain. La charte finale engage soit trois ouvrieres pendant 24 secondes et 80 miel pour +5 000 de capacite, soit deux nourrices pendant 28 secondes et 60 miel pour +4 de stabilite et +1 aux soins futurs. Les engagements sont uniques, les effets passent au chapitre 2, aucune option payante n'accorde de puissance et seule l'installation complete debloque la recompense de 250 miel. Le second cible la nurserie et comporte maintenant neuf objectifs lies: trouver la chambre, envoyer une nourrice pour un diagnostic de 6 secondes, stabiliser la chambre, choisir le soin, verifier separement la chaleur, la gelee royale et la respiration, orienter le developpement, accomplir deux tournees de soins, puis valider la routine. La stabilite part de 46, plus les bonus eventuellement acquis au chapitre 1. La ventilation gratuite affecte trois ouvrieres pendant 9 secondes, ajoute 8 de stabilite et 1 point au prochain soin; la doublure affecte deux batisseuses pendant 12 secondes, engage une seule fois 80 cire, ajoute 14 de stabilite et 3 points au soin. Le joueur voit le resultat avant de choisir entre un soin attentif avec deux nourrices pendant 12 secondes pour 24 points de base, ou un soin rapide avec quatre nourrices pendant 7 secondes pour 22 points de base. Le bonus de preparation est ajoute au resultat; le miel et la nutrition restent inchanges pendant le soin, puis sont appliques une seule fois a la fin. Les trois controles larvaires sont uniques et doivent tous etre effectues. Le plan Croissance engage une seule fois 60 pollen et reduit de 80 miel les deux rations du chapitre 3; le plan Resilience ne coute rien et ajoute 6 de stabilite. Apres cette decision, deux tournees operationnelles restent obligatoires avant la recompense. Pour chaque tournee, le joueur choisit une ration nectar de 16 secondes produisant 180 miel ou une ration pollen de 18 secondes produisant 140 pollen, puis doit la recolter manuellement sur le bon batiment. Il choisit ensuite une rotation de trois nourrices pendant 16 secondes, coutant 110 miel et 60 pollen pour +8 nutrition et +3 stabilite, ou une veille thermique de deux batisseuses pendant 13 secondes, coutant 45 cire pour +5 nutrition et +7 stabilite. Les couts ne peuvent etre engages deux fois, les gains arrivent a la fin et la seconde tournee est obligatoire. Le troisieme comporte maintenant dix objectifs lies. Il termine la nutrition par un choix entre une ration mesuree de 500 miel avec trois nourrices pendant 12 secondes, ou une ration urgente de 600 miel avec cinq nourrices pendant 7 secondes; ces couts deviennent 420 et 520 miel lorsque Croissance a ete choisie. Il annonce ensuite le vrai cout de 420 miel et 180 pollen, lance la file de 14 secondes et montre sa progression reelle. La population reste a 30 pendant le timer, puis passe a 31 a l'arrivee de l'ouvriere tandis que la nutrition redescend a 60 %. Avant l'affectation permanente, la nouvelle ouvriere doit essayer un poste. L'essai Reserve affecte une ouvriere pendant 10 secondes et produit 90 miel qui doit etre recolte manuellement dans le batiment. L'essai Nurserie affecte une ouvriere pendant 12 secondes et ajoute 5 de stabilite a la fin. L'essai ne verrouille pas l'affectation finale: la reserve ajoute 3 % a la production locale de miel, de 2 540 a 2 616 par heure, tandis que la nurserie ajoute 2 points aux futurs soins manuels. Le premier quart obligatoire produit ensuite 120 miel a recolter, ou 150 apres emergence renforcee, ou ajoute 5 a 7 points de stabilite en nurserie. L'ouvriere doit enfin reussir deux rondes de certification. Chaque ronde commence par une tache de reserve de 14 secondes produisant 140 miel a recolter manuellement, ou une tache de nurserie de 16 secondes ajoutant 5 de stabilite. Le joueur valide ensuite trois controles uniques, precision, cadence et securite, avant de choisir une autonomie de 18 secondes coutant 120 miel et 60 pollen pour +1 % de production locale, ou un mentorat de 20 secondes avec deux abeilles et 50 cire pour +1 aux futurs soins et +3 de stabilite. Les engagements sont uniques, les gains arrivent a la fin et la recompense reste verrouillee jusqu'aux deux rondes. Ce poste civil ne remplace pas le futur choix de classe de combat. Le quatrieme cible l'atelier de cire et comporte maintenant dix objectifs. Apres l'audit, la specialisation, l'amelioration reelle, le lot temoin et sa premiere application, deux commandes completes restent obligatoires. Chaque commande combine un choix de fabrication, une collecte manuelle, un choix de chantier et trois controles structurels uniques. Une doctrine finale ajoute soit de la production, soit de la capacite de cire. La recompense reste verrouillee jusqu'a cette certification. Le cinquieme fait apparaitre une fausse-teigne de cire haute definition, cible uniquement la caserne et commence par une reconnaissance de 6 secondes avec deux eclaireuses. Le joueur choisit ensuite une interception gratuite avec cinq gardiennes pendant 8 secondes et +10 de securite, ou un barrage avec quatre gardiennes pendant 12 secondes, un cout unique de 120 cire et +14 de securite. Les gardiennes ne sont pas consommees et cette defense ne pretend pas etre un combat serveur. Le sixieme enseigne le bouton contextuel unique Carte/Ruche, ouvre la vraie scene mondiale canonique 50x50, charge et cadre la ruche du joueur en C32_32, exige sa selection, puis reprend le meme chapitre apres le retour dans LivingHive. Le septieme cible le pollen existant `res_pollen_core`, envoie trois butineuses dans un vol local de 7,35 secondes, garde 20 pollen en attente, puis exige le retour dans LivingHive et une reclamation manuelle avant tout credit. La cible guidee est traitee avant les anciennes interactions de carte afin qu'aucune zone superposee ne vole le clic. Le guidage n'altere aucune tuile ni image de terrain et ne simule ni marche serveur ni butin persistant. Pendant les attentes guidees, seules les abeilles affectees a la tache restent animees dans le halo. Les clics hors cible sont bloques. Le voile de tutoriel existe uniquement pendant une etape guidee; sa fermeture ou le dernier ecran le retire immediatement, restaure les couleurs et rend le controle libre. La persistance apres redemarrage et les chapitres suivants restent a construire.

Mise a jour prioritaire du chapitre 2, qui remplace son ancien statut a neuf objectifs: il comporte maintenant treize objectifs, douze decisions, neuf controles actifs et deux collectes manuelles. Apres les deux tournees de soins, deux cohortes d'incubation doivent chacune passer une observation, trois releves uniques et un traitement. L'observation oppose un balayage de quatre abeilles pendant 12 secondes a une lecture precise de deux abeilles pendant 18 secondes et +2 stabilite. Le traitement oppose une rotation hygienique de 18 secondes et 35 cire pour +4 nutrition et +6 stabilite a un soutien de gelee de 22 secondes, 90 miel et 45 pollen pour +8 nutrition et +2 stabilite. Le protocole final engage soit trois abeilles pendant 26 secondes, 140 miel et 60 pollen pour renforcer le premier quart de la future ouvriere, soit deux abeilles pendant 30 secondes, 80 miel et 40 cire pour +8 stabilite et +1 aux soins futurs. La recompense reste verrouillee jusqu'a cette certification, les couts sont uniques et aucune option payante n'accorde de puissance.

Etat actuel de la passe de rythme: les chapitres 1 a 5 possedent maintenant des circuits operationnels avec decisions, controles et consequences mesurables. Le chapitre 1 comporte quatorze objectifs, onze decisions, douze controles actifs et six collectes manuelles pour 150 a 184 secondes de travail chronometre. Le chapitre 2 comporte quinze objectifs, treize decisions, douze controles actifs et trois collectes manuelles pour 180 a 224 secondes. Le chapitre 3 comporte treize objectifs, dix decisions, quinze controles actifs et jusqu'a cinq collectes manuelles pour 145 a 170 secondes. Le chapitre 4 comporte treize objectifs, neuf decisions, quinze controles structurels et quatre collectes manuelles pour 145 a 165 secondes. Le chapitre 5 comporte treize objectifs, treize decisions, douze controles et quatre collectes manuelles pour 166 a 208 secondes. L'Acte I atteint 786 a 951 secondes et reste explicitement sous sa cible finale.

Profil actif par objectif: une matrice reproductible couvre maintenant les 68 objectifs des chapitres 1 a 5 dans `Artifacts/OpeningActPacing/OpeningActObjectivePacing.md`. Elle distingue decisions, controles, collectes manuelles et temps chronometre sans pretendre mesurer la lecture, la navigation ou l'hesitation. La dotation fondatrice porte le chapitre 1 a 29 interactions, l'orientation active porte le chapitre 3 a 30, et le briefing/debrief tactique portent le chapitre 5 a 29. Les chapitres 2 et 4 partagent maintenant le plancher a 28 interactions sans ajout d'attente; le chapitre 2 constitue la prochaine tranche recommandee dans l'ordre de l'Acte I.

Passage de relais chapitre 1 vers chapitre 2: apres l'introduction de la nurserie, un bilan fonctionnel expose les trois controles de mise en service comme conformes et affiche les gains effectivement transportes par la route jouee: stabilite de depart, bonus aux soins, capacite de reserve et production locale de miel. Le joueur engage ensuite ce circuit avant de chercher la nurserie. Cette confirmation ne repete aucun controle, n'ajoute aucun minuteur et ne modifie pas les gains; elle rend leur persistance locale visible et compréhensible. Les deux routes sont couvertes par les crochets de preuve, et la route couvain est capturee en 390x844 et 1600x900 dans `Artifacts/GuidedBroodIncubation`.

Feedback immediat des routes du chapitre 1: la fin reelle d'un trajet direct declenche pendant au plus neuf secondes une pulsation ambree concentree sur la reserve, avec trois porteuses en orbite et le gain `+180 miel`. Le relais couvain trace plutot une liaison verte coudee de la reserve vers la nurserie, anime trois porteuses le long du trajet et annonce `+3 stabilite`. Les deux couches sont temporaires, respectent le mode mouvement reduit, ne laissent aucun pictogramme permanent et ne modifient ni timer, ressource ou image de fond. Les quatre rendus 390x844 et 1600x900 sont produits par le harnais `SandboxGuidedBroodIncubationCapture`.

Preuve visuelle du chapitre 1: les ecrans Mise en service et Charte sont captures automatiquement en 390x844 et 1600x900 dans `Artifacts/GuidedOpeningInstallation`. Les commandes tiennent dans le panneau, le `X` reste accessible et le voile de guidage a 0,12 conserve les couleurs de la ruche. L'outil refuse la preuve si Unity ne produit pas les dimensions demandees.

Validation du chapitre 2: les assemblages jeu et editeur compilent sans erreur avec la nouvelle incubation et ses tests. Le passage F8 complet a ete rejoue dans la VM le 20 juillet 2026 sous Unity 6000.5.3f1 et tous les controles LivingHive passent. Cette verification a aussi corrige une assertion QA restee sur l'ancien total de neuf objectifs alors que le runtime et le plan en exposent treize. Quatre preuves visuelles reproductibles couvrent maintenant le choix d'observation et la doctrine finale en 390x844 et 1600x900 dans `Artifacts/GuidedBroodIncubation`. Les titres, les deux choix et la fermeture restent visibles dans les deux formats; les boutons de doctrine indiquent explicitement le nombre d'abeilles. Le lanceur refuse toute capture dont les dimensions ne correspondent pas exactement a la preuve demandee.

Retour vivant du couvain valide le 21 juillet 2026: la nurserie expose maintenant des cellules larvaires pulsees, une fiche compacte nutrition/stabilite, quatre niveaux de vitalite et le temps restant d'un soin actif. La couche n'apparait que pendant le chapitre 2, une selection de la nurserie ou une activite qui la cible; elle respecte le mouvement reduit, ne capte aucun clic et ne modifie ni economie, ni progression, ni illustration de base. Le mobile rend uniquement le dernier instantane reconnu et son cache reste borne, partitionne par joueur et non autoritaire. La lecture serveur v6 authentifiee est ratifiee derriere `BroodVitality:Enabled=false`: migrations/repository 20/20, HTTP 2/2 avec isolation de deux joueurs, suite serveur 255 reussis et 7 SQL ignores, build Release 0 erreur. Aucun endpoint de mutation n'existe et le candidat courant precede v6. La preview reste explicitement locale et non officielle jusqu'au raccordement du shell mobile, de l'adaptateur HTTP et du cache protege. Compilation et suite LivingHive: `Artifacts/BroodVitalityFinalF8.log`, 0 `error CS`. La campagne `Artifacts/GuidedBroodIncubation` produit 20 PNG sur 20; les preuves dediees `Chapter2_NurseryVitalityCare_390x844.png` et `Chapter2_NurseryVitalityCare_1600x900.png` ont ete inspectees a resolution native avec le soin visible a 42 %. Le rapport complet est `Docs/Product/LivingHive_BroodVitalityFeedbackMilestone_2026-07-21.md`.

Mise a jour du chapitre 4: le joueur fait auditer l'atelier par deux batisseuses pendant 6 secondes avant de choisir sa specialisation. Apres l'amelioration de 18 secondes, deux batisseuses produisent un lot d'etalonnage pendant 10 secondes. Les 120 cire restent dans l'atelier et doivent etre recoltees manuellement. Le joueur affecte ensuite 80 cire de ce lot a la reserve avec deux batisseuses pendant 10 secondes pour +1 % de miel, ou a la nurserie avec trois batisseuses pendant 12 secondes pour +4 de stabilite. Il doit ensuite livrer deux commandes completes. Chaque commande commence par le recyclage de rayons avec deux batisseuses pendant 13 secondes pour 90 cire, ou par le pressage de feuilles neuves avec trois batisseuses pendant 16 secondes, 70 miel et 120 cire. La cire doit etre recoltee manuellement, puis 80 cire sont deployees vers la reserve pendant 15 secondes pour +1 % de miel, ou vers la nurserie pendant 17 secondes pour +4 de stabilite. Adhesion, ventilation et charge doivent etre controlees une fois par commande. Apres six controles, la doctrine Precision engage 60 miel et 40 cire pendant 18 secondes pour +1 % de production de cire; Cadence engage 80 miel et 30 pollen pendant 14 secondes pour +5 % de capacite. A la fin de cette preparation, le joueur valide separement le gabarit, la releve et la tracabilite. Le bonus et le choix persistant ne sont inscrits qu'au troisieme controle unique. La recompense reste verrouillee jusqu'a cette certification et aucune option payante n'accorde de puissance.

Mise a jour du chapitre 5: apres l'inspection du poste de garde, deux eclaireuses analysent la fausse-teigne pendant 6 secondes. Le joueur choisit ensuite l'interception ou la barriere de cire, puis doit organiser l'apres-combat. Le nettoyage mobilise trois ouvrieres pendant 9 secondes, recupere 60 cire et ajoute 2 securite; la releve mobilise deux gardiennes pendant 12 secondes et ajoute 5 securite. Enfin, une patrouille gratuite mobilise trois gardiennes pendant 10 secondes pour +3 de securite, ou un sceau de propolis mobilise deux batisseuses pendant 12 secondes, coute 70 cire et ajoute 6 de securite. Le chapitre expose maintenant sept objectifs lies et trois decisions dont les consequences restent mesurables.

Deuxieme mise a jour du chapitre 5: apres la protection durable, le joueur doit accomplir deux circuits complets de commandement. Chaque circuit demande un choix de production miel/pollen, deux collectes manuelles dans les batiments, un choix de renouvellement entre ouvriere et gardiennes, puis un choix de securisation entre ronde gratuite et scellement de cire. Le premier circuit enseigne la boucle; le second la fait repeter avec de nouveaux choix. La recompense reste verrouillee jusqu'aux deux passages. Le chapitre comporte maintenant dix objectifs, neuf decisions au total et quatre collectes manuelles. Les couts sont engages une seule fois, les gains de population arrivent apres leur file et aucune option payante n'accorde de puissance.

Troisieme mise a jour du chapitre 5: apres les deux circuits, le joueur signe un mandat pour la premiere expedition. Le corridor eclaireur mobilise trois eclaireuses pendant 14 secondes et ajoute 6 pollen au butin local du chapitre 7. L'escorte mobilise deux gardiennes pendant 16 secondes, ajoute 4 pollen au butin et 2 securite. Les deux routes sont gratuites; cap, rappel et releve doivent etre controles separement. Le mandat est persistant, le bonus est applique avant la reclamation manuelle et aucune option n'achete de puissance exclusive.

Regle de rythme ajoutee apres essai joueur: ces sept chapitres sont des tranches verticales fonctionnelles, mais les cinq premiers sont trop courts pour former la campagne d'ouverture finale. Le chapitre 1 vise environ 3 a 5 minutes; les chapitres 2 a 5 visent 7 a 12 minutes chacun. Le profil reproductible mesure actuellement 786 a 951 secondes de travail temporise obligatoire pour l'ensemble des chapitres 1 a 5, contre une cible d'Acte I de 1 860 a 3 180 secondes. Le chapitre 1 represente 150 a 184 secondes, le chapitre 2 180 a 224 secondes, le chapitre 3 145 a 170 secondes, le chapitre 4 145 a 165 secondes et le chapitre 5 166 a 208 secondes. Les chapitres transmettent des consequences economiques ou operationnelles aux etapes suivantes, sans gonfler artificiellement leurs minuteurs. L'ecart global est donc toujours confirme et la cible n'est pas atteinte.

Premiere couche de continuite livree: les chapitres 1 a 5 forment maintenant l'Acte I avec un compteur persistant sur cinq jalons. Chaque fin de chapitre expose une recompense distincte a reclamer avant la transition. Le menu `Quetes` est fonctionnel et presente les cinq objectifs lies, leur etat reclame/en cours/a venir, le sous-objectif courant des chapitres 2 a 5 et les totaux cumules de miel, cire et pollen. Il se ferme par son `X` ou par un second clic sur `Quetes`. Cette couche corrige la lecture en micro-tutoriels, sans pretendre que les durees finales sont deja atteintes.

Premiere passe de comprehension livree au chapitre 4: une introduction plein ecran, illustree par une banniere originale d'abeilles batisseuses, explique le miel comme energie, la cire comme materiau de construction et le pollen comme ressource de croissance. Elle ne fait pas progresser silencieusement l'objectif lorsqu'elle se ferme. Les choix economiques emploient desormais `-` pour les couts et `+` ou `produit` pour les gains; les commandes distinguent le miel depense, la cire fabriquee et la cire encore en attente d'une collecte manuelle. Le blocage de `Commande 1/2` a aussi ete corrige en autorisant explicitement les boutons de choix et les trois controles structurels dans le filtre d'entree guide, avec preuves paysage et portrait. Cette page constitue le modele reutilisable pour les futures introductions narratives des autres chapitres; elle n'altere ni l'image de base de la ruche, ni la carte 50x50, ni ses images de terrain.

Le lanceur d'aperçu du chapitre 4 fonctionne maintenant depuis le mode Edition et le mode Play. Il conserve la demande pendant le changement de scene, attend la fin de l'initialisation normale de `LivingHive`, puis applique l'etat cible. Cela empeche le demarrage du chapitre 1 d'ecraser l'aperçu selectionne et rend aussi accessibles directement les etapes ciblees `Test Batch`, `Structural Checks`, `Supply Choice` et `Doctrine Choice`.

### Phase 3 - Ruche vivante premium

Objectif: faire ressentir une colonie active meme lorsque le joueur ne commande rien.

* trajets courts d'ouvrieres entre chambres
* nourrices autour du couvain
* ventilation, stockage, nettoyage et gardiennes
* bannieres de menus animees selon le batiment
* changements d'ambiance lies a la meteo, aux alertes et aux ameliorations
* niveaux de detail et cadence adaptes aux performances mobiles

Etat: une premiere tranche est implantee. Des abeilles haute definition circulent maintenant dans les zones actives, avec une cadence liee au remplissage des producteurs, un budget de huit personnages en paysage et cinq en portrait, et aucun trafic dans les zones verrouillees ou serveur. La collecte ajoute un transporteur temporaire, le nourrissage declenche un groupe local de nourrices et les minuteurs guides conservent uniquement les abeilles de la tache dans le halo. Le diagnostic montre une nourrice; la ventilation montre trois ouvrieres; la doublure de cire montre deux batisseuses. La premiere intervention defensive deploie cinq gardiennes visibles entre la caserne et une fausse-teigne animee. Les autres comportements specialises restent a enrichir.

### Phase 4 - Progression et choix de classe

Objectif: donner une identite strategique claire au compte.

* choix force d'une classe Bee Kingdom au moment narratif approprie
* triangle de forces lisible inspire du principe melee/haste/range, avec noms et roles propres aux abeilles
* arbres de recherche, unites et bonus qui renforcent le choix sans rendre les autres voies inutiles
* seconde classe debloquable vers le niveau 100 pour renouveler la progression
* changements visibles de la ruche a chaque palier majeur

### Phase 5 - Monde, alliances et activites

Objectif: relier la ruche a un monde persistant sans brouiller les commandes.

* recherche et chasse sur la carte existante
* marches, occupation, rapports et defense
* federation de ruches, aide, dons et evenements cooperatifs
* aventure scenarisee, arene et defis quotidiens
* chat et messagerie serveur integres au jeu par l'agent `Communication`, avec interface mobile et reprise reseau

### Phase 6 - Economie commerciale equitable

Objectif: proposer des achats desirables tout en gardant la competition rattrapable.

* Butineuses intendantes: collecte automatique au meme rendement et avec les memes plafonds
* files supplementaires temporaires
* accelerateurs aussi obtenables en jouant
* cosmetiques de reine, de chambres, d'abeilles et d'effets de ruche
* passes d'evenement avec voie gratuite substantielle
* offres contextuelles affichant duree, contenu, valeur et alternative gratuite

Interdits: unite militaire payante exclusive, bonus de combat permanent sans plafond, ressource essentielle inaccessible par le jeu ou offre qui masque volontairement son cout reel.

## Definition de termine pour chaque tranche

* comportement jouable de bout en bout
* aucune alteration des fondations visuelles protegees
* controle automatise du calcul ou de l'etat critique
* verification visuelle en format paysage et portrait pertinent
* zones tactiles conformes aux elements visibles
* fermeture, erreur, capacite pleine et etat vide verifies
* frontiere appareil/cache/serveur documentee et autorite economique testee contre le rejeu
* documentation de la nouvelle regle de jeu

## Prochaine tranche recommandee

La mesure par objectif est instrumentee, le chapitre 1 conclut maintenant son installation par une dotation fondatrice a valeur egale, le chapitre 3 oriente activement la première ouvrière puis transmet une consequence economique reelle a l'atelier, le chapitre 4 possede une certification finale sous contrainte et le chapitre 5 transmet un debrief tactique puis un briefing persistant. Les choix structurants sont conserves dans un profil local versionne pour la preview; l'autorite de production migre progressivement vers le serveur. Les chapitres 2 et 4 partagent le plancher a 28 interactions actives; le chapitre 2 constitue la prochaine tranche recommandee dans l'ordre de l'Acte I. Conserver la cible de 3-5 minutes pour le chapitre 1 et de 7-12 minutes pour les chapitres 2 a 5; ne pas ajouter de chapitre 8 avant cette fondation.

## Profil strategique local versionne - 2026-07-20

Le profil local `v12` possede un identifiant stable et une revision incrementale. Il migre automatiquement les JSON `v1` a `v11` et conserve seize decisions structurantes, de la charte et la dotation d'ouverture au briefing de sortie. Le mandat ajoute 6 pollen, ou 4 pollen et 2 securite, au premier vol. Le briefing ajoute soit le repere C32_32 au premier parcours mondial, soit 3 securite et un retour garde. Les effets structurels sont recalcules depuis les choix et appliques par delta; les gains de certification operationnels restent des champs distincts et aucun redemarrage ne rejoue une recompense.

Le panneau joueur rend ces seize decisions lisibles sous cinq domaines: charte, couvain, ouvrieres, atelier et defense. Il s'ouvre aussi depuis l'en-tete de ruche en portrait. Les libelles passent par les catalogues `fr-CA` et `en-US`, maintenant alignes a 466 cles. Ce profil demeure un cache de preview locale et non officielle. En production, le serveur valide l'eligibilite, verrouille le choix de dotation, accorde la recompense atomiquement et renvoie les soldes autoritaires; le mobile ne conserve que l'affichage, le cache et la cle d'idempotence de reprise.

## Checkpoint transactionnel du tutoriel - 2026-07-20

Chaque changement d'objectif enregistre le chapitre actif et l'objectif interrompu, mais la valeur restaurable reste volontairement la frontiere de debut du chapitre. Le checkpoint conserve les ressources, la capacite, la population ouvriere, la nutrition, la stabilite, la securite et les jalons deja acquis a cette frontiere. Au redemarrage, Bee Kingdom restaure ces valeurs exactes et relance l'introduction du chapitre. Il ne reprend jamais au milieu d'un timer situe apres un debit et avant un gain. Aucun cout n'est rejoue, aucune recompense n'est accordee par la restauration et les chapitres precedents restent marques accomplis.

Une banniere localisee `Reprise securisee` explique que le chapitre repart de son debut et indique l'etape interrompue sous une forme joueur. Cette politique prefere une petite reprise de jeu explicite a un etat economique ambigu. Elle est locale et non officielle; elle ne constitue pas encore la sauvegarde complete du compte.

## Persistance locale des files - 2026-07-20

Les files d'amelioration et de formation ecrivent desormais un journal local versionne au moment exact de l'engagement. Chaque operation conserve un identifiant unique, sa cible, son cout detaille et ses bornes UTC. Au redemarrage, la preview reconstruit le temps restant sans remettre le minuteur a zero. L'achevement revendique d'abord le resultat dans le journal, puis applique la valeur finale exacte; un second chargement ne peut donc pas ajouter un niveau ou un groupe de troupes une deuxieme fois. Cette persistance est explicitement locale et non officielle. Elle ne remplace ni la future sauvegarde de compte ni l'autorite serveur.

La reprise est maintenant visible dans une carte compacte, fermable et localisee. Elle distingue chaque operation restauree, montre sa cible et son temps restant actualise, puis rappelle `Sauvegarde locale · non officielle`. Le rail paysage continue d'afficher les memes minuteurs reels; le format portrait obtient la meme information dans la carte de retour sans ajouter de pictogramme permanent sur la ruche. Les preuves sont `QueueResume_390x844.png` et `QueueResume_1600x900.png`.

## Fondation multilingue et file reelle - 2026-07-20

Chacun des sept chapitres possede maintenant une introduction plein ecran originale, avec banniere propre, fade-in, texte progressif, revelation immediate au premier clic, puis fade-out. Les recits, titres, cartes explicatives, commandes, feedbacks des routes et de la dotation du chapitre 1, l'orientation de la première ouvrière, les transmissions entre chapitres, la validation de doctrine, le debrief tactique, le mandat et le briefing de sortie, ainsi que le profil strategique proviennent de catalogues JSON `fr-CA` et `en-US` contenant les memes 466 cles. Les noms, roles et actions des quatorze batiments LivingHive, les ressources principales, la navigation commune, la file et les commandes de traduction du chat partagent la meme fondation. Les pistes Suno utilisent un identifiant de chapitre suffixe par langue. Voir `Docs/Product/BeeKingdom_Localization.md` et `Docs/Audio/TutorialChapterNarration_Suno.md`.

La file gauche ne contient plus d'exemples statiques. La construction suit le batiment exact en amelioration, l'entrainement suit le type exact en formation et la recherche reste explicitement inactive tant qu'aucune vraie operation de recherche n'existe. Une file libre n'affiche ni progression ni faux compte a rebours.
### Jonction atelier vers defense - realise

- Le chapitre 4 ne se ferme plus sur une validation abstraite: une derniere commande prepare concretement le chapitre 5.
- Boucliers de cire: 3 batisseuses, 15 s, 90 cire produite et collectee manuellement, puis remise persistante de 60 cire sur le premier barrage.
- Grille ventilee: 2 batisseuses, 17 s, 70 cire produite et collectee manuellement, puis +4 securite persistante.
- Trois controles uniques verrouillent la livraison: fixations, aeration et passage des gardiennes.
- Profil strategique v7, migration v1-v6 et application idempotente par delta.
- Chapitre 4: 13 objectifs, 25 interactions actives, 145-165 s. Acte I: 62 objectifs, 730-885 s chronometres.
- Aucun achat de puissance, aucune automatisation de collecte, aucune modification du terrain ou de l'image de base LivingHive.

### Ravitaillement d'ouverture vers le couvain - realise

- Le chapitre 1 se termine par une préparation concrète du chapitre 2, avant la récompense.
- Réserve de gelée royale: 2 nourrices, 14 s, 60 pollen collecté manuellement, puis +2 durable aux soins du couvain.
- Escorte thermique: 3 ouvrières, 12 s, 120 miel collecté manuellement, puis +3 stabilité durable.
- Fraîcheur, quantité et passage vers la nurserie sont trois contrôles uniques.
- Profil stratégique v8, migration v1-v7 et application idempotente par delta.
- Chapitre 1: 14 objectifs, 25 interactions actives, 150-184 s. Acte I: 64 objectifs, 742-899 s chronométrés.
- Le chapitre 2 devient la prochaine tranche recommandée parmi les chapitres à 23 interactions actives.

### Préparation du couvain vers la première ouvrière - réalisée

- Après le protocole d'incubation, le chapitre 2 prépare réellement l'émergence avant sa récompense.
- Ration d'émergence: 3 nourrices, 14 s, 100 miel collecté manuellement, puis remise durable de 60 miel sur le soin final du chapitre 3.
- Opercule renforcé: 2 bâtisseuses, 16 s, 60 cire collectée manuellement, puis remise durable de 40 cire sur le renforcement de l'opercule.
- Opercule, ration et signal d'émergence sont trois contrôles uniques.
- Profil stratégique v9, migration v1-v8 et application idempotente des remises.
- Chapitre 2: 15 objectifs, 28 interactions actives, 180-224 s. Acte I: 66 objectifs, 756-915 s chronométrés.
- Le chapitre 3 devient la prochaine tranche recommandée à 23 interactions actives.

### Passation technique de la première ouvrière - réalisée

- Après la liaison économique vers l'atelier, le chapitre 3 exige une passation technique avant d'accorder sa récompense.
- Gabarit de calibration: 2 ouvrières, 15 s, puis +40 cire au lot témoin du chapitre 4, soit 160 au lieu de 120.
- Trousse d'application: 3 ouvrières, 18 s, puis remise durable de 40 cire sur la première application du chapitre 4, soit 40 au lieu de 80.
- Gabarit, outils et relève sont trois contrôles uniques; répéter un contrôle ne fait pas progresser la passation.
- Profil stratégique v10, migration v1-v9 et restauration idempotente des deux effets économiques.
- Chapitre 3: 13 objectifs, 27 interactions actives, 145-170 s. Acte I: 67 objectifs, 771-933 s chronométrés.
- Le chapitre 5 devient la prochaine tranche recommandée à 23 interactions actives.
- Aucune collecte automatique, aucun achat de puissance, aucune modification du terrain ou de l'image de base LivingHive.

### Briefing de sortie vers le territoire - réalisé

- Après l'autorisation du mandat, le chapitre 5 exige une préparation distincte avant sa récompense.
- Balise solaire: 3 éclaireuses, 15 s, puis repère `C32_32` affiché pendant la première localisation de la ruche au chapitre 6.
- Retour gardé: 2 gardiennes, 18 s, puis +3 sécurité et rappel du couloir protégé pendant le premier retour mondial.
- Deux situations simulées enseignent le recalage du cap et la conservation du groupe. Une mauvaise réponse ne consomme rien, ne fait pas avancer l'étape et invite à réessayer.
- Profil stratégique v11, migration v1-v10 et restauration idempotente des deux conséquences.
- Chapitre 5: 13 objectifs, 29 interactions actives après le débrief tactique, 166-208 s. Acte I: 68 objectifs, 786-951 s chronométrés.
- Après les validations actives de doctrine, de charte et de débrief, le chapitre 3 devient seul plancher à 27 interactions et la prochaine tranche recommandée.
- Aucune collecte automatique, aucun achat de puissance, aucune modification du terrain 50x50 ou de l'image de base LivingHive.
- Validation VM du 21 juillet 2026: compilation Unity globale et suite LivingHive réussies dans `Artifacts/DefenseTacticalDebriefFinalF8.log`; rapport de rythme régénéré dans `Artifacts/OpeningActPacing/OpeningActObjectivePacingDebriefGeneration.log`; 46 captures sur 46 et manifeste produits dans `Artifacts/GuidedOpeningInstallation`. Les écrans du briefing, de doctrine, de ratification et de débrief ont été inspectés en 390x844 et 1600x900 sans texte coupé ni collision de commande. La validation utilise Unity 6000.5.4f1, seule version installée dans la VM; la cible prescrite reste 6000.5.3f1.

### Validation active de la doctrine d'atelier - réalisée

- Après la préparation temporisée de Précision ou Cadence, trois contrôles uniques sont obligatoires: gabarit, relève et traçabilité.
- Un contrôle répété ne compte pas. Le bonus de production ou de capacité et le choix `workshop_doctrine` restent absents du profil jusqu'au troisième contrôle.
- Aucun délai artificiel n'est ajouté: le chapitre conserve 13 objectifs et 145-165 s, mais passe de 25 à 28 interactions actives et de 12 à 15 contrôles structurels.
- Les 16 libellés de cette tranche sont localisés dans les catalogues `fr-CA` et `en-US`; après la ratification de charte, ces catalogues contiennent chacun 436 clés uniques et strictement alignées.
- Preuves visuelles: `Chapter4_OperationsDoctrineChecks_390x844.png` et `Chapter4_OperationsDoctrineChecks_1600x900.png` dans `Artifacts/GuidedOpeningInstallation`; les trois commandes, l'explication et le compteur `0/3` restent entièrement visibles.
- La carte canonique 50x50, ses images et l'image de base LivingHive sont inchangées.

### Ratification active de la charte - réalisée

- Après la préparation de Réserve sûre ou Pont couvain, trois contrôles uniques sont obligatoires: registre, priorité de la nurserie et signal d'alerte.
- Un contrôle répété ne compte pas. Le gain de capacité ou de stabilité/soin et le choix `opening_charter` restent absents du profil jusqu'au troisième contrôle.
- Aucun délai artificiel n'est ajouté: le chapitre conserve 14 objectifs et 150-184 s, mais passe de 25 à 28 interactions actives et de 9 à 12 contrôles.
- Les 17 libellés de cette tranche sont localisés. Après le débrief tactique, les catalogues `fr-CA` et `en-US` contiennent chacun 444 clés uniques et alignées.
- Preuves visuelles: `Chapter1_CharterRatification_390x844.png` et `Chapter1_CharterRatification_1600x900.png` dans `Artifacts/GuidedOpeningInstallation`; le compteur `0/3`, l'explication et les trois commandes restent entièrement visibles.
- Avec le débrief tactique livré ci-dessous, le chapitre 3 devient seul plancher à 27 interactions et la prochaine tranche recommandée.
- La carte canonique 50x50, ses images et l'image de base LivingHive sont inchangées.

### Débrief tactique après la première défense - réalisé

- Après Interception ou Barrage, trois contrôles uniques sont obligatoires: brèche, passage du couvain et ligne de ravitaillement.
- La recommandation dépend de la riposte: relève après Interception, nettoyage après Barrage. Elle reste informative et ne retire pas l'autre choix.
- Aucun délai, coût ou gain artificiel n'est ajouté. Le chapitre conserve 13 objectifs et 166-208 s, mais passe de 26 à 29 interactions actives et de 9 à 12 contrôles.
- Les 8 nouveaux libellés portent les catalogues `fr-CA` et `en-US` à 444 clés uniques et alignées.
- Preuves visuelles: `Chapter5_TacticalDebrief_390x844.png` et `Chapter5_TacticalDebrief_1600x900.png` dans `Artifacts/GuidedOpeningInstallation`.
- Le chapitre 3 devient seul plancher à 27 interactions actives et la prochaine tranche recommandée.
- La carte canonique 50x50, ses images et l'image de base LivingHive sont inchangées.

### Orientation active de la première ouvrière - réalisée

- Après l'arrivée de la première ouvrière, le joueur observe séparément ses ailes, ses corbeilles à pollen et sa réponse au signal de relève. Un doublon ne compte jamais deux fois.
- L'operculation renforcée recommande l'essai à la réserve; l'émergence naturelle recommande l'essai en nurserie. Cette recommandation reste informative et les deux essais demeurent disponibles.
- Aucun délai, coût, gain ou choix persistant artificiel n'est ajouté. Le chapitre conserve 13 objectifs et 145-170 s, mais passe de 27 à 30 interactions actives et de 12 à 15 contrôles.
- Les 8 nouveaux libellés portent les catalogues `fr-CA` et `en-US` à 452 clés uniques, strictement alignées.
- Validation VM sous Unity 6000.5.3f1: compilation globale et suite LivingHive réussies dans `Artifacts/WorkerOrientationFinalF8.log`; rapport régénéré dans `Artifacts/OpeningActPacing/OpeningActObjectivePacingOrientationGeneration.log`; 48 captures sur 48, toutes aux dimensions attendues, dans `Artifacts/GuidedOpeningInstallation`.
- Preuves inspectées à résolution native: `Chapter3_WorkerOrientation_390x844.png` et `Chapter3_WorkerOrientation_1600x900.png`; titre, explication, compteur `0/3`, trois commandes et fermeture restent lisibles sans collision.
- Les chapitres 1, 2 et 4 partagent le plancher à 28 interactions; le chapitre 1 devient la prochaine tranche recommandée dans l'ordre de l'Acte I.
- La carte canonique 50x50, ses images et l'image de base LivingHive sont inchangées.

### Dotation fondatrice du chapitre 1 - réalisée

- Après le ravitaillement et ses trois contrôles, le joueur choisit explicitement la dotation de départ: `Réserve` accorde 250 miel; `Fondation mixte` accorde 170 miel et 80 pollen.
- Les deux routes valent exactement 250 ressources. Le choix est unique, la seconde tentative est ignorée et aucune option payante, minuterie ou puissance exclusive n'est ajoutée.
- Le profil de preview migre en `v12`, conserve `opening_reward` et restaure le choix sans rejouer le gain. En production, ce profil est un cache mobile: l'autorité d'éligibilité, l'attribution atomique, l'anti-rejeu et les soldes appartiennent au serveur.
- Le chapitre conserve 14 objectifs et 150-184 s, passe de 28 à 29 interactions actives et de 10 à 11 décisions. Les chapitres 2 et 4 partagent le plancher à 28 interactions; le chapitre 2 devient la prochaine tranche recommandée.
- Les catalogues `fr-CA` et `en-US` contiennent chacun 466 clés uniques, strictement alignées.
- Validation VM sous Unity 6000.5.3f1: compilation globale et suite LivingHive réussies dans `Artifacts/FoundationRewardFinalF8.log`; rapport régénéré dans `Artifacts/OpeningActPacing/OpeningActObjectivePacingRewardGeneration.log`; 50 captures sur 50 validées aux dimensions attendues dans `Artifacts/GuidedOpeningInstallation`.
- Preuves dédiées inspectées à résolution native: `Chapter1_FoundationReward_390x844.png` et `Chapter1_FoundationReward_1600x900.png`. Le premier rendu portrait a révélé une ligne masquée; la hauteur du panneau mobile a été corrigée, puis le texte, les deux choix et la fermeture ont été ratifiés sans collision.
- La carte canonique 50x50, ses images et l'image de base LivingHive sont inchangées.

### Frontière mobile, serveur et chat - règle permanente

- Appareil: rendu, interactions tactiles, brouillon de chat, cache récent, état de défilement, choix optimiste et clé d'idempotence en attente.
- Serveur: authentification, appartenance aux canaux, ordre et persistance des messages, reçus, traduction autorisée, modération, éligibilité de progression, transaction économique et soldes autoritaires.
- Reprise réseau: une même clé d'idempotence retourne le même résultat; une commande contradictoire est refusée; le mobile remplace toujours son état optimiste par la réponse serveur.
- L'agent `Intégrateur de production` possède les ajouts dans `Server/`. L'agent `Communication` possède les modules, tests, documents et interface de chat; l'Architecte coordonne seulement les points d'ancrage LivingHive.

### Contrats mobiles préparés le 21 juillet 2026

- Dotation fondatrice serveur: commande authentifiée et idempotente préparée sous `/game/v1/hives/{hiveId}/chapter-1/foundation`, avec preuve persistée et migration JSON/SQL. Le modèle de ruche passe en v5 avec un noyau de certification tutorielle strictement ordonné. Le téléphone ne transmet que l'étape observée, la révision attendue et une clé d'idempotence; le serveur conserve la progression monotone et une preuve finale opaque. Cette preuve ne certifie ni l'économie ni les opérations temporisées et laisse toujours `InstallationComplete=false`. Le drapeau `FoundingFoundation:Enabled` reste donc fermé par défaut et dans Production: aucun crédit officiel n'est activable.
- Chat LivingHive: entrée paysage de 44 px et entrée portrait `Plus -> Communication` de 50 px, overlay borné entre HUD et rail, commandes d'au moins 44 px, cache récent persistant/protégé/partitionné et borné, envoi optimiste, outbox, reprise, non-lus, reçus et traduction. Fermer l'overlay conserve la connexion et le brouillon; seul le logout ou changement de joueur purge la session volatile. Le cache ne conserve que les messages confirmés de la conversation sélectionnée et se réconcilie avec le serveur.
- Le mobile ne possède pas encore de shell d'authentification de production capable d'appeler `LivingHiveChatBootstrap.ActivateAsync`. L'interface affiche donc honnêtement `NotConfigured`, sans compte, message, canal, badge ou statut fictif.
- Preuves ciblées: Communication 131/131, cache recent protege 138/138, contrat DTO serveur 18/18, HiveOperations 20/20, BroodVitality HTTP 2/2 et endpoint fondation 2/2. La suite serveur contemporaine passe 255 tests, avec 7 tests SQL externes différés; la build Release passe avec 0 erreur et 2 avertissements SqlClient préexistants. Le candidat local `BeeKingdom.Server.20260721T225554Z` passe son manifeste de 55 fichiers et son smoke Healthy, mais il précède le modèle v6 et reste `DeploymentAuthorized=false`; aucun nouveau candidat n'a été construit. Les catalogues `fr-CA` et `en-US` contiennent 477 clés alignées.
- Validation Unity finale du chat sous 6000.5.3f1: compilation globale et F8 réussis dans `Artifacts/LivingHiveChatFinalF8.log`, avec sortie 0 et aucun `error CS`; les trois tests Unity de disposition passent 3/3 dans `Docs/WorldMapCommunication/Evidence/LivingHiveChat/LivingHiveChat_LayoutTests.xml`. Les preuves `NotConfigured` ont été inspectées à résolution native en 390x844 et 1600x900, sans collision tactile ni contenu fictif. Le harnais a d'abord refusé deux rendus erronés, 390x823 puis 3840x2160; aucun recadrage ou redimensionnement n'a été utilisé pour les contourner.

### Lecture active de la vitalité du couvain - réalisée

- Après chaque observation d'incubation, le joueur compare nutrition et stabilité; la mesure la plus basse devient la priorité, avec nutrition prioritaire en cas d'égalité.
- Une erreur ne consomme ni ressource ni temps, ne déverrouille pas les contrôles et explique comment réessayer. Une bonne lecture ouvre ensuite température, humidité et mouvement.
- La priorité nutrition recommande la gelée royale; la priorité stabilité recommande la rotation hygiénique. La recommandation reste informative et les deux soins demeurent disponibles.
- Deux décisions actives sont ajoutées sans minuterie artificielle. Le chapitre 2 conserve 15 objectifs et 180-224 s, mais atteint 30 interactions actives. Le chapitre 4 devient seul plancher à 28 interactions et la prochaine tranche recommandée.
- Sur mobile, le rendu dérive la recommandation du dernier instantané reconnu; il ne modifie jamais vitalité, ressources ou minuterie. Le serveur reste propriétaire des valeurs et devra persister progression tutorielle et révision avant activation officielle.
- Audit serveur documentaire: `Docs/ProductionIntegration/Chapter2_BroodVitality_InterpretationContractAudit_2026-07-21.md`; aucune implémentation, compilation, mutation de soin ou construction de candidat pendant le test manuel.
- Les catalogues `fr-CA` et `en-US` contiennent chacun 500 clés uniques et alignées.
- Validation Unity 6000.5.3f1: compilation globale et suite LivingHive réussies dans `Artifacts/BroodVitalityInterpretationFinalF8.log`; 22 captures sur 22 aux dimensions attendues dans `Artifacts/GuidedBroodIncubation`.
- Preuves inspectées: `Chapter2_VitalityAssessment_390x844.png` et `Chapter2_VitalityAssessment_1600x900.png`. Rapport: `Docs/Product/LivingHive_BroodVitalityInterpretationMilestone_2026-07-21.md`.
- La scène canonique 50x50, ses images et l'image de base LivingHive sont inchangées.

### Qualification active du lot témoin — réalisée

- Après la collecte manuelle de 120 ou 160 cire, le joueur relie désormais la spécialisation de l'atelier au risque dominant : chaleur pour Rendement, tenue sous charge pour Stockage.
- Une erreur est réversible, sans coût, délai, gain ni progression. Une réussite unique ouvre les deux applications et affiche une recommandation informative : réserve après Rendement, nurserie après Stockage.
- Le chapitre 4 conserve 13 objectifs et 145-165 s, mais passe de 28 à 29 interactions actives. Les chapitres 1, 4 et 5 partagent maintenant le plancher à 29.
- Le blocage manuel des clics de l'introduction est corrigé : les contrôles du HUD et des rails sont désactivés sous le tutoriel, sauf lorsque Carte/Ruche est explicitement la cible.
- L'appareil conserve seulement le rendu et le dernier instantané reconnu; la spécialisation, la collecte, la quantité, la progression, la révision et l'idempotence officielles restent serveur. Le noyau `Server/` passe 24/24 tests HiveOperations et le build Release sans erreur. Route et drapeau restent fermés; les tests HTTP doivent encore être rejoués sous .NET 8 natif avant toute ouverture.
- Les catalogues `fr-CA` et `en-US` contiennent chacun 509 clés uniques et alignées.
- Validation Unity 6000.5.3f1 : F8 global vert dans `Artifacts/WorkshopBatchQualificationFinalF8.log`; 52 captures sur 52 aux dimensions exactes dans `Artifacts/GuidedOpeningInstallation`.
- Preuves inspectées : `Chapter4_BatchQualification_390x844.png` et `Chapter4_BatchQualification_1600x900.png`. Rapport : `Docs/Product/LivingHive_WorkshopBatchQualificationMilestone_2026-07-21.md`.
- Le validateur exact-crop repasse sur 2 500 tuiles/importeurs et 4 900 voisinages sans incohérence; scène canonique, manifeste terrain et image LivingHive gardent leurs empreintes.

### Localisation bilingue de la première session du chapitre 1 — réalisée

- Le cadre partagé du tutoriel et les panneaux du chapitre 1, de la première collecte jusqu'au ravitaillement du couvain, utilisent désormais les catalogues `fr-CA` et `en-US` pour les titres, explications, commandes, contrôles et barres de progression.
- 71 entrées bilingues portent les catalogues de 509 à 580 clés uniques chacun. Les deux ensembles sont strictement identiques, sans doublon; les 134 références du chapitre 1 et du cadre partagé existent dans les deux langues.
- Aucun coût, délai, gain, choix stratégique, interaction active ou contrat serveur n'est modifié. La langue et le rendu restent sur l'appareil; l'économie et la progression officielles restent serveur.
- Les assemblages jeu et éditeur compilent avec 0 erreur via le projet C# généré. F8 et la suite LivingHive passent avec sortie 0 dans `Artifacts/Chapter1LocalizationF8.log`; la campagne contient 54/54 captures exactes, 27 par format.
- Les preuves `Chapter1_ProductionChoice_en-US_390x844.png` et `Chapter1_ProductionChoice_en-US_1600x900.png` ont été inspectées à résolution native sans coupure ni collision. Une preuve française régénérée confirme que chaque capture conserve sa langue imposée.
- Rapport: `Docs/Product/LivingHive_Chapter1FirstSessionLocalizationMilestone_2026-07-22.md`. Le terrain, l'image LivingHive et Communication sont inchangés.

### Choix de langue mobile avant la première session — réalisé

- L’entrée LivingHive propose `FR` et `EN` avant le tutoriel; chaque cible mesure au moins 44x44 px et l’état actif est souligné en or.
- La préférence est versionnée dans `PlayerPrefs`, prime sur la langue système et demeure strictement sur l’appareil. Aucune donnée ni autorité serveur n’est nécessaire.
- Les 40 textes `splash.*` rendent l’accueil et l’authentification locale cohérents dans les deux langues. Les catalogues comptent 620/620 entrées uniques, alignées et sans doublon.
- F8 Unity et captures passent avec sortie 0 dans `Artifacts/LivingHiveLanguageSelectorF8.log` et `Artifacts/LivingHiveLanguageSelectorCapture.log`.
- Quatre preuves exactes, FR/EN en 390x844 et 1600x900, sont ratifiées dans `Docs/Product/Evidence/LivingHiveLanguageSelector`.
- Rapport: `Docs/Product/LivingHive_MobileLanguageSelectorMilestone_2026-07-22.md`. Terrain, image LivingHive, serveur et Communication inchangés.

### Coquille mobile de pré-authentification — réalisée

- `Connexion` montre maintenant l’état réel du service officiel au lieu d’afficher un formulaire de courriel/mot de passe sans transport de production. Dans l’état courant, sessions et jetons sont explicitement fermés côté serveur.
- `Créer` produit seulement un profil de démo local avec nom d’affichage. Aucun courriel, mot de passe, compte serveur, access token ou refresh token n’est demandé, créé ou persisté.
- La future collecte de justificatifs exige simultanément une readiness serveur complète et un transport mobile sécurisé. Un logout ou changement de joueur referme les deux portes.
- L’Intégrateur a durci `/accounts`, `/auth/login` et `/auth/refresh` : en Production, les drapeaux fermés retournent `503 auth.unavailable` avant tout service métier. Aucun jeton, candidat ou déploiement n’est activé.
- Appareil : langue, nom de démo, rendu et caches locaux non officiels. Serveur : identité, compte, empreinte de justificatif, sessions, jetons et profil officiel. Le futur refresh token devra être protégé par Android Keystore ou iOS Keychain; l’access token restera en mémoire.
- Le chat demeure `NotConfigured` jusqu’au raccordement réel de la session officielle et du stockage protégé. Aucun fichier Communication n’a été modifié pendant ce créneau coordonné.
- F8 et captures passent avec sortie 0 dans `Artifacts/LivingHivePreAuthShellF8.log` et `Artifacts/LivingHivePreAuthShellCapture.log`. Les catalogues comptent 649/649 clés uniques et alignées; huit preuves exactes couvrent accueil, serveur fermé et profil démo aux formats 390x844 et 1600x900.
- Rapport: `Docs/Product/LivingHive_PreAuthMobileShellMilestone_2026-07-22.md`. La scène canonique, le terrain et l’image LivingHive sont inchangés.

### Préparation d’escouade fondée sur le roster réel — réalisée

- `Armée -> Préparer` relie le triangle
  Gardiennes/Voltigeuses/Lanceuses au roster doctrinal officiel sans renommer
  les rôles historiques.
- Avec session autorisée, le roster, les soldes, les offres, l'heure UTC et
  l'opération active proviennent du serveur. Sans session, l'ancien roster
  local reste un aperçu explicitement non officiel.
- Soldats et Éclaireuses restent `hors doctrine`; aucune migration ne les
  convertit en Voltigeuses ou Lanceuses.
- Le brouillon et la menace restent volatils sur l'appareil. Aucun combat,
  bonus, perte, puissance ou résultat n'est simulé.
- START/CLAIM utilisent une outbox protégée et une clé stable. Une réponse
  ambiguë ne provoque jamais une soumission automatique; `Vérifier` reprend
  exactement la commande préparée.
- `CombatRecruitment:Enabled=false` et
  `CombatFormationReadiness:Enabled=false` restent fermés par défaut et en
  Production.
- Preuves hors Unity : mobile 13/13 + 10/10; assemblages 0 erreur; serveur
  4/4 cœur, 3/3 HTTP et 341 réussis/0 échec/8 SQL ignorés; catalogues
  1262/1262. Rapport :
  `Docs/Product/LivingHive_OfficialDoctrineRecruitmentMobileMilestone_2026-07-23.md`.
- La réservation officielle de composition est maintenant raccordée; ses
  portes restantes sont F8, tests Editor, preuves visuelles et Android staging.

### Composition multi-famille et réservation persistée — réalisée

- `Armée -> Préparer` permet maintenant de composer jusqu’à 12 Gardiennes, Voltigeuses et Lanceuses au lieu de choisir une seule famille. Les quantités sont bornées par le roster et la capacité.
- La suggestion face à une menace produit une lecture explicable `répondent / exposées / neutres`; elle ne simule ni victoire, dégâts, pertes, récompense ou puissance.
- Le brouillon reste volatil, mais la réservation officielle passe maintenant
  par une outbox protégée avant transport. Une panne ne provoque aucun rejeu
  automatique; la reprise est un geste explicite avec la commande exacte.
- Le serveur persiste une réservation par joueur/ruche sous le contrat
  `phase4-combat-squad-reservation-v1`, avec commit/release atomiques,
  révisions, heure UTC et reçus publics rejouables. Les routes restent fermées
  par `CombatSquadReservation:Enabled=false` par défaut et en Production.
- Preuves de fondation : assemblages jeu/éditeur 0 erreur, harnais autonome
  64/64, serveur 47/47 HiveOperations et captures FR/EN ratifiées. Preuves du
  raccord officiel : client 24/24, présentation 10/10, serveur 3/3 cœur,
  5/5 HTTP et suite net10 346/0/8 SQL ignorés.
- Rapports :
  `Docs/Product/LivingHive_SquadCompositionMilestone_2026-07-22.md` et
  `Docs/Product/LivingHive_OfficialSquadReservationMobileMilestone_2026-07-23.md`.
  Les trois fondations protégées gardent leurs empreintes; Communication est
  resté gelé.

### Sortie au périmètre — fondation serveur et interface mobile ratifiées

- Le contrat `phase5-hive-perimeter-sortie-v1` enchaîne la réservation d’escouade avec deux signaux non-combat cyclés sur huit heures, sans activer ni simuler la carte mondiale.
- Le serveur possède l’instance du signal, `HazardDoctrine`, l’horloge UTC, l’éligibilité, la sortie active, la réclamation, le rappel et les soldes. Le mobile ne conservera qu’un cache, le brouillon, le rendu et des notifications locales.
- Une récompense ne peut être obtenue qu’une fois par signal et par cycle. Launch, claim, recall, changement de cycle, reconstruction, capacité et isolation sont couverts par des tests persistants et HTTP.
- Preuves : 5/5 tests DurableJson ciblés, 52/52 HiveOperations, 5/5 HTTP ciblés, 265 réussis + 7 SQL ignorés sur `BeeKingdom.Tests`, build Release sans erreur.
- `HivePerimeterSortie:Enabled=false` reste fermé par défaut et en Production; la carte mondiale demeure `PreparationOnly/ReadOnlyNonLiveFoundation` et `DeploymentAuthorized=false`.
- Le read model est raccordé au panneau par injection et le risque affiché vient du signal officiel. Sans session mobile autorisée, le contrôleur de production reste honnêtement `NotConfigured` et aucune mutation n'est disponible.
- Rapport : `Docs/Product/LivingHive_HivePerimeterSortieServerMilestone_2026-07-22.md`.

### Contrat mobile de sortie — réalisé avec interface injectable

- `BeeKingdom.Networking` expose un client injectable pour réservation, lecture
  du tableau, lancement, réclamation et rappel. Il exige la porte de session
  officielle avant toute consultation du jeton ou du réseau.
- Le mobile refuse les versions, identités, cycles, signaux, risques, récompenses,
  instances, réservations et révisions incohérents. Il ne persiste aucun jeton ou
  snapshot et n'accorde jamais lui-même une récompense.
- L'audit a corrigé le serveur : `HivePerimeterSnapshot.Revision` rend possible
  le second signal après une première réclamation. La séquence 0 → 1 → 2 → 3 est
  prouvée côté service, HTTP et client. `ServerTimeUtc` permet un compte à rebours
  relatif sans faire de l'horloge murale du téléphone une autorité.
- Preuves client et présentation isolée : 18/18 scénarios et compilation `netstandard2.1` sans erreur ni
  avertissement. Les deux filtres serveur restent à 5/5.
- L'interface est visible et ratifiée en portrait/paysage, mais le transport HTTP
  officiel, le cache protégé et l'activation des flags restent fermés. Rapport :
  `Docs/Product/LivingHive_HivePerimeterSortieMobileContractMilestone_2026-07-22.md`.

### Session officielle mobile — fondation ratifiée et fermée par défaut

- LivingHive possède maintenant readiness, login, refresh rotatif, restauration,
  expiration, logout bearer et changement de joueur derrière une injection
  runtime absente du build courant.
- L'access token reste en mémoire. Le refresh vise Android Keystore AES-GCM;
  PlayerPrefs ne contient qu'un identifiant d'installation aléatoire, non
  matériel et non autoritaire. Editor, iOS sans Keychain et toute plateforme sans
  coffre restent `NotConfigured`.
- Le formulaire n'apparaît que si les portes client et serveur sont toutes
  ouvertes. L'état réel actuel reste sans formulaire; les états prêt/connecté
  sont marqués `APERÇU QA` et n'appellent aucun serveur.
- Le serveur atteste joueur/compte/session, rotation, expiration et révocation.
  L'IP déclarée par l'app est ignorée; seule la connexion HTTP est enregistrée.
  Les flags Production et `DeploymentAuthorized` restent fermés.
- Preuves : harnais autonome 34/34, F8 final vert, 4 captures exactes,
  catalogues 1054/1054 sans doublon. Rapport :
  `Docs/Product/LivingHive_OfficialMobileAccountSessionMilestone_2026-07-22.md`.
- Prochaines portes : Android physique, configuration HTTPS staging, deux joueurs,
  SQL natif/proxy de confiance, cache offline protégé puis migration des autorités
  de jeu locales. Communication est resté gelé.

### REST de jeu authentifié et cache protégé — réalisé, activation fermée

- Le client de sortie utilise maintenant un transport Unity HTTPS borné pour
  `/game/v1/**`, avec JSON réel et validation de l'identité serveur.
- L'expiration déclenche une rotation dédupliquée. Un premier `401` répète
  exactement une fois la même commande/idempotence; un second purge la session.
  Une panne réseau ne répète jamais une mutation.
- Les GET validés peuvent alimenter un cache AndroidKeyStore AES-GCM v1,
  partitionné joueur + ruche + contrat + route, borné à 12 entrées et 7 jours.
  La corruption purge le document; POST, gains et progression n'y entrent jamais.
- Hors ligne, le panneau affiche explicitement `consultation seulement`, masque
  tous les verbes serveur et ne montre pas un reçu ancien comme nouveau débrief.
- Le serveur ajoute `private, no-store`/`Pragma: no-cache` aux lectures game et
  conserve `401 game.session_required` sans fuite. Flags Production fermés.
- Preuves : 43/43 autonomes, F8 global vert, catalogues 1056/1056, huit captures
  exactes dont les deux états hors ligne inspectés. Rapport :
  `Docs/Product/LivingHive_AuthenticatedGameRestAndProtectedReadCacheMilestone_2026-07-22.md`.
- Appareil : jetons protégés, cache de lecture et rendu. Serveur : compte,
  identité, ruche, révisions, temps, réservations, mutations et récompenses.
- Portes restantes : asset d'environnement et HiveId, Android physique,
  IL2CPP/AOT, HTTPS/SQL staging, flags et comptes réels, puis déploiement.
  Communication est resté gelé.

### Audit Android IL2CPP — chaîne native atteinte, packaging non ratifié

- Un harnais fermé construit uniquement `LivingHive` en Android IL2CPP ARM64,
  APK Development, sans asset runtime, compte, secret, `HiveId`, flag Production
  ni installation appareil.
- La passe a atteint les sorties IL2CPP, `libil2cpp.so` et la configuration CMake
  `arm64-v8a`. Le Player mesure toutefois 2,02 Gio compressés / 2,25 Gio non
  compressés; Gradle a échoué à `compressDebugAssets` faute d'espace disque.
- Aucun APK/manifeste de succès n'est produit. Le build Android installable,
  AndroidKeyStore et le comportement sur appareil restent donc ouverts.
- Le harnais restaure maintenant Mono/architectures antérieures avant la sortie.
  Les caches temporaires ont été nettoyés et aucun processus ne reste.
- La recompilation Windows ne montre aucune erreur C#, mais F8 n'a pas exécuté
  ses assertions après trois refus de licence headless. Le dernier F8 ratifié
  demeure celui du jalon REST/cache.
- Prochaine tranche : inventaire des 2,25 Gio, budget et livraison de contenus
  mobile, puis nouvelle construction IL2CPP et test appareil.
- Rapport :
  `Docs/Product/LivingHive_AndroidIl2CppPackagingAudit_2026-07-22.md`.

### Budget et propriété du contenu mobile — audit réalisé, migration non commencée

- `Resources` contient 995,1 Mio / 5 273 fichiers. Le terrain Wave6 en représente
  922,8 Mio / 5 004 fichiers : 825,0 Mio canoniques et 97,8 Mio `v1` obsolètes
  pour la scène actuelle.
- Les 2 500 tuiles canoniques Android sont importées sans compression. Leur
  inclusion automatique explique l'essentiel des 2,25 Gio du Player.
- Aucun terrain, PNG, manifeste, `.meta`, scène ou chargeur n'a été modifié. La
  migration exige un créneau terrain explicite et des comparaisons visuelles.
- Cible : installation de base sous 200 Mio, contenu initial sous 150 Mio et
  terrain canonique téléchargé sous 300 Mio, à mesurer et non encore ratifié.
- Appareil : bundles vérifiés, cache/quota/reprise/rollback. Serveur/CDN :
  manifeste versionné et bundles immuables. Autorité joueur/économie inchangée.
- L'Intégrateur a livré le contrat serveur fermé
  `GET /runtime/world-map-content-manifest`: HTTPS strict, ETag/304, 512 Mio par
  bundle, 2 Gio cumulés, 7/7 tests indépendamment rejoués. Production reste
  fausse; aucun terrain, candidat ou déploiement ne lui est confié.
- Rapport :
  `Docs/Product/LivingHive_MobileContentOwnershipAndBudget_2026-07-22.md`.

### Production après absence — tranche verticale fermée et ratifiée

- Le serveur possède l’horloge UTC, le catalogue, les taux, capacités, pending décimal, soldes, révision et reçus idempotents sous `living-hive-offline-production-v1`.
- Le mobile conserve seulement le rendu, l’état transitoire, une clé d’idempotence en attente et un dernier GET protégé/partitionné. Il ne calcule ni ne crédite aucune ressource officielle.
- Les routes authentifiées sont `GET /game/v1/hives/{hiveId}/offline-production` et `POST /game/v1/hives/{hiveId}/offline-production/{buildingKey}/collect`.
- Le panneau LivingHive est raccordé au cycle de session officiel. `NotConfigured`, chargement, lecture hors ligne, collecte, erreur et succès restent distincts; une panne réseau ne répète jamais aveuglément un POST.
- Le parcours officiel réel reste fermé au shell. Un état de panneau injectable affiche `Serveur requis`, sans taux, stock, réussite ou reçu inventé; il prouve la mise en page, pas une session live. Les deux premières captures non conformes ont été refusées avant la ratification finale.
- Preuves : serveur 315 réussis/8 SQL ignorés, client 60/60, `Assembly-CSharp` 0 erreur, F8 final vert, catalogues 1080/1080 et deux captures exactes inspectées en 390x844/1600x900.
- Aucun terrain 50x50, PNG, image de ruche ou scène n’a été modifié. Communication est resté gelé.
- Rapport : `Docs/Product/LivingHive_OfflineProductionMobileMilestone_2026-07-22.md`.
- Portes suivantes : catalogue économique réel, SQL/TLS staging, asset/HiveId, Android Keystore physique, paquet IL2CPP installable, puis seulement flags/candidat/déploiement.

### File de construction officielle — tranche implémentée, validation Unity ouverte

- Le contrat `living-hive-building-upgrade-v1` place catalogue, coûts, soldes,
  file, UTC, durée, niveau, révision et reçus sur le serveur.
- Le téléphone conserve seulement le rendu, le cache GET protégé, l'état
  transitoire et une clé d'idempotence de reprise. Il ne termine jamais une
  amélioration parce que son propre compte à rebours atteint zéro.
- La file latérale et le détail de l'atelier de cire utilisent l'état officiel
  lorsqu'un contrôleur de session est injecté. Hors configuration, aucune
  valeur serveur n'est inventée et la démonstration reste locale.
- Preuves acquises : serveur 321 réussis/0 échec/8 SQL ignorés, client 13/13,
  quatre assemblages compilés sans erreur, catalogues 1115/1115 avec 35 clés
  dédiées sans doublon.
- Les empreintes de la scène canonique, de `LivingHive.unity` et de l'image de
  base sont inchangées. Communication est resté gelé.
- F8 et les deux captures 390x844/1600x900 ne sont pas encore ratifiés : Unity
  est resté fermé et la limite de l'outil a refusé le lancement automatisé.
- Rapport :
  `Docs/Product/LivingHive_BuildingUpgradeMobileMilestone_2026-07-22.md`.
- Portes suivantes : F8/captures Unity, catalogue économique réel, SQL/TLS
  staging, environnement/HiveId, deux joueurs et Android physique, paquet
  IL2CPP, puis seulement flags/candidat/déploiement.

### Recherche officielle — tranche mobile compilée, validation Unity ouverte

- Le contrat `living-hive-research-v1` place catalogue, coûts, soldes, UTC,
  durées, file, effets en bps, révision et reçus sur le serveur.
- Le téléphone conserve le rendu, un cache GET protégé en lecture seule,
  l’état transitoire et une clé d’idempotence stable. Son compte à rebours
  monotone n’autorise jamais une complétion sans `awaiting_completion` serveur.
- L’écran Recherche et la file latérale utilisent l’état officiel lorsqu’un
  contrôleur de session est injecté. Sans configuration, aucune valeur serveur
  n’est inventée et la prévisualisation locale reste non officielle.
- Le serveur projette désormais `effects` de façon structurée, applique les bps
  persistés à la production et filtre les offres par un catalogue configuré,
  vide par défaut et en Production. Le flag reste faux.
- Preuves hors Unity : client 13/13, présentation 6/6, serveur HTTP 4/4,
  serveur complet 325 réussis/0 échec/8 SQL ignorés, quatre assemblages à
  0 erreur, catalogues 1153/1153 avec 38 clés dédiées sans doublon.
- Les empreintes de la scène canonique, de `LivingHive.unity` et de l’image de
  base sont inchangées. Communication est resté gelé.
- F8, les assertions agrégées et les deux captures honnêtes 390x844/1600x900
  restent à ratifier à la prochaine ouverture Unity. Les preuves HTTP détaillées
  de mutations/rejeu/effets historiques restent aussi ouvertes avant staging.
- Rapport :
  `Docs/Product/LivingHive_OfficialResearchMobileMilestone_2026-07-22.md`.

### Sac & stocks officiel — tranche mobile compilée, validation Unity ouverte

- Le contrat de lecture `living-hive-stock-v1` place sur le serveur l’identité,
  la ruche, le catalogue, la révision, l’UTC, les trois stocks, leurs capacités,
  la population optionnelle, les recherches terminées et les opérations
  actives.
- Le téléphone conserve le rendu, l’état transitoire et un dernier GET protégé,
  cloisonné par joueur et ruche. Il ne crée, ne collecte, ne débite et ne
  termine aucune ressource ou opération dans ce parcours.
- Le panneau `Sac & stocks` affiche uniquement un snapshot validé. Sans
  configuration, aucun montant n’est inventé. Les boutons `Voir` de 44 px
  naviguent seulement vers les bâtiments et ne marquent pas une preuve
  quotidienne locale quand le mode officiel est actif.
- Preuves hors Unity : client 10/10, présentation 5/5, quatre assemblages à
  0 erreur, catalogues 1174/1174 avec 21 clés dédiées sans doublon, et suite
  HiveOperations 51/51.
- Le serveur passe 2/2 tests noyau Stock, 3/3 HTTP ciblés et 328 tests globaux,
  avec 0 échec et 8 SQL ignorés. Le test activé prouve le DTO camelCase et deux
  lectures strictement non mutantes. La cohérence après une mutation Ronde
  quotidienne reste à prouver.
- F8, les 7 tests Editor et les deux captures honnêtes 390x844/1600x900 restent
  à ratifier à la prochaine ouverture Unity. Aucun terrain, PNG, scène ou image
  de base de la ruche n’a été modifié; Communication est resté gelé.
- Rapport :
  `Docs/Product/LivingHive_OfficialStockMobileMilestone_2026-07-22.md`.
