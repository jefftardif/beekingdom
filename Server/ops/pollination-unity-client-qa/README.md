# BeeKingdom P5 — Vérifications du client Unity et du contrôleur

Ce banc .NET compile directement les sources réelles
Assets/BeeKingdom/Networking/PollinationContracts.cs,
Assets/BeeKingdom/Networking/PollinationClient.cs et
Assets/BeeKingdom/Playground/PollinationPresentation.cs, en simulant
uniquement les interfaces de transport et de session Unity.

Commande depuis la racine du worktree :

    dotnet run --project Server/ops/pollination-unity-client-qa/PollinationUnityClientQa.csproj

Résultats attendus : P5_UNITY_CLIENT_QA_PASS, P5_PRESENTATION_QA_PASS,
soit 33 contrôles PASS, sans crédit de ressource simulé.

Vérifications : encodage Unicode FR/EN, feature flag fermé,
sécurité des routes et identifiants, identité d'un snapshot, refus de
réserve épuisée et site dupliqué, retour d'authentification 401,
interdiction de changement de joueur, émission de commande civile
avec deux révisions et clé d'idempotence, refus de faux reçu,
séparation des projets d'alliance, refus de complétion solo, sélection de
site officielle, population bornée, absence de mutation optimiste,
réutilisation de la même clé après une réponse réseau perdue,
interdiction de seconde expédition et purge de données au logout.

Ce banc ne compile PAS UnityEngine, le rendu IMGUI ni la carte complète.
Le projet de compilation Unity minimal sous BK-Studio/qa compile six scripts
runtime P5 avec les vraies DLL UnityEngine et des dépendances simulées.
Les tests Unity Editor finaux ne seront valides qu'après réparation du
Unity Package Manager du jeu complet.

Aucun jeton réel, identité d'un utilisateur ni adresse de production
n'est utilisé par ces vérifications.
