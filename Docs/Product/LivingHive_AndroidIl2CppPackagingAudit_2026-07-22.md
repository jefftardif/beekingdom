# LivingHive — audit de construction Android IL2CPP

Date : 22 juillet 2026  
Responsable : Architecte  
État : conversion et compilation native atteintes; APK non produit; porte mobile encore ouverte

## Verdict

Une construction locale fermée de la scène `Assets/Scenes/LivingHive.unity` a
été lancée avec Unity 6000.5.3f1, Android, IL2CPP, ARM64 et un APK Development.
Elle a traversé la compilation des assemblages, la génération IL2CPP et la
compilation native ARM64. Gradle a ensuite refusé l'emballage à la tâche
`:launcher:compressDebugAssets` avec `Espace insuffisant sur le disque`.

Le journal mesure la scène à 2,02 Gio compressés et 2,25 Gio non compressés.
Aucun APK et aucun manifeste de succès n'ont donc été produits. Cette passe ne
ratifie pas un build Android installable et ne doit pas être présentée comme
telle.

## Harnais fermé

Le fichier
`Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveAndroidAotProofBuilder.cs`
impose les limites suivantes :

- scène unique : `Assets/Scenes/LivingHive.unity`;
- backend temporaire : IL2CPP;
- architecture temporaire : ARM64;
- format : APK Development, signature debug locale;
- aucun basculement de flag Production, aucun compte, secret ou `HiveId`;
- refus si `Assets/Resources/BeeKingdom/MobileAccountSessionRuntime.asset`
  existe;
- aucun transfert, installation sur appareil ou déploiement serveur;
- restauration des réglages Android antérieurs avant toute demande de sortie
  Unity.

La première version demandait la sortie batch dans le `try/catch`, avant que la
restauration du `finally` soit durablement sauvée par Unity. Après l'échec de
Gradle, les réglages étaient restés à IL2CPP/ARM64. Ils ont été remis à leur
état antérieur (`scriptingBackend Android: 0`,
`AndroidTargetArchitectures: 3`) et le harnais restaure maintenant, appelle
`AssetDatabase.SaveAssets`, puis seulement `EditorApplication.Exit`.

## Preuves de la passe

Journal : `Artifacts/LivingHiveAndroidAotProof_Build.log`.

- Android Build Support, SDK, NDK, OpenJDK et Gradle ont été trouvés;
- 0 `error CS` et aucune panne de compilation d'assemblage n'ont précédé la
  construction du Player;
- les sorties IL2CPP sont visibles sous le chemin de construction Android;
- Gradle configure `libil2cpp.so` et
  `configureCMakeDebug[arm64-v8a]`;
- statistiques Player : 2,02 Gio compressés / 2,25 Gio non compressés;
- 33 tâches Gradle ont été exécutées;
- échec exact : `:launcher:compressDebugAssets` →
  `Espace insuffisant sur le disque`;
- résultat Unity final : `Failure`, 3 erreurs rapportées;
- APK attendu
  `Artifacts/AndroidAotProof/BeeKingdom-LivingHive-IL2CPP-arm64.apk` absent;
- manifeste attendu absent.

Cette trace prouve que les nouveaux contrats réseau ont franchi la conversion
IL2CPP jusqu'à la chaîne native ARM64. Elle ne prouve ni la fin du packaging,
ni le démarrage de l'application, ni AndroidKeyStore sur appareil.

## Nettoyage et retour à l'état produit

Les caches de cette passe sous `Library/Bee/Android` et
`Library/Bee/artifacts` ont été nettoyés après la fermeture de Unity. Trois
petits fichiers Gradle verrouillés, environ 150 Kio, sont restés sans valeur de
livrable. L'espace libre est remonté à environ 15 Gio. Aucun Asset, scène,
image de ruche ou source produit n'a été supprimé.

Contrôles finaux :

- Unity, dotnet, testhost, Java, Bee et clang : 0 processus;
- configuration runtime compte/serveur : absente;
- Android : Mono et architectures antérieures restaurées;
- APK et manifeste de succès : absents.

## Régression Unity après Android

Une validation F8 Windows a été tentée dans
`Artifacts/LivingHiveAndroidAotProof_ClosureF8.log`.

Le changement de cible et le rechargement d'assemblages se sont terminés sans
`error CS`, `Compilation failed` ou `AssertionException`. Le harnais n'a
toutefois jamais exécuté ses assertions : le client de licence Unity a échoué
trois fois à retrouver `com.unity.editor.headless`. L'unique processus Unity
batch a alors été arrêté de façon ciblée. Cette passe est classée
`non exécutée — licence`, et non verte ou rouge.

Le dernier F8 global ratifié demeure celui du jalon REST/cache protégé :
`Artifacts/LivingHiveAuthenticatedGameBridge_ClosureF8.log`.

## Conséquence mobile

Le poids observé est une porte produit, pas seulement une contrainte de la VM.
Avant une nouvelle tentative complète, il faut inventorier ce qui entre dans le
Player, retirer du build mobile les références de travail et preuves non
nécessaires, optimiser textures/audio, puis décider quels contenus restent dans
l'installation et lesquels sont téléchargés par lots versionnés.

L'audit suivant a localisé la cause dominante : 922,8 Mio / 5 004 fichiers sous
`Resources/WorldMapWave6Runtime`, dont 825,0 Mio pour les 2 500 tuiles exact-crop
canoniques non compressées sur Android et 97,8 Mio pour l'ancien paquet `v1`.
Les autres ressources totalisent 72,3 Mio. Voir
`Docs/Product/LivingHive_MobileContentOwnershipAndBudget_2026-07-22.md`.

La séparation d'autorité reste inchangée :

| Appareil | Serveur |
|---|---|
| exécutable, interface, assets essentiels, coffre Android, cache de lecture borné | comptes, sessions, ruche, temps, révisions, mutations et récompenses |
| contenus téléchargeables vérifiés et caches révocables | manifeste/version des contenus et autorisation de téléchargement |
| aucune récompense ni progression inventée hors ligne | validation finale, idempotence et persistance |

## Portes restantes

1. Rétablir Unity Hub/licence et relancer le F8 global.
2. Auditer les 2,25 Gio embarqués et définir un budget mobile par catégorie.
3. Retirer ou externaliser les contenus non requis au lancement sans toucher à
   l'image de base LivingHive ni au terrain protégé.
4. Produire un APK ou AAB IL2CPP ARM64 complet et inspecter package, taille,
   architecture et signature.
5. Tester sur un appareil Android réel : démarrage, mémoire, performance,
   AndroidKeyStore, corruption, reprise, TLS, authentification et cache offline.
6. Configurer HTTPS/SQL staging, comptes et `HiveId` réels; conserver les flags
   Production fermés jusqu'aux preuves.
7. Synchroniser vers l'hôte lorsque le partage `Z:` redevient accessible.

La frontière serveur du manifeste de contenu est désormais ratifiée localement
et fermée par défaut; elle ne lève aucune des portes de construction, CDN ou
appareil ci-dessus. Voir
`Docs/ProductionIntegration/WorldMap_ContentManifestBoundary_2026-07-22.md`.

La synchronisation officielle tentée à `2026-07-22T21:52:53Z` a échoué avant
toute copie par accès refusé au partage hôte. Aucun conflit n'a été créé et
aucun contournement par `Z:` n'a été tenté.

Communication est resté gelé; aucun module, test, catalogue ou document chat
n'a été modifié.
