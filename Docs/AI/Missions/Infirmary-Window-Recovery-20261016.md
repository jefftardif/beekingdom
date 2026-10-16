# Infirmerie - restauration de la fenetre fonctionnelle (2026-10-16)

## Cause confirmee
La presentation premium et InfirmaryRecoveryOverview etaient toujours presentes.
Le clic INFIRMARY passait toutefois dans le vieux panneau 350x210 de
HiveMapUnsupportedBuildingBootstrap, sans BindInfirmaryController ni Show.
Le catalogue avait aussi regresse de Active vers Future. Ce n'etait pas un manque
de fonctionnalite serveur a remplacer par une nouvelle simulation locale.

## Correction bornee
Le meme host conserve son abonnement au clic et son drapeau d'overlay existant.
INFIRMARY ouvre desormais HiveMapPremiumBuildingWindow, lie a l'instance courante de
CombatPatrolControllerForHiveMap : compteurs par famille, total, prochaines guerisons,
Actualiser, Retour et amelioration via le selecteur Construction. Les autres batiments
conservent leurs routes; la Banque reste hors de ce fallback. Pas de modification
du serveur, des soins, des stocks, du combat, du startup ni du grand presenter.
Le rebind de session retire les anciennes donnees affichees. Une erreur ou une lecture
initiale incomplete affiche -- plutot qu'un faux total de zero blessees.
L'embleme premium deja approuve est conserve et ajoute au suivi Git avec son meta,
car le fichier image local existait mais n'etait pas suivi.

## Verification
119 tests Editor cibles passes, 0 echec : tests Infirmerie recuperes et adaptes au
contrat courant, nouveau clic reel du host, fermeture, garde d'overlay, reouverture,
etats inconnus, retour de session, compteur sans guerison locale; controles Banque,
Nurserie, Alliance, tutoriel, startup et catalogue.
Le test historique qui classait INFIRMARY Future est mis a jour pour attendre Active;
une assertion explicite conserve l'absence de capacite de soin inventee.
Une erreur de composition du script de modification a ete corrigee avant ces tests.

Controle visuel dans Environment2D5D_HiveMap_Test, Play Mode 1920x1080 : fenetre complete,
emblemes, trois familles, defilement, Actualiser, Ameliorer et Retour visibles.
Controleur reel configure, Etat ReadyToLaunch, zero lot de blessees dans cette session.
Le zero affiche correspond a la lecture reelle, pas a une fixture.
Actualiser a ete utilise et la lecture est revenue ReadyToLaunch. Retour ferme la
fenetre et libere le monde; une nouvelle ouverture reutilise une seule vue.
Aucun combat, depot/retrait, soin force ni credit de troupes declenche pour le test.
Le parcours avec blessees et echeances est teste sur fixtures Editor, sans infliger
artificiellement de pertes au compte reel. Pas de guerison chronometree reelle certifiee.

## Preuves locales
C:/BeeKingdom/DEV/BK-Studio/infirmary-recovery-20261016/
- infirmary-tests-final.json : 119/119.
- infirmary-live.png et infirmary-live-review.jpg : rendu en jeu.
- live-data-proof.txt : contenu de la vue et etat de la vraie file de convalescence.
- interaction-proof.txt : Actualiser, Retour, liberation du monde et reouverture.
- before/ : sauvegardes des seuls fichiers modifies.

Livraison locale sur main. Aucun nouveau worktree, reset global, push ou deploiement.
Unity reste en Play Mode, Infirmerie ouverte. Le scene-start temporaire a ete restaure.