# 38 — Protocole obligatoire de travail pour l'agent codeur

## Mission

Transformer progressivement le système de pollinisation décrit par les fichiers 01..37 en gameplay réel sans perdre une idée, sans supprimer du code existant, sans casser Unity, Alliance ou la production.

**Ce document est une instruction pour un futur agent**, pas une preuve que les opérations ont été exécutées.

## Hiérarchie des exigences

1. **Décisions du créateur marquées [APPROUVÉ]** dans 01 et 17.
2. Compatibilité et données réelles du dépôt, sécurité et résultats des tests.
3. Contrats gameplay recommandés dans les fichiers 02..37.
4. Paramètres exploratoires et exemples (jamais « décisions finales »).
5. Nouvelles décisions ADR enregistrées et soumises au créateur si elles changent durablement le jeu.

Si une proposition contredit les faits du code, **corriger la proposition dans la documentation avant d'écrire un patch**. Ne pas cacher le conflit.

## Démarrage de toute mission

- Exécuter git status et noter HEAD et fichiers préexistants modifiés.
- Lire les conventions du repo et le fichier 21-AUDIT-TECHNIQUE-CODE-CANONIQUE.md, puis re-vérifier le code actuel.
- Chercher l'existant avant de créer une classe/service/asset (FlowerManager, WorldResourceCollection, NurseryCohorts, Academy, AllianceResearch/Diplomacy).
- Préparer un plan de modification ciblé + tests de non-régression avant toute écriture.
- Si un autre agent travaille sur la même zone, éviter le conflit par branche/worktree sans invalider main.

## Restrictions

- NE PAS refaire le paysage et les tuiles.
- NE PAS fusionner population civile dans CommittedTroops, CombatRecruitment ou CombatSquadReservation.
- NE PAS fabriquer un nouveau compte de monnaie nectar ni nouveau bâtiment intraruche sans décision.
- NE PAS écrire fertilité officielle uniquement dans la simulation Unity.
- NE PAS attribuer des gains via un code client ou des faux endpoints.
- NE PAS effacer les modifications préexistantes de main ou faire git add . / git reset --hard.
- NE PAS afficher un bouton de mutation en prod avant contrat API fonctionnel.
- NE PAS utiliser WorldPresence (échantillon) comme index global de participants.
- NE PAS inventer un niveau d'alliance à partir d'un champ non existant ; implémenter la vraie progression.
- NE PAS considérer la présence visuelle de la tanière comme un raid déjà jouable.
- NE PAS changer une API publiée sous même version en cassant ses clients.
- NE PAS faire de déploiement/activation hors mandat.

## Périmètre d'un lot autonome

Un lot apporte UN gain E2E mesurable (par exemple : une cohorte civile durable) et reste derrière un flag. Conserver un test qui protège les systèmes impactés, pas simplement les nouveaux tests qui réussissent.

Chaque lot a 6 résultats :
1. Contrat/API schéma clair.
2. Code serveur autoritaire testé.
3. Client Unity avec état d'erreur et FR/EN si requis.
4. Migration compatible et receipt/replay.
5. Preuve d'exécution E2E, idéalement à 2 joueurs pour monde partagé.
6. Commit ciblé et rapport documentaire.

## Idempotence et transactionnel : critère bloquant

Si une mission retire une ressource du site A puis crédite une ruche B et un projet C, prouver que crash, retries et accès concurrents ne dupliquent **ni stock, ni pollen, ni honey, ni XP, ni points collectifs**.

Une synchronisation par l'UI seulement est un échec de conception. Une simple transaction par PlayerHiveState est insuffisante si le monde partagé vit ailleurs.

## Politique de décisions non validées

- Choix réversible de faible risque : choisir un défaut **configurable** selon 39, documenter l'ADR.
- Choix irréversible ou touchant la monétisation, la mortalité, les monnaies, le PvP et la propriété : ne pas le rendre définitif en douce. Préserver une abstraction et laisser la fonction désactivée si nécessaire.
- Aucune valeur exploratoire ne devient « officielle » sans tableau de simulation et validation.
- Les futurs choix du créateur mettent à jour 01, 17, 18 et les tests associés.

## Format du rapport en fin de lot

~~~text
BeeKingdom Pollinisation — Lot Pn
HEAD initial / HEAD final :
Décisions APPROUVÉES respectées :
Modules inspectés :
Fichiers modifiés / nouveaux :
Contrats/API :
Migrations, transactions, idempotency :
Compatibilité anciens comptes/systèmes :
Tests serveur — commande / résultat :
Tests Unity FR/EN — commande/capture / résultat :
Tests multi-joueurs/hors-ligne :
Flags / déploiement (NON ou OUI avec preuve) :
Risques, décisions ouvertes et tâches suivantes :
Commit(s) ciblés / PR :
~~~

## Ordre recommandé

P0 audit et architecture transactionnelle -> P1 population civile -> P2 sites floraux -> P3 mission E2E -> P4 collectif et niveau d'alliance -> extensions ; voir 14 et 20. Ne pas commencer la tanière de l'ours ou les guerres avant que le moteur de transactions partagées soit éprouvé.

