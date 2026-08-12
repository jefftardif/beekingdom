# Préflight d’intégrité du candidat serveur

Candidat vérifié: `BeeKingdom.Server.20260721T195555Z`.

- 54 fichiers manifestés; 0 divergence SHA-256.
- 0 symbole PDB et aucune configuration Development publiée.
- Aucun secret, bearer, mot de passe ou chaîne de connexion dans les fichiers de configuration du candidat.
- `DeploymentAuthorized=false`, `ChatEnabled=false`, `RealtimeEnabled=false`, fournisseur de persistance `InMemory`, état `PreparationOnly`.
- Smoke local: `Healthy`, protocole `chat-v1`, rétention des reçus 30 jours.
- Re-smoke direct du binaire publié avec `ASPNETCORE_ENVIRONMENT=Production`: `Healthy` sur loopback, sans activation chat.

Cette vérification est locale et non destructive. Elle ne constitue pas une autorisation de transfert, de déploiement ou d’activation publique.
