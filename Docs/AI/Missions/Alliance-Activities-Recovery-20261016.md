# Alliance - restauration Activites v4 (2026-10-16)

## Origine et perimetre
Jeff a signale le retour a l'ancienne fenetre Alliance. Le presenter de main ne
contenait plus DrawAllianceActivitiesSurface ni la navigation Alpha a sept onglets.
La refonte a ete retrouvee dans la sauvegarde 55264b3b, avec le rapport M124B du
2026-10-04 dans son troisieme parent (fichiers alors non suivis).

Recuperation syntaxique membre par membre, jamais remplacement global du presenter.
Etat final : 3654 membres existants identiques, 65 membres Alliance/skin commun
restaures, ajoutes ou ajustes ; aucune modification hors de ce perimetre.
Le controleur Alliance recupere seulement les 31 lignes de raccordement UpdateProfile
vers l'operation client deja existante. Aucun changement serveur ou de protocole.

## Retabli et corrige
- Centre plein ecran avec l'entete et les surfaces Activites v4.
- Navigation unique : Apercu, Membres, Chat, Journal, Aide, Dons, Recherches.
- Listes defilables, filtres du Journal, actions de membre et permissions conservees.
- Retour ferme le profil avant de fermer Alliance ; les overlays se liberent.
- Correction apres inspection : titre du profil separe du bouton Retour, corps sous
  l'entete, valeurs non disponibles rendues par un tiret plutot que -1.
- Le bouton externe d'amelioration du batiment ne se dessine plus sur un profil
  membre ou une action modale.

## Preuves
Dossier : C:/BeeKingdom/DEV/BK-Studio/alliance-recovery-20261016/
- final-scope-proof.txt : comparaison syntaxique finale avec le code avant reprise.
- alliance-and-tutorial-tests-final.json : 107 tests cibles passes, 0 echec, 0 ignore.
  Comprend les 34 tests tutoriel, les contrats client Alliance/Aide/Recherche,
  les protections d'overlays et 23 tests de restauration UI.
- overview-settled.png, members.png, chat.png, journal.png, help.png,
  donations.png, research.png et profile-final.png : controles dans la scene canonique
  Environment2D5D_HiveMap_Test en Play Mode 1920x1080.
- back-navigation-proof.txt : retour profil -> Alliance puis retour -> ruche.

Les captures Alliance utilisent un controleur Editor uniquement, explicitement nomme
TEST LOCAL, avec des donnees fictives et aucune operation reseau. Les etats vides ou
non charges y sont intentionnels. Ce controle ne certifie pas un envoi de chat, un don,
une invitation ou une mutation de production. La fixture a ete retiree apres controle,
le controleur precedent restaure et Play Mode arrete. La scene canonique reste ouverte.

## Limite de la suite elargie
Une exploration plus large des tests Alliance a trouve un echec preexistant :
AllianceCreationRolesAndMembership_BlockPersistentOrImplicitRuntimeActions, ligne 45.
Ce test de fondation attend AllianceLeaderAuthorityMissing alors que sa propre fixture
contient une permission Leader avec RequiresServerAuthority=true. La regle evalue cette
permission comme presente. Le module Colony et ce test n'ont pas ete modifies ; l'echec
est reproductible isole. Ni assertion desactivee, ni resultat masque.
Details : legacy-foundation-isolated-result.json et legacy-foundation-failure-note.txt.

## Livraison
Correction locale sur main, independante du commit du tutoriel. Pas de nouveau worktree,
pas de reset global, pas de push, pas de build Windows ni de deploiement serveur.
Prochain controle : compte officiel connecte et donnees reelles de l'Alliance de Jeff.