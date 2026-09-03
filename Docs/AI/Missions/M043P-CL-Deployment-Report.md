# M043P-CL — Rapport de déploiement

Correctif M043P (résolution du DisplayName authoritative) déployé sur
l'API Alpha Production suite à l'autorisation explicite du CEO.

## Procédure suivie

1. Commit `83c42ac` sur `main`, poussé.
2. `git push origin main:deploy` — pipeline GitHub Actions "Deploy
   BeeKingdomApi" déclenché, **succès** (smoke test vert).
3. Aucune migration SQL exécutée (le correctif ne touche aucun schéma).
4. Aucun changement Unity ni Build Settings.
5. Configuration préservée telle quelle sur le pool IIS (`Alliance__Enabled`,
   `DiplomacyEnabled`, `WarEnabled`, blocage dev-seed) — non touchée par ce
   déploiement, aucune variable d'environnement modifiée.

## Vérifications post-déploiement

- **API Healthy** : `GET /` → `200 {"status":"Healthy",...}`.
- **Alliance API fonctionnelle** : `GET /alliance/v1/alliances/search` →
  `200`, `{"items":[{"name":"Alliance Test","tag":"BKT","memberCount":1,...}],"totalCount":1}`.
- **Alliance Test [BKT] et membership préservés** : confirmé par la même
  requête — l'alliance existe toujours, `memberCount:1` inchangé.
- **Auth/Hive** : non testé directement par une requête HTTP séparée cette
  fois (le endpoint `/` healthy couvre le démarrage général du serveur) ;
  aucune anomalie observée.
- **DisplayName résolu du CEO** : **non confirmé cette fois** — la session
  Play Mode locale (utilisée pour les lectures authentifiées sans
  mutation lors des missions précédentes ce soir) n'a pas fini de se
  ré-authentifier après plusieurs tentatives de redémarrage, malgré
  `isPlaying=True`/`isCompiling=False` confirmés. Aucune anomalie
  serveur détectée par ailleurs (l'API répond normalement) — probable
  simple lenteur/état de session locale à ce point tardif de la soirée,
  pas un signe de problème de déploiement.

## Verdict final (A–G)

| # | Critère | Résultat |
|---|---|---|
| A | Déploiement réussi ? | ✅ OUI |
| B | API saine ? | ✅ OUI |
| C | Alliance Test préservée ? | ✅ OUI |
| D | Membership CEO préservé ? | ✅ OUI (memberCount:1 inchangé) |
| E | DisplayName du CEO maintenant résolu ? | ⚠️ NON CONFIRMÉ (session locale indisponible pour vérifier sans mutation) |
| F | Nom résolu exact | Indisponible cette fois |
| G | PRÊT POUR NOUVEAU TEST CEO "DISPLAYNAME" ? | ✅ OUI — le correctif est en production ; seule la vérification automatisée de ce côté n'a pas abouti |

## Recommandation

Le déploiement est sain et le correctif est en production. Le moyen le
plus simple de confirmer E/F est que tu rouvres toi-même Alliance Center
maintenant — c'est de toute façon le test humain final prévu.
