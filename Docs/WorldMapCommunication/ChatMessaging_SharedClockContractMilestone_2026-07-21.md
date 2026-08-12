# Chat — contrat d’horloge unifié (2026-07-21)

## Résultat

Le blocage de compilation Unity causé par deux déclarations incompatibles de `IChatClock` et `SystemChatClock` est supprimé.

- Une seule interface de niveau espace de noms subsiste dans `RemoteChatContracts.cs`.
- Son instant UTC canonique est un `DateTime`, compatible avec le modèle local historique.
- `ManualChatClock`, le fournisseur local et le fournisseur serveur consomment maintenant ce même contrat.
- L’horloge factice des tests serveur convertit explicitement `DateTimeOffset` en UTC, sans dépendre d’une conversion implicite.

La classe privée imbriquée du fournisseur local reste un détail d’implémentation et ne redéclare aucun type de niveau espace de noms.

## Validation

- Inventaire source : une seule déclaration `IChatClock` et une seule déclaration publique `SystemChatClock` dans l’espace de noms Communication.
- Suite isolée Communication : **90/90 tests réussis**.
- Compilation du harnais : aucune erreur ni aucun avertissement.

La compilation Unity globale peut reprendre sans `-ignoreCompilerErrors`. Sa confirmation finale appartient au contrôle global de l’Architecte.

## Production

Ce correctif ne modifie aucun endpoint ni aucune donnée serveur. Le prochain candidat serveur doit néanmoins être construit depuis un état contractuel incluant ce jalon et rester `DeploymentAuthorized=false` jusqu’aux portes déjà documentées. Aucun transfert, déploiement, activation ou synchronisation n’est autorisé ici.
