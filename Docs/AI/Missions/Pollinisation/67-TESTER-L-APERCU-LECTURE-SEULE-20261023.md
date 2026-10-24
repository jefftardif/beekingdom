# P16 - Tester les champs floraux dans Unity officiel

Date : 23 octobre 2026
Environnement : PCJEFF (Unity Editor), API officielle SRVESDT
Statut : catalogue en lecture seule, aucun gameplay civil productif

## Activation reservee a Unity Editor

La configuration distribuee MobileAccountSessionRuntime.asset reste OFF
(pollinationFeatureEnabled=false par defaut). Le fichier local suivant, ignore
dans les builds Unity, active l'apercu en lecture seule pour Unity Editor :

C:\projets\beekingdomgame-master\Temp\BKPollinationReadOnlyPreview.enabled

Le remplacement de configuration est limite a UNITY_EDITOR et uniquement quand
le serveur est l'API HTTPS officielle api-ops.beekingdomgame.com.
Le marqueur ne contourne pas la connexion joueur ni les controles du serveur.

## Parcours de test

1. Arreter et relancer Play (la configuration est chargee avant SceneLoad).
2. Cliquer sur Connexion dans le jeu; se connecter avec un compte officiel,
   pas un joueur en demo locale.
3. Ouvrir la carte du monde (scene WorldMap...).
4. Cliquer sur POLLINISATION, en haut au centre de la carte.
5. Onglet Champs : verifier deux sites (Prairie des trefles et
   Jardin des fleurs sauvages), pollen et noms FR/EN provenant du serveur.
6. Onglets Essaims et Alliance : verifier les messages d'apercu, les boutons
   des mutateurs doivent rester desactives.
7. Quitter la session : le panneau doit fermer et les donnees joueurs etre
   nettoyees. Verifier qu'aucune ressource, mission ni progression ne change.

## Etat valide

- SRVESDT /health : HTTP 200; GET floral-sites sans session : HTTP 401.
- Population et expeditions civiles : HTTP 503 (flags OFF).
- Projets d'alliance : HTTP 503 (flag OFF).
- SQL 096-098 installees, mais aucune nouvelle migration requise.
- Contrat client .NET P5 avec services civils explicitement fermes : PASS.
- Unity isole : 11/11 tests Pollination PASS apres adaptation du test de
  projets d'alliance disponible; tests WorldMap anterieurs : 8/8.
- Unity officiel apres installation : preview flag Editor TRUE, connexion
  joueur FALSE tant qu'un compte officiel n'a pas ouvert une session.
- Aucune preuve E2E authentifiee des deux sites et aucun multijoueur reel.
- Une version installee sur telephone ne contient pas ce changement Editor :
  il faudra construire et distribuer un client de test apres validation.

## Desactiver immediatement l'apercu

Quitter Play; supprimer uniquement
Temp\BKPollinationReadOnlyPreview.enabled puis relancer Play.
Les fichiers de configuration de la production restent inchanges.
Le correctif conserve un traitement 503 explicite en lecture seule et n'invente
aucune population, mission, ressource ou recompense.