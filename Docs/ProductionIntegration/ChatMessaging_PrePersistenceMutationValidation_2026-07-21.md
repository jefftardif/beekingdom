# Validations avant persistance — contrat borné

Le contrat serveur est aligné avant tout journal, reçu ou écriture SQL. Les identifiants de requête sont opaques, 1..256, sans espaces de bord; corps 1..4000; canal connu; game/world <=64; audience/titre <=256; participants <=100 et au moins un destinataire privé; catégorie 1..64. Une donnée restaurée invalide reste conservée localement mais n'est jamais envoyée.

Migration additive: `064_chat_contract_bounds.sql` élargit sans réécrire 060-063 les colonnes ClientRequestId à 256, Body à 4000, locales à 35, modèle à 128 et texte traduit à 16000. Le rollback `064_chat_contract_bounds.rollback.sql` refuse de réduire si des données dépassent les anciennes bornes.

Fichiers ajoutés:
- `Server/src/BeeKingdom.Database/Scripts/064_chat_contract_bounds.sql`
- `Server/src/BeeKingdom.Database/Scripts/064_chat_contract_bounds.rollback.sql`

Le catalogue d'exécution `DatabaseCatalog.Migrations` et le catalogue de rollback enregistrent désormais 064 dans l'ordre sûr. Reconstruction SQL jetable et tests de bornes (+1) restent une porte staging; aucune activation publique ni déploiement.

Nouveau candidat local après cette correction: `BeeKingdom.Server.20260721T195555Z`, smoke Healthy, `DeploymentAuthorized=false`.
