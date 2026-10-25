# Pollinisation BeeKingdom — contrôle SQL QA local

Ce programme exécute **de vraies transactions SQL Server** sur des comptes/sites temporaires et nettoie ses données à la fin. Il ne doit **jamais** être exécuté sur la base de production.

## Configuration isolée PCJEFF (Windows)

Pré-requis : SQL Server LocalDB 2025, .NET SDK, sqlcmd. Instance dédiée : `BKPollinationQA`. Base indépendante : `BeeKingdomPollinationQA`.

1. Créer et démarrer une instance LocalDB distincte, si elle n'existe pas :

~~~powershell
SqlLocalDB create BKPollinationQA
SqlLocalDB start BKPollinationQA
~~~

2. Créer la base de données QA avec SQLCMD, puis exécuter les scripts **dans cet ordre** :
`Server/src/BeeKingdom.Database/Scripts/070_hive_operations.sql`,
`096_floral_site_states.sql`, `097_floral_civil_contributions.sql`.

3. Depuis le dépôt de travail :

~~~powershell
dotnet run --project Server/ops/pollination-sql-qa/PollinationSqlQa.csproj
~~~

Le programme utilise, par défaut, l'authentification Windows intégrée et le serveur `(localdb)\BKPollinationQA`. Dans un autre environnement SQL **de test uniquement**, définir la variable `BEEKINGDOM_POLLINATION_QA_SQL` avec une chaîne de connexion de test. Ne jamais commiter cette variable ni une chaîne contenant un secret.

## Ce qui est prouvé

- Deux ruches réelles en SQL lancent des essaims civils vers une réserve florale **partagée** ; la somme des récompenses ne dépasse pas le stock commun.
- Les requêtes rejouées et les règlements concurrents ne produisent ni double débit ni double crédit.
- Une erreur SQL volontaire dans le journal des contributions provoque un rollback des écritures de la ruche, du champ et de la file.
- Après redémarrage des objets applicatifs, les missions conservées et le registre SQL restent cohérents.
- Le processeur SQL des échéances termine une nouvelle mission sans intervention d'un joueur et vide la file.
- Le nettoyage final est limité aux GUID / WorldId nouvellement créés par ce programme.

**Non démontré ici :** Unity connecté, charge MMO, SQL distant SRVESDT, géolocalisation des sites officiels, projets collectifs d'alliance, optimisation de la table des contributions ou tests de restauration d'une sauvegarde réelle. Feature flags désactivés par défaut.