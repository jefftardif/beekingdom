# Chat et messagerie — validation du cache de réponse capabilities

Date : 2026-07-21  
Responsable : Communication

## Résultat

`ChatTransportResponse` expose maintenant `CacheControl` et `AgeSeconds`. Le transport Unity lit les en-têtes HTTP `Cache-Control` et `Age` sans les mélanger au corps ou aux diagnostics.

Une réponse capabilities réussie est acceptée seulement si elle contient les directives `no-store`, `no-cache` et `max-age=0`, sans distinction de casse ni d'espacement. `Age` doit être absent ou égal à zéro. Toute autre réponse produit `RemoteChatError.Incompatible`, code `capability_cache_policy_invalid`, HTTP 0.

Le rejet survient avant l'installation de `NegotiatedCapabilities`, la création du bail, l'acquisition de session ou toute opération métier. Une réponse potentiellement ancienne ne peut donc pas ouvrir le chat avec une rétention, une limite ou une porte périmée.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 85/85 réussie.
- `Cache-Control: public, max-age=300` rejeté avant session.
- Politique correcte avec `Age: 1` rejetée avant session.
- Politique correcte avec âge nul acceptée; capabilities sans bearer et requête métier suivante authentifiée.
- Les 84 essais précédents restent verts.
- Aucun déploiement, activation ni synchronisation effectué.

## Directive d'intégration

Ajouter les en-têtes exigés directement dans l'application serveur, puis vérifier qu'IIS et le proxy les préservent sans injecter un `Age` positif. Le préflight doit contrôler les valeurs finales reçues après TLS, pas uniquement la configuration source. Ajouter des tests HTTP sous le runtime .NET 8 pour politique correcte, en-tête absent, réponse `public`, `max-age>0` et `Age>0`. `/capabilities` doit rester sans redirection et sans bearer. Le candidat local doit être reconstruit seulement après passage de ces contrôles et du nouveau champ de rétention; il reste `DeploymentAuthorized=false`.
