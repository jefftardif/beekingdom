# BeeKingdom — Quête vidéo récompensée v1
Date : 2026-10-24
Branche : feat/rewarded-quest-server-20261024
État : implémentation isolée, sécurité serveur testée, Unity compile. Non publiée sur SRVESDT.

## Contrats
- Unity LevelPlay Android App Key : 28f6cc45d.
- Bloc LevelPlay Rewarded : BK_DailyQuest_3Ads.
- Google AdMob App ID : ca-app-pub-6355269720386478~5764103111.
- AdMob Rewarded Unit : ca-app-pub-6355269720386478/4091928791.
- Les publicités de test Google et ironSource ont été validées sur une tablette Android. Cela ne valide pas la diffusion Live.

## Principe
- Dans Journal de la ruche, nouvel onglet « Cadeaux » FR/EN.
- Trois visionnements facultatifs par joueur et jour UTC.
- Après chaque visionnement, seul un callback LevelPlay S2S dûment signé déclenche le crédit serveur. Le callback client ne crédite jamais.
- Crédits cumulés proposés sur 3 visionnements : jusqu'à 120 miel et 60 pollen (le troisième inclut le bonus « coffre »). Les montants sont des paramètres de gameplay v1 à équilibrer avant sortie.
- Capacité du sac respectée : le crédit ne dépasse jamais la capacité autorisée; l'arbitrage « récompenses en attente » si capacité pleine reste à terminer avant lancement commercial.
- Les notifications sont idempotentes : réémission du même EventId ne crédite jamais deux fois.
- Les complétions sont persistées dans le PlayerHiveState existant, avec les ressources, au sein de la même transaction atomique SQL. **Aucune nouvelle migration SQL**.
- 512 événements max dans la fenêtre; historique de 12 jours couvrant les 7 jours de re-tentatives LevelPlay.
- Ne pas créer de ruche par un callback. Identité playerId Guid au format N (32 caractères), dérivée du compte connecté, pas de l'ID matériel de tablette.
- Pas d'annonce dans les combats.

## Routes
GET /game/v1/hives/{hiveId}/rewarded-ads
- Require Bearer token existant et propriétaire authentifié.
- Retourne le compteur, le jour UTC, l'état Enabled, les montants du prochain gain et les totaux.

GET /monetization/levelplay/reward-callback
- Utilisé seulement par LevelPlay, sans token joueur, avec signature S2S requise.
- Validation MD5 documentée par Unity : timestamp yyyyMMddHHmm UTC + eventId + applicationUserId + rewards + privateKey.
- Signature compare à temps constant; borne temporelle de dix jours; récompense égale à 1; appKey attendue.
- Ne répond EVENT_ID:OK qu'après transaction persistée.
- Réponse 403/503 pour invalidité indisponibilité, permettant les nouvelles tentatives côté LevelPlay.

## Configuration de production — non effectuée
1. Dans LevelPlay > Apps > BeeKingdom > Set S2S callback, définir une clé privée aléatoire forte (32 caractères minimum) ; ne jamais la saisir dans Unity ni dans Git.
2. URL du callback (macros exactes LevelPlay) :
   https://api-ops.beekingdomgame.com/monetization/levelplay/reward-callback?applicationUserId=[USER_ID]&rewards=[REWARDS]&eventId=[EVENT_ID]&appKey=[APP_KEY]
   Les paramètres timestamp et signature sont ajoutés par LevelPlay.
3. Sur SRVESDT, configurer l'environnement de processus sécurisé RewardedAdsQuest__PrivateKey avec exactement cette clé, et RewardedAdsQuest__Enabled=true **après** validation bout en bout et revue du consentement.
4. Compléter le consentement par une CMP certifiée si les régions desservies l'exigent (Google UMP / TCF). L'option de visionnement volontaire ne remplace pas la CMP.
5. Profils de paiement Google et Unity, revue AdMob, présence sur store, app-ads.txt, politique confidentialité à finaliser avant monétisation réelle.
6. Vérifier en production le flux trois visionnements, quatrième refusé, double événement, perte de réseau, multi-appareils, journée UTC, stockage saturé et publication après sauvegarde complète.
7. L'API en production n'a **pas** été remplacée : la branche principale PCJEFF comprend des modifications non commitées de pollinisation, d'UI et de serveur. Auditer et réconcilier avant toute publication.
8. Aucun secret n'est enregistré dans ce dossier ni dans un fichier appsettings commité.

## Tests terminés
- 6 tests xUnit sur la signature, l'idempotence, les crédits, la remise à zéro UTC, la concurrence et la persistance JSON : PASS.
- Compilation serveur .NET 8 : PASS (0 erreur).
- Importation / compilation Unity 6000.5.3f1 : PASS (0 erreur, code de sortie 0).
- Reste : vrai build Android avec nouvel onglet, test de callback simulé en environnement isolé, validation S2S LevelPlay live, QA visuelle.

Sources :
- https://docs.unity.com/en-us/grow/levelplay/platform/settings/server-to-server-callback
- https://docs.unity.com/en-us/grow/levelplay/platform/settings/event-handlers
