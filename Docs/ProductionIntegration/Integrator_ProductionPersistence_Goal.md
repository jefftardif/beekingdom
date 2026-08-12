# Agent Integrateur - Persistance de production

Tu es l'agent **Integrateur de production** du projet Bee Kingdom.

## Mission

Commencer la transformation des systemes locaux de Bee Kingdom en systemes
persistants adaptes a une exploitation reelle. Ta priorite est la permanence
fiable de l'etat du joueur et de sa ruche apres une fermeture du jeu, une
reconnexion, un redemarrage du serveur ou une interruption reseau.

L'Architecte travaille actuellement sur l'experience LivingHive dans une autre
VM. Tu dois avancer en parallele sans ecraser ses changements ni modifier ses
fichiers visuels.

## Contexte obligatoire

Avant toute decision:

1. Lire `AGENTS.md`.
2. Lire `Docs/VM/Codex\_VM\_Continuation.md`.
3. Lire `Docs/Product/BeeKingdom\_LivingHive\_ExecutionPlan.md`.
4. Lire `Docs/Demos/LivingHive.md`.
5. Inspecter les services, modeles, bases de donnees, API, systemes d'identite et
mecanismes de persistance deja presents.
6. Lire les rapports recents lies a la production, au serveur et a la simulation.
7. Ne pas modifier le chantier chat et messagerie, maintenant attribue a l'agent
   `Communication`.

Ne demande pas a l'utilisateur de reexpliquer le projet. Documente tes
decouvertes localement.

## Protections absolues

* Ne jamais modifier la carte mondiale 50x50 ni ses images de terrain.
* Ne jamais modifier ou recomposer l'image de base de la ruche.
* Ne pas modifier l'interface LivingHive, ses animations ou son tutoriel pendant
que l'Architecte y travaille.
* Ne pas modifier `Server/src/BeeKingdom.Chat/`, les tests serveur commencant par
  `Chat` ou `SignalRChat`, `Assets/BeeKingdom/Gameplay/Communication/` ni
  `Docs/WorldMapCommunication/`. Ces fichiers appartiennent a l'agent
  `Communication`.
* Ne pas modifier directement un fichier deja change par l'Architecte.
* Ne pas utiliser le lecteur `Z:` comme dossier de travail Unity.
* Ne pas introduire Git dans la VM.
* Ne jamais mettre de secret, mot de passe, jeton ou chaine de connexion de
production dans le depot.

## Premiere tranche verticale

Construire une fondation de persistance de production, puis livrer une premiere
tranche fonctionnelle couvrant:

* identite stable du joueur et de sa ruche;
* soldes de miel, cire, pollen et autres ressources existantes;
* capacite de stockage;
* niveaux des batiments;
* files d'amelioration, de formation et de production;
* date de debut, date de fin et etat de chaque operation;
* ressources produites mais encore en attente d'une collecte manuelle;
* progression du tutoriel et du chapitre;
* recompenses en attente ou deja reclamees;
* version du modele de donnees.

Commencer en priorite par la boucle **ressources + collecte manuelle + file de
batiment**, car elle constitue la base commune du jeu.

## Autorite et securite

La production doit considerer le serveur comme autorite:

* le client demande une action, mais ne decide jamais seul de son resultat;
* le serveur valide les couts, prerequis, capacites et recompenses;
* utiliser l'heure UTC du serveur;
* calculer les fins de files a partir d'horodatages persistants;
* ne pas dependre d'un compteur Unity restant actif;
* empecher une meme depense, collecte ou recompense d'etre appliquee deux fois;
* rendre les commandes sensibles idempotentes;
* prevoir une version ou un controle de concurrence sur l'etat du joueur;
* traiter atomiquement les depenses et les gains associes;
* ne pas crediter automatiquement une production qui exige une collecte manuelle;
* ne jamais faire confiance a l'horloge, aux compteurs ou aux montants envoyes par
le client;
* prevoir des migrations versionnees plutot que de casser les anciennes
sauvegardes.

## Reconnexion et progression hors ligne

Apres une reconnexion:

* reconstruire les files depuis l'etat persistant;
* terminer les operations dont l'echeance serveur est passee;
* conserver la production terminee en attente de collecte;
* empecher les gains dupliques lors de nouvelles tentatives reseau;
* restaurer la progression du tutoriel a une etape coherente;
* reprendre l'affichage sans faire rejouer une depense;
* conserver un mode local degrade uniquement comme cache, jamais comme autorite
finale.

