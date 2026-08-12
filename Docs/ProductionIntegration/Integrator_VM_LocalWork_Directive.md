# Directive temporaire - Travail local de l'Integrateur dans la VM

Je confirme que `C:\\projets\\beekingdomgame-master` est ta copie locale autorisee
pour cette tranche. L'indisponibilite de `Z:` dans le bac a sable Codex est attendue
et ne doit pas bloquer ton travail.

Continue sans tenter de remapper le lecteur, de relacher le bac a sable ou d'ecrire
directement dans le partage.

## Perimetre exclusif

Ton perimetre exclusif est:

* `Server/`;
* les tests serveur associes;
* `Docs/ProductionIntegration/`;
* de nouveaux contrats d'integration isoles ne modifiant pas LivingHive.

Sont exclus de ton perimetre car attribues a l'agent `Communication`:

* `Server/src/BeeKingdom.Chat/`;
* les tests `Chat*` et `SignalRChat*`;
* `Assets/BeeKingdom/Gameplay/Communication/`;
* `Docs/WorldMapCommunication/`.

Ne modifie aucun fichier dans `Assets/`, aucune scene Unity, aucune interface
LivingHive, aucune image de ruche et aucun element de la carte 50x50.

L'Architecte travaille dans la meme copie locale, mais sur LivingHive. Inspecte
l'architecture serveur existante, etends la pile .NET/SQL actuelle et poursuis la
premiere tranche de persistance.

## Fin de tranche

A la fin:

1. executer les tests pertinents;
2. produire un rapport dans `Docs/ProductionIntegration/`;
3. fournir la liste exacte des fichiers crees ou modifies;
4. signaler tout conflit ou point d'integration necessaire avec LivingHive;
5. ne pas lancer la synchronisation finale.

La synchronisation finale sera realisee manuellement apres verification.
