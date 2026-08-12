# Bee Kingdom - Chat Messaging Communication Relaunch Status

**Date:** 2026-07-16  
**Scope:** serveur chat/messagerie et coordination communication uniquement  
**Images/Unity/APK:** non touches

## Etat court

Validation communication prise en compte apres lecture des rapports frais dans `Docs/WorldMapCommunication`.

Les agents locaux suivants sont consideres relances cote coordination:

- `IMAGE_LOCAL_AGENT_RELAUNCHED=YES`
- `QA_LOCAL_AGENT_RELAUNCHED=YES`
- `BUILDERC_LOCAL_AGENT_RELAUNCHED=YES`
- `COMMUNICATION_LOCAL_AGENT_RELAUNCHED=YES`

## Chat/messagerie

- `CHAT_PROD_LIVE=YES`
- `CHAT_HEALTH_LAST_REPORTED_OK=YES`
- `CHAT_CAPABILITIES_LAST_REPORTED_OK=YES`
- `CHAT_SIGNALR_NEGOTIATE_LAST_REPORTED_OK=YES`
- `CHAT_WEB_TEST_PAGE_LAST_REPORTED_OK=YES`
- `CHAT_SERVER_WORK_CONTINUES=YES`

Le chantier serveur chat/messagerie reste ouvert pour la suite: provider Unity REST/SignalR, durcissement auth/session client, tests de reconciliation realtime/polling, et documentation de handoff client. Aucun changement serveur live n'est effectue par ce checkpoint.

## Gates fermees

- `READY_FOR_FINAL_50X50=NO`
- `READY_FOR_QA_BUILDERC=NO`
- `READY_FOR_UNITY_HANDOFF=NO`
- `MASTER_25600_AUTHORIZED=NO`
- `UNITY_MAP_FINAL_GATE=CLOSED`
- `FINAL_MAP_GATE=CLOSED`

## Restrictions maintenues

- Pas de modification images.
- Pas de modification Unity.
- Pas de modification APK.
- Pas de modification carte finale.
- Pas de promotion QA/Builder-C/Unity tant que les gates ci-dessus restent fermees.

## Prochaine action serveur minimale

Preparer la tranche suivante du chantier chat/messagerie: `ServerChatProvider` cote Unity documente/planifie contre `https://chat.dravii.com`, sans integration Unity effective tant que `READY_FOR_UNITY_HANDOFF=NO`.

## Exigence ajoutee le 2026-07-20 - traduction a la demande

Le chantier reste en pause, mais son contrat produit inclut maintenant la traduction d'un message etranger par une commande `Traduire` placee avec le message. Le joueur peut ensuite revenir au texte original. L'original reste la donnee officielle et moderee.

La reprise devra ajouter une traduction serveur authentifiee, limitee en debit et mise en cache par `message_id + target_locale + translation_model_version`. La reponse conserve la langue source detectee, la langue cible, le fournisseur/version, le statut et le texte traduit. Une traduction existante est partagee entre les lecteurs de meme langue; elle ne cree pas un second message et ne contourne pas la moderation.

Les libelles d'interface francais et anglais sont reserves dans les catalogues `chat.*`. Voir `Docs/Product/BeeKingdom_Localization.md`. Aucune requete de traduction, aucun endpoint et aucun deploiement serveur ne sont actives par cette note.