Toute progression hors ligne doit etre bornee, explicite et testee.

## Architecture

Respecter l'architecture existante. Ne pas creer un second systeme concurrent si
des abstractions serveur, depots, services ou modeles existent deja.

Separer clairement:

* contrats et modeles de domaine;
* commandes d'ecriture;
* requetes de lecture;
* persistance;
* transport reseau;
* cache client;
* adaptation Unity;
* configuration d'environnement;
* migrations;
* observabilite.

Maintenir le fonctionnement de la demo locale derriere les abstractions existantes
tant que la connexion de production n'est pas prete. Preferer un adaptateur ou une
bascule de configuration a une reecriture brutale.

Eviter autant que possible les fichiers LivingHive actuellement modifies par
l'Architecte. Construire les nouvelles fondations dans des modules isoles et
fournir ensuite un contrat d'integration clair.

## Tests obligatoires

Ajouter des tests pour au minimum:

* fermeture et reprise pendant une file;
* operation terminee pendant que le joueur est hors ligne;
* double clic ou double requete de collecte;
* nouvelle tentative apres un delai reseau;
* deux commandes concurrentes sur le meme batiment;
* capacite de stockage presque pleine;
* ressources insuffisantes;
* recompense deja reclamee;
* modification de l'horloge du client;
* migration d'une ancienne version de sauvegarde;
* restauration d'une etape de tutoriel;
* redemarrage du service ou de la base pendant une operation.

Aucun test ne doit dependre d'attentes reelles de plusieurs secondes: utiliser une
horloge injectable ou controlee.

## Exploitation

Prevoir des la fondation:

* journaux structures avec identifiants de correlation;
* erreurs exploitables sans exposer de donnees sensibles;
* delais d'expiration reseau;
* politique de nouvelle tentative limitee;
* verification de sante;
* configuration par environnement;
* metriques sur les echecs, doublons, files et temps de traitement;
* strategie de sauvegarde et de migration;
* possibilite de diagnostiquer l'etat d'un joueur sans modifier ses donnees.

## Coordination

Avant chaque serie de modifications:

1. Synchroniser la copie locale.
2. Verifier le rapport de conflits.
3. Dresser la liste des fichiers que tu comptes modifier.
4. Eviter les fichiers actifs de LivingHive et les fondations protegees.
5. Preferer de nouveaux modules isoles lorsque cela reduit les conflits.

Le bac a sable Codex peut ne pas avoir acces a `Z:` ou au partage UNC. Si la copie
locale a ete preparee par l'utilisateur et que le partage est inaccessible, ne pas
bloquer la tranche pour cette seule raison. Travailler exclusivement dans `C:`,
limiter les modifications a `Server/`, aux tests serveur et a
`Docs/ProductionIntegration/`, puis remettre a l'utilisateur la liste des fichiers
modifies. Ne jamais tenter de contourner le bac a sable ou d'ecrire directement
dans le partage.

A la fin:

1. Compiler les composants concernes.
2. Executer les tests.
3. Documenter le modele d'autorite et les decisions.
4. Produire un rapport d'integration destine a l'Architecte.
5. Indiquer precisement les API et contrats que LivingHive devra appeler.
6. Synchroniser seulement lorsque le partage est accessible et qu'aucun conflit
   n'est present; sinon laisser la synchronisation finale a l'utilisateur.

## Premier livrable attendu

Livrer une premiere tranche demontrant qu'une operation reelle de batiment peut:

1. etre validee et demarree;
2. debiter ses ressources une seule fois;
3. survivre a une fermeture ou reconnexion;
4. se terminer selon l'heure serveur;
5. conserver son resultat en attente;
6. etre collectee manuellement une seule fois;
7. mettre a jour le solde et la capacite;
8. etre relue correctement par Unity;
9. etre couverte par des tests automatises;
10. etre documentee pour l'integration LivingHive.

Poursuis de facon autonome tant que tu peux terminer une tranche verifiable sans
toucher aux fichiers actifs de l'Architecte. En cas de conflit de fichiers ou de
decision irreversible sur l'architecture de production, arrete cette partie,
documente les options et signale precisement le blocage.
