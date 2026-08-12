# Bee Kingdom - Jalon presentation de traduction

Date: 2026-07-21  
Agent: `Communication`

## Livraison

`ChatTranslationController` porte maintenant l'etat de presentation sans modifier
le message officiel. Pour chaque message, il expose:

- `Original` avec le corps serveur intact;
- `Loading` tout en laissant l'original visible;
- `Translated` uniquement apres une reponse `completed` non vide;
- `Error` avec l'original visible et un code d'erreur explicite.

Une annulation restaure l'etat original et est propagee a l'appelant. La commande
`ShowOriginal` remplace l'affichage traduit sans effacer le cache du provider. Un
nouveau choix de langue utilise la cle distincte message, locale cible et version
de modele.

La traduction ne cree aucun `RemoteChatMessage`, ne change aucune sequence et ne
touche jamais au statut de moderation.

## Verification

- 20 tests Communication executes;
- 20 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux tests couvrent un fournisseur 503, la conservation de l'original,
une traduction ulterieure reussie, le retour manuel a l'original et l'annulation.

## Handoff Integrateur

Le client interprete 503 comme fournisseur indisponible temporaire, 429 comme
limite de debit, 401 comme session a renouveler et 403 comme refus definitif.
Integrateur doit conserver des corps d'erreur sans texte original, avec un code
stable localisable, et ajouter metriques/journaux sans contenu de message avant
toute activation du fournisseur.

## Fichiers du jalon

Crees:

- `Assets/BeeKingdom/Gameplay/Communication/ChatTranslationController.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ChatTranslationController.cs.meta`
- `Docs/WorldMapCommunication/ChatMessaging_TranslationPresentationMilestone_2026-07-21.md`

Modifie:

- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucune interface LivingHive, scene, image, carte ou configuration publique n'a ete
modifiee. Aucun deploiement ni synchronisation n'a ete effectue.
