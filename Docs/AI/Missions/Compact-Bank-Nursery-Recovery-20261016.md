# Tutoriel compact final, Banque et Nurserie - 2026-10-16

## Corrections demandees par Jeff

Tutoriel : meme direction artistique approuvee, hauteur paysage reduite de 348 a
300 pixels pour le rendu 1920x1080 (environ 14 %). Largeur conservee a 1488 pixels.
Zephyra est legerement reduite ; boutons tactiles et defilement des textes longs
conserves. Les gardes de regression exigent maintenant <= 28 % de hauteur en paysage
ordinateur. La variante telephone conserve l'espace necessaire a la lisibilite.

Banque : recuperee dans la sauvegarde Git 55264b3b, sans restaurer le presenter
monolithique en bloc. Fenetre et onze paliers dans HiveViewProductUiPresenter.Bank.cs ;
client authentifie separe RoyalReserveClient.cs. Le batiment a de nouveau son bootstrap,
la route apres changement de scene, les gardes d'input et d'occlusion, ainsi que Retour.
L'ancien panneau de fonctionnalite future n'intercepte plus la Banque.
La Reserve Royale retrouve lecture, depot et retrait, revision serveur et cle d'idempotence.
Les reponses tardives d'une fenetre fermee ou d'une ancienne session sont rejetees ; les
anciens soldes sont purges a la fermeture. Aucun debit ou credit local.

Nurserie : la recuperation precedente avait remplace les textures procedurales
Activites par des images de ruche etirees sur les cartes. Le bon materiau et la densite
Activites v4 sont retablis. La jauge Image.Type.Filled a maintenant un sprite source,
sans lequel une valeur nulle apparaissait pleine. Une erreur avant initialisation
n'est plus presentee comme un chargement permanent. Libelles d'erreur FR/EN ajoutes.
Les operations Nourrir/Stabiliser/Terminer/Verifier et les couvees desactivees sont
preservees. Aucun remplacement de service serveur.

## Verification

Dossier de preuves :
C:/BeeKingdom/DEV/BK-Studio/compact-bank-nursery-20261016/

- recovery-tests-v2.json : 125 tests cibles passes, 0 echec, 0 ignore.
  Inclut tutoriel, restauration Banque, contrat client bancaire, Nurserie, client couvees,
  vitalite et gardes de la restauration Alliance precedente. Ce n'est pas toute la suite.
- scope-proof.txt : 3717 membres existants du presenter principal preserves, aucun retire.
  Seuls les deux raccordements modaux et le pont de materiaux ont change dans ce fichier.
- closing-proof.txt : Banque et Nurserie fermees, aucune autre surcouche bloquante.
- tutorial-compact-panel.jpg : inspection visuelle de la version finale compacte.
- bank-settled-review.jpg : feuille de route Banque.
- bank-reserve-real-review.jpg : Reserve Royale avec lecture authentifiee reussie.
- nursery-final-review.jpg et nursery-operations-review.jpg : fenetre et zone des soins.

Inspection Play Mode dans Environment2D5D_HiveMap_Test a 1920x1080. Le passage initial
par l'entree de demonstration n'a pas remplace les controles de session : une session
bancaire configuree a ensuite fourni une vraie reponse serveur validee (snapshot non nul,
aucune erreur). Aucun depot, retrait, lancement de soin ou mutation de test envoye en
production. Les tests de mutation utilisent exclusivement un faux transport local.

## Blocage distinct des soins de Nurserie

Observation runtime : controleur configure, State=Error, Initialized=false,
ErrorCode=server_unavailable. Lecture sur SRVESDT : BroodVitality.Enabled=false dans
appsettings.json et appsettings.Production.json de C:/inetpub/BeeKingdomApi.
Le endpoint de vitalite refuse explicitement le service par HTTP 503 quand cette option
est desactivee. La fonction de soins n'est donc pas certifiee utilisable en production.
Aucun parametre serveur n'a ete change ; aucune couvee experimentale n'a ete activee.

## Livraison et limites

Corrections locales uniquement, sur main. Pas de nouveau worktree, pas de reset global,
pas de publication serveur ni de build Windows. Les changements preexistants de plugins,
polices, metadonnees d'assets et ProjectSettings restent exclus.
Unity a ete laisse hors Play Mode ; fermeture des fenetres verifiee et ancien choix de
scene de demarrage restaure. Les fonds de tests et les donnees de comptes ne sont pas
reinitialises. Les transactions bancaires reelles restent a valider explicitement.