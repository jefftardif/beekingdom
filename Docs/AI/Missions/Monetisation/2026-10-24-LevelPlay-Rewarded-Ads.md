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
- LevelPlay App Key : A RECUPERER depuis le tableau Apps (distinct de l'AdMob App ID).
- LevelPlay Rewarded Ad Unit ID : copier depuis Setup > Ad units (ne pas retranscrire depuis une capture).

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
- Obtenir la veritable LevelPlay App Key et copier l'ID Rewarded.
- Finaliser le profil de paiement et approbation de l'application AdMob.
- Mettre a jour politique de confidentialite / consentement et app-ads.txt si requis.
- Terminer les tests Unity + API et un audit Git avant integration a main.
