# BeeKingdom — Nurserie royale, cohortes serveur v1 (prototype désactivé)

Date : 14 octobre 2026. Vision produit approuvée par le fondateur.
Base de référence : commit 609401a8. Branche isolée : bkstudio/nursery-growth-20261014.
Aucun code de cette branche n'est fusionné dans main, poussé vers GitHub ni déployé sur SRVESDT.

## Objectif

Définir des conséquences mesurables de la Nutrition et de la Stabilité :
- Nutrition : vitesse de croissance des couvées.
- Stabilité : rendement de recrues à maturité.
- Qualité globale : minimum de Nutrition/Stabilité, jamais la moyenne.
- Les recrues maturées sont déposées dans une réserve séparée, **pas dans l'armée existante**.

## Fonctionnement certifié en isolation

- Moteur purement déterministe : 37 nouveaux tests de règles ont réussi.
- Etat nullable NurseryCohorts ajouté à PlayerHiveState; anciennes sauvegardes compatibles.
- NurseryCohortService utilise IHiveStateRepository.ExecuteAtomicallyAsync, une révision optimiste,
  des clés idempotentes et des reçus anti-rejeu.
- Cohorte de base niveau 1 : 30 recrues, 60 minutes, capacité de réserve 120.
- Départ refusé si bâtiment non construit, couvain absent, réserve insuffisante,
  cohorte déjà en cours, révision périmée ou ruche du joueur inexistante.
- Maturation sur l'heure du serveur, crédit unique dans la réserve.
- GET authentifié : état réel, minuterie et prévision de la prochaine couvée.
- POST authentifié : démarrage d'une couvée, sans coût inventé ni client autoritaire.
- Les routes GET /brood/cohorts et POST /brood/cohorts/start sont ajoutées sur la branche,
  avec identité joueur tirée du jeton et hiveId validé.
- 11 tests de service ciblés réussis; 10 tests HTTP ciblés réussis
  en Debug et Release. 14 tests combinés couvées + soins existants réussis.
- Réglage NurseryCohorts.Enabled explicitement false dans appsettings.json et
  appsettings.Production.json; false aussi par défaut dans NurseryCohortOptions.

## Preuves de non-régression

Suites Hive Operations :
- Base 609401a8 : 198 réussis, 2 échecs préexistants / 200.
- Branche après couvées : 246 réussis, exactement les mêmes 2 échecs / 248.

Suite HTTP serveur complète (exécution avant les 3 derniers cas de sécurité) :
- Base : 671 réussis, 3 échecs préexistants, 8 ignorés / 682.
- Branche : 674 réussis, exactement les mêmes 3 échecs, 8 ignorés / 685.

Tests de sécurité ajoutés et validés depuis : ruche inexistante = HTTP 404
et aucune création implicite; autre compte ne peut lire ni démarrer la couvée;
identifiant de ruche invalide = HTTP 400.

Les résultats complets sont dans :
C:\BeeKingdom\DEV\BK-Studio\reports\nursery-growth-20261014
avec les fichiers TRX et final-qa-comparison.json.

## Sécurité et limites du prototype

- Pas d'activation de la nouvelle fonctionnalité : le serveur de production est inchangé.
- Aucune modification de l'API ou des ressources de soins existants (300 miel, 45 cire,
  12/13 secondes, +22/+7).
- La Caserne et l'Infirmerie n'ont pas été modifiées. La réserve ne finance pas encore
  les entraînements; une fois pleine elle bloque les nouveaux départs, sans perte de recrues.
- Les niveaux avancés et plusieurs couvées simultanées restent à concevoir et équilibrer.
- Les étapes visuelles (œuf, larve, nymphe), les animations et la lecture client Unity
  ne sont PAS encore implémentées.
- Le dépôt JSON de tests sérialise durablement. Les tests SQL dédiés ne sont pas encore
  certifiés sur un serveur de staging avec base de données : 8 tests de la suite HTTP
  restent ignorés comme sur la référence.
- La vérification préalable de ruche existante évite les créations involontaires en
  conditions normales, mais une suppression concurrente entre la lecture et l'écriture
  requiert une primitive de dépôt « mutation seulement si déjà existant » si l'on veut
  une garantie atomique parfaite. A couvrir avant toute activation publique.

## Prochaine tranche avant Play et production

1. Construire le client réseau Unity authentifié et son modèle de lecture,
   avec gestion d'erreur, retry et feature flag désactivé.
2. Ajouter la carte Couvées à la Nurserie premium sans casser Nourrir/Stabiliser.
3. Tester la progression en Play et les sorties de veille/reconnexion.
4. Certifier les migrations SQL, la sécurité concurrente et le comportement à long terme.
5. Revoir le balancing et décider ensuite d'activer uniquement en staging.
6. Établir l'intégration Caserne/réserve avec migration non destructrice, séparément.

**Statut : preuve technique en isolation. Le premier vrai parcours en Play reste à livrer.**
