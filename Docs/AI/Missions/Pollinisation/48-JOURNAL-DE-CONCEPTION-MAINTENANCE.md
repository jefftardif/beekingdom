# 48 — Comment maintenir cette documentation sans perdre les idées

## Pourquoi ce fichier

La conception de BeeKingdom évoluera. Pour ne pas perdre de mécanique ni donner à un agent une version contradictoire, chaque décision modifiant la pollinisation doit être traçable et liée aux tickets de code.

## Processus de décision

1. Le créateur propose/confirme une règle.
2. Inscrire date, source et statut dans 17-DECISIONS-OUVERTES.md.
3. Modifier 01-VISION-DECISIONS.md si le principe fondamental change.
4. Mettre à jour le document métier concerné (03 populations, 05 économie, 08 alliances, etc.).
5. Mettre à jour 18-TRACABILITE-IDEES.md sans supprimer l'idée d'origine : « remplacée par D-XXX le ... ».
6. Répercuter dans catalogues (27/39), backlog (20), tests (16/33/47) et API (24) si pertinent.
7. Annoter compatibilité migration et feature flags (23/37) si données durables impactées.
8. Lier PR/commit de développement et preuve d'acceptation à la décision.

## Statuts officiels de suivi

- IDEA : piste à étudier.
- RECOMMENDED : direction proposée, réversible.
- APPROVED : explicitement validée par le fondateur.
- SPECIFIED : suffisamment définie techniquement pour développer.
- IMPLEMENTING : code en cours.
- IMPLEMENTED_FLAG_OFF : présent sous flag fermé.
- QA_VERIFIED : preuve QA exécutée et conservée.
- RELEASED : en service réellement confirmé.
- SUPERSEDED : remplacée, archive maintenue.

L'emploi de termes anglais dans ce suivi est une convention de ticketing, pas une exigence d'interface joueur.

## Changelog

### 2026-10-20 — Dossier initial
- Décisions : troisième pilier pollinisation, système hybride, collectif, niveaux, essaims civils, ours mondial.
- Documentation : vision, systèmes, cartes, économie, architecture, Alpha/Bêta, QA, traçabilité.
- Audit local : services existants et lacunes constatées.
- Aucun code de pollinisation implémenté dans le cadre de cette mission de documentation.

### 2026-10-20 — Extension « agent codeur »
- Nouveaux contrats techniques : architecture détaillée, schémas, API JSON, automates, formules, catalogues, niveaux, sécurité, UX, tests, protocole de déploiement.
- Audit supplémentaire du moteur floral Unity et des modèles d'alliance, de collecte, Nurserie et Académie.
- Prompt autonome de délégation et procédure d'acceptation multi-joueurs.
- À confirmer ultérieurement : les valeurs finales, les arbitrages de progression et les fonctionnalités territoriales détaillées.

## Modèle de changement

~~~text
Date :
Auteur / validation :
Référence d'idée POL-XXX :
Décision D-XXX :
Ancienne règle :
Nouvelle règle :
Compatibilité avec version existante :
Documents mis à jour :
Tickets et tests associés :
PR / commit :
État de déploiement réel :
~~~

## Contrôle automatique conseillé

Vérifier qu'aucun lien vers un fichier de ce dossier n'est brisé, que chaque nouveau ticket a un ID référencé, que les textes FR/EN sont présents et que les exemples se déclarent **non approuvés**. Ajouter un rapport d'écart avant fusion de gros changements.

## Intégrité de la transmission

Lorsqu'un agent différent reprend la mission : il reçoit **tout le dossier et son index** et lit les décisions approuvées avant toute proposition nouvelle. Ne pas supprimer les idées reportées sous prétexte qu'elles ne sont pas à l'Alpha.

