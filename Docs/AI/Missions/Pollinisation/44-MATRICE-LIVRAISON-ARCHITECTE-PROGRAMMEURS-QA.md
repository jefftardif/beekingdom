# 44 — Répartition de tâches possibles entre architecte, programmeurs Unity/API, QA et game design

Ce fichier sert à déléguer du travail à un agent codeur ou à plusieurs agents **sans qu'ils se marchent dessus**. Ce n'est pas une autorisation de lancer des processus ou de déployer.

## Responsabilités

**Game Design / Product Owner :** garde la vision, tranche paramètres économiques, mortalité, propriété territoriale, échelle collective, règles de raid. Valide les ADR irréversibles.

**Architecte :** définit frontières domaine, IDs mondiaux stables, atomicité multi-agrégats, migrations, feature flags, versionnement et ports Alliance/Combat. Garantit qu'aucun composant ne recrée une capacité existante.

**Développeur serveur :** implémente données persistantes, cohortes civiles, sites partagés, transactions de mission, rewards, progression d'alliance, droits, événements et endpoints officiels. Tests unitaires/intégration/concurrence.

**Développeur Unity :** points d'intérêt, filtres, client API, préparation/ETA et rapport, fenêtres Activités v4, localisation bilingue, affections championnes ; projections basées sur état officiel.

**Graphiste :** éléments ponctuels d'activité (icônes premium, relais, essaims animés, marqueurs de zone) cohérents avec la direction existante ; PAS refonte des tiles.

**QA :** scénarios fonctionnels, fuzz, charge, invariants économiques, E2E multi-client et hors connexion, non-régression, visuels FR/EN.

## Contrats entre agents

Architecte produit avant code : datatypes stables, diagramme relationnel, API draft, liste migration, source de vérité, risques, versions et catalogue.
Serveur produit avant UI live : JSON contractVersion, endpoints GET et erreurs vérifiables, fixtures stables sur flag QA.
Unity produit : composants isolés et tests de présentation, aucune récompense mutée localement.
QA valide : même IDs/révisions entre clients ; que les tests soient réalisés, pas simulés.

## Parallélisme sûr

- **Parallèle possible après ADR :** UX maquettes + catalogue localisé + backend population civile.
- **Parallèle possible après schéma :** UI site en lecture seule + API lecture sites.
- **Séquentiel nécessaire :** moteur de transaction -> livraison de pollen -> récompenses -> contributions multi-alliances.
- **À différer :** pactes, relais, guerre et ours avant preuve du même état partagé.

## Périmètres Git

Un ticket = un périmètre explicite. Ne pas éditer à plusieurs simultanément HiveOperationModels.cs / Program.cs / HiveStateMigrator.cs sans coordination et intégration ordonnée. Les contrats modifiés doivent être annoncés à Unity et QA avant merge.

## Plan de première itération (résultat concret)

Architecture :
- Document ADR de transaction partagé et politique identité/collision.
- État initial des sous-modules Unity/serveur confirmé.

Serveur :
- CivilPopulation persistant + cohorte + API de lecture + test dédoublement.
- 2 sites floraux stables + lecture + mutation écologique contrôlée.
- Une expédition de butinage end-to-end sur flag QA.

Unity :
- Fiche site et mission officielle contrôlée.
- Rapport fr-CA/en-US + apparence civils distincte.

QA :
- Deux comptes, un site, concurrence/double clic, fermeture Unity, vérification soldes.

## Communication attendue

Chaque agent remet :
- numéro ticket et références de décisions/sections ;
- PR / commit ciblé ;
- fichiers exacts et contrats changés ;
- tests exécutés, sorties, erreurs ;
- flag activé ou non ;
- capture UI si présent ;
- problèmes d'intégration et risques.

L'architecte réconcilie les livraisons avant tout déploiement, préserve le travail concurrent et tient le registre d'idées à jour.

