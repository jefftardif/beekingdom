# LivingHive — frontière serveur des préférences appareil

Les réglages mobiles « Mouvement réduit » et « Mode économie » sont des préférences locales d'accessibilité/performance. Ils pilotent le rendu et le budget local d'abeilles, sans modifier ressources, progression, timers, autorisations ou résultats serveur.

Aucun contrat serveur n'est requis dans la tranche actuelle : pas d'endpoint, mutation, lecture authentifiée, reçu d'idempotence ou donnée autoritaire à synchroniser. La persistance versionnée reste côté appareil (PlayerPrefs ou équivalent protégé). Une future synchronisation de profil devra être explicitement non autoritaire et faire l'objet d'un contrat distinct.

Fichiers modifiés : uniquement ce rapport. Aucun fichier `Server/`, `Assets/` ou chat n'a été touché; aucun test/build/candidat/déploiement n'est nécessaire.
