# BeeKingdom - Publicites recompensees (Android)
Date : 2026-10-24
Etat : PREPARATION EN BRANCHE ISOLEE (aucun deploiement en production)

## Configuration verifiee
- AdMob Android App ID : ca-app-pub-6355269720386478~5764103111
- AdMob Rewarded Ad Unit : ca-app-pub-6355269720386478/4091928791
- AdMob unit configuree en Partner Bidding.
- Unity LevelPlay : Beekingdom Android.
- LevelPlay ad unit : BK_DailyQuest_3Ads (Rewarded).
- Google Bidding Rewarded lie a All Countries et affiche comme active.
- LevelPlay App Key : 28f6cc45d (fourni par le proprietaire).
- LevelPlay Rewarded Ad Unit ID : na3phetjxrlsnnax (lu sur la capture Setup > Ad units, a confronter au presse-papiers lors du test reel).

## Integration SDK
1. Ajouter le package officiel com.unity.services.levelplay version 9.5.1 (fait dans ce worktree).
2. Ouvrir CE worktree dans Unity Editor apres sauvegarde des autres sessions.
3. Ads Mediation > Network Manager : installer / verifier l'adaptateur Google AdMob; ironSource par defaut.
4. Android Resolver : verifier les dependances.
5. Verifier l'injection de l'App ID AdMob dans AndroidManifest.
6. Configurer l'App Key LevelPlay et l'Ad Unit ID Rewarded dans les ressources du client.
7. Init SDK avec l'ID du joueur connecte pour les callbacks S2S; uniquement sur mobile.
8. Charger/afficher a la demande les videos Rewarded, sans publicites interstitielles.
9. Declencher l'UI du progres seulement sur confirmation autoritaire du serveur.

## Quete quotidienne
- Facultative : maximum trois visionnements recompenses par compte et par jour.
- Jour serveur UTC; progression synchronisee entre appareils.
- Chaque video attribue un bonus defini cote serveur; le troisieme ouvre un coffre.
- Pas de recompense pour une annonce abandonnee, indisponible ou echouee.
- UI en francais et en anglais; aucun impact negatif pour les joueurs qui refusent.

## Securite et attribution des recompenses
- Ne JAMAIS crediter une recompense sur le seul callback client OnAdRewarded.
- Configurer les callbacks LevelPlay server-to-server (S2S) avec une cle privee unique.
- Verifier la signature attendue par LevelPlay, horodatage et ID utilisateur.
- Stocker EVENT_ID de maniere idempotente et refuser les doubles credits.
- Transaction SQL atomique : evenement publicitaire + compteur quotidien + attribution.
- Retourner EVENT_ID:OK uniquement apres persistance correcte.
- Conserver la cle privee dans la configuration securisee du serveur, jamais dans Unity.
- Verifier les CGU AdMob et consentement publicitaire applicable; aucun affichage automatique.
- Pendant QA utiliser appareils de test et annonces de test; zero revenue reel attendu.

## Criteres de validation
- Build Android compile; iOS hors perimetre de cette phase.
- Test de l'initialisation LevelPlay et de la mediation Google.
- 3 videos recompensees maximum par jour; 4e bloquee par serveur.
- Duplique S2S et conflit multiappareils sans double recompense.
- Recompense refusee si signature invalide ou event ID deja traite.
- UI responsive, FR/EN, et pas de clignotement/refresh.
- Non-regression pollinisation, alliance, batiments, quetes et banque.
- Validation sur appareil Android avant toute activation en production.

## Conditions avant mise en production
- App Key recue; verifier l'ID Rewarded copiable sur le dashboard en cas de no-fill.
- Finaliser le profil de paiement et approbation de l'application AdMob.
- Mettre a jour politique de confidentialite / consentement et app-ads.txt si requis.
- Terminer les tests Unity + API et un audit Git avant integration a main.

## Travaux realises sur la branche d'integration (non publies en jeu)
- Package Unity com.unity.services.levelplay 9.5.1 ajoute au manifest isole.
- Assets/BeeKingdom/Monetization/BeeKingdomLevelPlayRewardedBridge.cs : initialization Android explicite apres login et choix de confidentialite; chargement, affichage, evenements; callbacks client ne donnent AUCUNE ressource.
- Assets/Editor/BeeKingdomAdMobAndroidManifestPostprocessor.cs : ajout idempotent du metadata AdMob Android sans remplacer les activity Unity.
- Tests locaux avec simulation du SDK via dotnet 10 : PASS (authentification, confidentialite, quota avant visionnement, ID S2S joueur, deconnexion, rechargement, manifeste).
- Les tests ont ete realises sur des simulateurs C# et NON sur le vrai SDK ni sur un Android reel.
- Le module est inactif tant que le code du jeu ne l'appelle pas explicitement. Le jeu Unity en production et SRVESDT n'ont pas ete modifies.

## Travaux restants indispensables
- Ouvrir la branche isolee dans Unity et resoudre le package; installer l'adaptateur Google via Ads Mediation > Network Manager.
- Valider le build Android avec SDK reel, init, consentement et publicites de test (ne pas utiliser les annonces de production pour le QA).
- Raccorder l'identifiant du joueur de la session BeeKingdom au module et creer l'interface quete FR/EN.
- Developper la route S2S cote SRVESDT, la migration SQL transactionnelle, l'idempotence EVENT_ID, la verification MD5 utilisant une cle privee protegee, et le verrouillage 3 credits par joueur/jour UTC.
- Activer les callbacks S2S dans LevelPlay uniquement apres tests fonctionnels du serveur.
- Ne PAS declarer le produit monetise tant que les etapes precedentes ne sont pas validees.

## Verification Android reelle (2026-10-24)
- Unity 6000.5.3f1, package LevelPlay 9.5.1 et EDM officiel 2.1.1 : importation PASS.
- Unity LevelPlay Native SDK 9.5.0 : IronSourceSDKDependencies.xml provenant du depot officiel Unity.
- Adaptateur AdMob 5.34.0.0 : ISAdMobAdapterDependencies.xml officiel, dependance Android admob-adapter 5.19.0.
- Gradle : mediation-sdk:9.5.0, admob-adapter:5.19.0, play-services-ads-identifier:18.1.0 confirmes dans unityLibrary/build.gradle.
- Build Android scene de test : PASS ; APK 101474376 octets.
- APK test : Builds/Android/BeeKingdom-LevelPlay-Smoke.apk (non publiable / non inclus dans Git).
- Correctif de compilation Android annexe : TutorialPremiumSkin.scaledBackgrounds uniquement UNITY_EDITOR.
- Log de build Android : C:\BeeKingdom\staging\logs\levelplay-android-smoke-core-20261024.log
C:\BeeKingdom\staging\logs\levelplay-android-smoke-core-20261024.log
- Important : aucune publicite reelle affichee, aucun S2S ni quete FR/EN en production. Le smoke test ne valide pas les annonces sur telephone.
- Unity principal sur PCJEFF et SRVESDT ne sont pas modifies.
