# 10 — Combats, escortes et conflits pour les fleurs

## Séparation essentielle

**[APPROUVÉ]** Les butineuses sont **civiles**, les militaires **protègent ou combattent**. Une mission florale peut inclure une escorte, sans convertir la population civile en troupes ni voler des effectifs à la Caserne.

## Trois environnements d'expédition

1. **Zone publique protégée** : butinage sans obligation d'escorte ; protection novice et restrictions d'attaque à définir.
2. **Zone sauvage/dangereuse** : risque PvE (prédateurs, parasites, embuscades) ; escorte optionnelle ou fortement utile.
3. **Zone contestée / territoire hostile** : objectifs économiques attractifs, défenses, accès diplomatique et règles PvP explicites.

Les zones sont **des statuts/permissions de gameplay** superposés à la carte existante, sans modification des tuiles.

## Escorte militaire

- À la préparation : civils engagés N, militaires engagés M via service de réservation de troupes existant (si compatible), championne civile et éventuelle championne militaire séparées.
- Une escorte doit avoir une portée/durée/condition d'utilisation compatibles et ne peut escorter deux convois avec les mêmes soldats.
- Le résultat combat est calculé **côté serveur**, pas via bouton client.
- Pendant combat, essaim civil peut rester protégé, battre en retraite ou subir pertes de cargaison/blessures selon règles.
- Si l'escorte est vaincue : pas de suppression silencieuse de l'essaim ; retour sécurisé et rapport clair.

## Conflits de relais et routes

- Un relais floral est un actif d'alliance attaquable dans des fenêtres définies.
- Une attaque réussie peut transférer contrôle économique, accès aux routes et bonus logistiques **sans effacer la fertilité**.
- Possibilité future : blocus d'accès à une route, escarmouches, patrouilles, reconquête, traité de passage.
- Les batailles doivent présenter un objectif économique visible et un coût réel pour l'attaquant.
- L'alliance vainqueure doit entretenir l'infrastructure si elle veut conserver l'avantage.

## Garde-fous

- Protection effective des premiers niveaux et des zones publiques de départ.
- Interdiction des attaques systématiques sur civils novices ; griefing, harcèlement et exploitation à surveiller.
- Plafonner les pertes définitives de civils, privilégier retraite, blessures récupérables, cargaison partiellement perdue.
- Pas de destruction définitive d'écosystèmes ; l'écologie se régénère.
- Prévoir fenêtres de vulnérabilité des relais compatibles avec fuseaux horaires et activités réelles.
- Jamais de « cible encore présente après victoire » si l'issue du combat doit la retirer : la mise à jour de carte doit se caler sur événement serveur et états d'occupation.

## Événements PvE

Prédateurs, araignées, pucerons, créatures attirées par le miel et **ours** peuvent perturber un secteur, mais l'ours a un système mondial spécifique (voir 11). Les joueurs spécialisés en combat sont utiles pour rendre l'activité économique possible.

## Intégration avec systèmes existants

Contrats à auditer avant implémentation :
- CombatPatrolService / CombatPatrolResolution ;
- CombatSquadReservationService / HiveTroopDeploymentAccounting ;
- WorldResourceCollectionService (actuellement réservation CommittedTroops) ;
- Défense, Infirmerie et Hall des championnes.
Ne pas modifier ces systèmes par remplacement massif. Adapter les réservations militaires via une interface stable, tout en ajoutant un stockage civil indépendant.

## Scénarios de tests

Escorte indisponible, double réservation, coupure réseau pendant combat, migration de propriété d'un relais, attaque d'un civil protégé, deux batailles simultanées, récompense de combat déjà réclamée, civils en retour quand le pacte expire, historique cohérent après victoire.

