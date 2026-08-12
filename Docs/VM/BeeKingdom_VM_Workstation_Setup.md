# Poste de travail Bee Kingdom dans Hyper-V

## Objectif

L'ordinateur principal et la VM utilisent chacun une copie locale du projet. Unity
ne doit jamais ouvrir directement le projet depuis un lecteur redirige ou un partage
reseau.

Cette organisation permet:

- a Codex de travailler dans la VM pendant que l'ordinateur principal reste libre;
- de tester Bee Kingdom sur l'ordinateur principal;
- de ne jamais partager `Library`, `Temp`, `Logs` ou les sorties de build;
- de bloquer les conflits au lieu d'ecraser silencieusement un fichier.

## Emplacements

- Projet principal: `C:\projets\beekingdomgame-master`
- Projet local dans la VM: `C:\projets\beekingdomgame-master`
- Projet principal vu depuis la VM:
  `\\DESKTOP-D3D29K7\BeeKingdomHost`
- Acces de secours en session etendue:
  `\\tsclient\C\projets\beekingdomgame-master`
- Outil de synchronisation:
  `tools\vm-sync\BeeKingdom-VmSync.ps1`
- Rapport dans la VM:
  `.codex\vm-sync-last-report.txt`

## Partage recommande

Le dossier `C:\projets\beekingdomgame-master` est partage uniquement sous le nom
`BeeKingdomHost`. La VM y accede avec les identifiants Windows de l'ordinateur
principal par `\\DESKTOP-D3D29K7\BeeKingdomHost`.

Sur l'ordinateur principal, lancer une seule fois
`tools\vm-sync\Configurer-Partage-Hote.cmd` et accepter la demande administrateur
Windows. Le configurateur limite le partage au compte Windows courant et n'active
pas la mise en cache hors connexion.

Ce partage prive remplace avantageusement la redirection de lecteur: il fonctionne
en session Hyper-V normale, lorsque VMConnect est reduit et apres une reconnexion.
Si le nom de l'ordinateur n'est pas resolu, l'outil detecte automatiquement la
passerelle privee Hyper-V et essaie le partage `BeeKingdomHost` a cette adresse.

## Configuration Hyper-V de secours

Dans les parametres de VMConnect:

1. Activer le mode de session etendue.
2. Ouvrir `Ressources locales`, puis `Plus`.
3. Cocher le lecteur `C:` de l'ordinateur principal.
4. Enregistrer les parametres de connexion.

La fenetre VMConnect doit rester connectee pendant une synchronisation. Elle peut
etre minimisee. Codex et Unity continuent de travailler localement dans la VM si la
connexion est temporairement fermee; la synchronisation attend simplement la
prochaine connexion.

Si `\\tsclient\C` est introuvable, fermer uniquement la fenetre VMConnect sans
arreter la VM. A la reconnexion, ouvrir `Afficher les options > Ressources locales >
Plus > Lecteurs`, cocher le lecteur `C:` et enregistrer les parametres.

## Installation Unity dans la VM

- Unity Editor `6000.5.3f1` revision `c2eb47b3a2a9`
- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

Apres l'installation, Unity doit afficher des chemins valides dans
`Edit > Preferences > External Tools` pour le JDK, le SDK et le NDK.

## Initialisation

L'initialisation copie une seule fois le projet principal vers le disque local de la
VM et cree l'etat de comparaison. La destination doit etre vide ou absente.

Cette procedure ne demande aucun compte Git ou GitHub. Dans l'Explorateur de la VM,
ouvrir `\\DESKTOP-D3D29K7\BeeKingdomHost`, puis aller dans `tools\vm-sync` et
double-cliquer sur `Initialiser-BeeKingdom-VM.cmd`.

La commande equivalente est:

```powershell
powershell -ExecutionPolicy Bypass -File "\\DESKTOP-D3D29K7\BeeKingdomHost\tools\vm-sync\BeeKingdom-VmSync.ps1" -Mode Initialize
```

## Routine de travail

Avant de commencer dans la VM:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\projets\beekingdomgame-master\tools\vm-sync\BeeKingdom-VmSync.ps1" -Mode Sync
```

Il est aussi possible de double-cliquer sur
`tools\vm-sync\Synchroniser-BeeKingdom.cmd` dans la copie locale de la VM.

Apres une modification verifiee dans la VM, executer la meme commande. Les fichiers
modifies dans la VM sont alors publies vers l'ordinateur principal. Les changements
effectues entre-temps sur l'ordinateur principal sont recuperes dans la VM.

Pour inspecter sans copier:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\projets\beekingdomgame-master\tools\vm-sync\BeeKingdom-VmSync.ps1" -Mode Status
```

## Conflits et suppressions

- Un fichier modifie des deux cotes n'est jamais ecrase.
- Le conflit est inscrit dans `.codex\vm-sync-last-report.txt`.
- Une suppression reste en attente par defaut.
- Apres verification du rapport, les suppressions peuvent etre appliquees avec
  `-Mode Sync -ApplyDeletions`.

## Regles Unity

- Ne jamais ouvrir la meme copie physique dans deux editeurs Unity.
- Ne pas copier `Library`, `Temp`, `Logs`, `obj`, `Builds` ou `UserSettings`.
- Sortir du mode Play avant de synchroniser une scene ouverte sur l'ordinateur
  principal.
- Laisser Unity terminer son importation avant de lancer un test.
- Les validations graphiques finales DirectX 12 restent a confirmer sur l'ordinateur
  principal tant que la VM n'utilise pas une acceleration GPU equivalente.

## Validation de l'outil

Le 20 juillet 2026, l'outil a ete verifie avec deux projets Unity temporaires:

- copie initiale: reussie;
- nouveau fichier VM vers ordinateur: reussi;
- nouveau fichier ordinateur vers VM: reussi;
- conflit bilateral: detecte et bloque;
- suppression sans autorisation: conservee et signalee;
- suppression avec `-ApplyDeletions`: appliquee;
- exclusions `.codex`, `Library` et `Build`: confirmees.
