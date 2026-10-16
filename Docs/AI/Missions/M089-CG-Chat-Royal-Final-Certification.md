# M089-CG — Certification finale Chat Royal

## Résultat et cause racine
Certification partielle, non validée en Play Mode/mobile. Régression confirmée par le chemin de code : tout statut différent de Online/Polling désactivait la source serveur ; le composer et le simulateur pouvaient alors ajouter des messages locaux dans une conversation réelle, hors outbox.

## Fichiers et implémentation
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.ChatRoyal.cs` : conserve le snapshot serveur comme source pendant déconnexion/reconnexion/erreur ; synchronise aussi les messages en attente.
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : commentaire de routage actualisé uniquement.
- `Assets/BeeKingdom/Playground/Editor/ChatRoyalSendWiringTests.cs` : deux cas privé/groupe vérifient le transport réel et l'absence d'ajout simulé hors connexion, avec restauration des données statiques.
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs` : quatre fixtures obsolètes remises en conformité (scope de création privée requis et dates relatives à la fenêtre de replay).
- Présent rapport. Aucune refonte UI, scène ou donnée de production modifiée.

## Tests et compilation
Serveur : **59/59 tests réussis**, 0 ignoré, build réussi avec avertissements préexistants ; filtres `ChatGroupServiceTests`, `ChatMessagingEndpointTests`, `ChatTransportContractTests`. Preuve locale : `Logs/M089/M089-chat-server.trx`.

Client hors Unity : **175 cas réussis au cumul** sur 180 (171 au premier passage, puis 4/4 fixtures corrigées relancées seules) ; **5 échecs environnementaux** dans le codec `JsonUtility`, qui exige le moteur natif. Les cinq classes couvrent provider, groupes/transport, annulation de connexion et stockage local ; preuves `Logs/M089-ClientTests/Results/M089-ClientTests.trx` et `M089-ClientTests-FixedFixtures.trx` dans le même dossier.

Unity EditMode : deux lancements arrêtés avant compilation par l'échec IPC du Package Manager, aucun XML ; preuve `Logs/M089-ChatRoyal-EditMode-Retry.log`. Compilation de secours non concluante : les références du projet hôte divergent de ce worktree ; les assemblages jeu/éditeur et les deux nouveaux tests ne sont donc pas validés.

## Checklist d'acceptation
- [x] Privé et groupes : échanges/idempotence/permissions validés côté serveur par les tests ciblés.
- [x] M083/M084 : libération du focus à la fermeture et verrou partagé du bouton de production présents, vérifiés dans le code.
- [x] Noms affichés et self/other : résolution des noms et bulles droite/gauche examinées ; aucune nouvelle régression confirmée.
- [x] Membres et composer : routage et variantes compactes examinés ; correction limitée au basculement vers les envois simulés.
- [x] Reconnexion/persistance : tests client du contrôleur, annulation de connexion et stockage local exécutés hors Unity ; cela ne certifie pas le parcours réel à deux appareils.
- [ ] Exécuter les tests Unity ciblés, dont les deux nouveaux cas privé/groupe hors connexion.
- [ ] Dans `Environment2D5D_HiveMap_Test`, confirmer deux comptes, noms après reconnexion, alignement self/other, ouverture/fermeture Membres et absence du bouton « i » traversant le chat.
- [ ] Sur mobile portrait/paysage : saisie clavier et emojis, envoi, défilement, fermeture/focus, absence de masquage du composer ; coupure réseau puis reconnexion/redémarrage sans perte ni doublon.

## Non résolu
Certification runtime, compilation Unity et ergonomie mobile bloquées par l'environnement ; cinq tests codec et deux nouveaux cas UI restent à valider sous Unity. Aucune capture actuelle du worktree. Travail sur le worktree initialement propre, base `4a411995`, sans push ni merge main ; revue ciblée et `git diff --check` terminés sans anomalie.
