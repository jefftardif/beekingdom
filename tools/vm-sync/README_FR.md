# Synchronisation Bee Kingdom avec la VM

## Principe

Unity travaille toujours sur une copie locale du projet dans chaque Windows:

- ordinateur principal: `C:\projets\beekingdomgame-master`
- VM: `C:\projets\beekingdomgame-master`
- acces recommande depuis la VM a la copie principale:
  `\\DESKTOP-D3D29K7\BeeKingdomHost`
- acces de secours avec la session Hyper-V etendue:
  `\\tsclient\C\projets\beekingdomgame-master`

Le partage reseau est recommande. Il fonctionne en session Hyper-V normale et ne
demande pas que la fenetre VMConnect reste connectee pendant une synchronisation.

## Configurer l'ordinateur principal une seule fois

Sur l'ordinateur principal, double-cliquer sur:

`tools\vm-sync\Configurer-Partage-Hote.cmd`

Accepter la demande administrateur de Windows. Le configurateur partage uniquement
le projet Bee Kingdom, desactive sa mise en cache et limite l'acces au compte
Windows courant. Il ne partage aucun autre dossier.

## Initialisation dans la VM

La copie locale de destination doit etre vide ou absente.

Methode simple, sans Git et sans commande a saisir:

1. Dans l'Explorateur de la VM, ouvrir `\\DESKTOP-D3D29K7\BeeKingdomHost`.
2. Entrer les identifiants Windows de l'ordinateur principal si Windows les demande.
3. Aller dans `tools\vm-sync`.
4. Double-cliquer sur `Initialiser-BeeKingdom-VM.cmd`.

Si le nom de l'ordinateur n'est pas reconnu, ouvrir
`\\172.21.224.1\BeeKingdomHost`. L'outil essaie aussi automatiquement la passerelle
privee Hyper-V, dont l'adresse peut changer apres un redemarrage de l'hote.

Si le partage reseau n'est pas disponible, le lecteur redirige reste une solution de
secours: fermer seulement VMConnect, puis rouvrir la connexion avec `Afficher les
options > Ressources locales > Plus > Lecteurs` et cocher le lecteur `C:`.

Methode en ligne de commande:

```powershell
powershell -ExecutionPolicy Bypass -File "\\DESKTOP-D3D29K7\BeeKingdomHost\tools\vm-sync\BeeKingdom-VmSync.ps1" -Mode Initialize
```

## Voir les changements sans rien modifier

```powershell
powershell -ExecutionPolicy Bypass -File "C:\projets\beekingdomgame-master\tools\vm-sync\BeeKingdom-VmSync.ps1" -Mode Status
```

## Synchroniser dans les deux directions

Dans la copie locale de la VM, double-cliquer sur:

`C:\projets\beekingdomgame-master\tools\vm-sync\Synchroniser-BeeKingdom.cmd`

La commande equivalente est:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\projets\beekingdomgame-master\tools\vm-sync\BeeKingdom-VmSync.ps1" -Mode Sync
```

Les fichiers modifies d'un seul cote sont copies automatiquement. Un fichier
modifie des deux cotes est bloque et inscrit comme conflit dans:

`C:\projets\beekingdomgame-master\.codex\vm-sync-last-report.txt`

Pour exporter les deux versions sans rien remplacer, lancer depuis le lecteur
partage de la VM:

`Z:\tools\vm-sync\Exporter-Conflits-VM.cmd`

Les copies sont deposees sur l'ordinateur principal dans
`.codex\vm-sync-conflicts\<date>\vm` et `ordinateur`. Elles peuvent ensuite etre
fusionnees avant de recopier la version resolue dans les deux projets.

Apres fusion sur l'ordinateur principal, appliquer les versions resolues dans la
VM avec:

`Z:\tools\vm-sync\Appliquer-Resolution-Conflits-VM.cmd`

L'outil sauvegarde d'abord les anciennes versions VM sous
`.codex\vm-sync-conflict-backups\<date>`, copie les documents fusionnes et verifie
leurs empreintes SHA256. Relancer ensuite la synchronisation depuis `Z:` afin que
l'etat commun soit reconnu.

## Suppressions

Par securite, les suppressions ne sont jamais reproduites automatiquement. Elles
sont inscrites dans le rapport. Apres verification, elles peuvent etre appliquees
explicitement avec:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\projets\beekingdomgame-master\tools\vm-sync\BeeKingdom-VmSync.ps1" -Mode Sync -ApplyDeletions
```

## Dossiers toujours exclus

`.codex`, `Library`, `Temp`, `Logs`, `obj`, `Build`, `Builds`, `UserSettings`, les
sorties de validation, les fichiers de solution et les paquets Android ne traversent
jamais la synchronisation. Chaque ordinateur conserve donc ses propres caches Unity
et son propre etat Codex.
