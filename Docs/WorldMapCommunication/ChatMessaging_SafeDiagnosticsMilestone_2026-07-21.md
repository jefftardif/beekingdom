# Bee Kingdom - Jalon diagnostics surs du chat

Date: 2026-07-21  
Agent: `Communication`

## Livraison

`IChatDiagnosticsSink` recoit maintenant des evenements structures limites a:

- code d'evenement;
- operation generique;
- statut HTTP;
- categorie d'erreur;
- code serveur stable;
- numero de tentative;
- compteur agrege.

Le modele ne possede aucun champ pour corps original/traduit, jeton, chemin URL,
identifiant joueur, conversation, message ou requete. Les evenements couvrent:

- capacites acceptees/refusees;
- connexion polling/realtime et fallback;
- retry de polling;
- trou de sequence sous forme de taille uniquement;
- tentative et acquittement outbox;
- erreur HTTP par statut/categorie/code stable.

Une exception du sink est absorbee: les diagnostics ne peuvent jamais casser une
action de jeu ou une synchronisation.

## Verification

- 55 tests Communication executes;
- 55 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les tests serialisent les evenements et prouvent l'absence du corps, du jeton, des
identifiants et du detail serveur. Ils valident aussi un sink qui leve une
exception et un trou de sequence rapporte uniquement par compteur.

## Handoff Integrateur

Le serveur doit adopter la meme discipline pour ses metriques et journaux:
statuts, codes stables, latence, cache hit/miss et compteurs uniquement. Aucun
corps original/traduit, bearer, identifiant brut ou curseur opaque ne doit etre
journalise. Les identifiants de correlation, s'ils deviennent necessaires, doivent
etre non reversibles, ephemeres et documentes.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_SafeDiagnosticsMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, secret, drapeau de production ou synchronisation n'a ete
ajoute ou active.
