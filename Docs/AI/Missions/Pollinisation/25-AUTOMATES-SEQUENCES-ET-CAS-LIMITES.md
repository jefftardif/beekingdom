# 25 — Automates de vie, séquences et comportement en cas de panne

## But

Un agent codeur ne doit jamais inventer l'issue d'un état transitoire. Les états doivent être persistants, déterministes et visibles côté client ; chaque mutation durable peut être rejouée après crash.

## Cohorte civile (recommandé)

~~~text
Uninitialized
    | activate_civil_faction (receipt once)
    v
ReadyForCohort -- start --> Incubating -- time reached --> AwaitingClaim
      ^                                                      |
      |--------- claim once + capacity check ----------------|
~~~

- Si capacité totale insuffisante au lancement : refus sans coût.
- Si le niveau de Nurserie diminue entre start et claim : ne pas perdre les civils ; conserver les adultes en réserve protégée ou bloquer nouvelles naissances selon politique.
- Si le joueur ferme Unity : le serveur continue via calcul horodaté.
- Un deuxième start avec même key/payload renvoie même cohorte ; nouvelle key concurrente échoue « busy ».

## Expédition civile complète (recommandé)

~~~text
Draft (client only)
  | start committed
  v
Outbound -> Gathering -> TravelingToDestination -> Pollinating
                                                   |
                                            optional conflict
                                                   v
                             Returning <- Interrupted/Defeated
                                 |
                                 v
                            AwaitingClaim? 
                                 |
                      settle exactly once
                                 v
                             Completed

Recalling -> Returning -> Completed
FailedBeforeStart -> no reservation/no debit
~~~

- Séparer « fin de la phase écologique » et « remise de ressources » pour qu'un claim tardif ne bloque pas la floraison de tous les autres.
- Transitions temporelles reposent sur serverNow >= phaseEndUtc avec horodatage de transition officiel.
- Les annulations ont une fenêtre définie ; sans autorisation, retour/rappel plutôt que suppression.
- Le moteur s'assure qu'un esclave/client ne décide ni des pertes ni des résultats.

## Séquence transactionnelle de démarrage (logique)

~~~text
1. Authentifier et vérifier owner hive, alliance, accès au site.
2. Valider IDs catalogue, route, famille, fenêtre florale et mode.
3. Vérifier idempotencyKey + hash(payload canonique).
4. Lire et verrouiller/contrôler versions des agrégats requis.
5. Vérifier slots, civils, championne, escorte, coûts et réserves.
6. Déduire coûts, réserver unités, créer expédition et dates.
7. Écrire ledger + reçus + nouvelles révisions DANS transaction cohérente.
8. Commit.
9. Renvoyer snapshot ; émettre événements/outbox APRES commit.
~~~

Si une défaillance survient avant commit, aucune écriture visible. Si après commit mais avant HTTP response, le replay de même clé retourne le reçu déjà sauvegardé.

## Séquence règlement écologique

~~~text
1. Identifier expédition due ; prendre lock idempotent.
2. Lire site(s), mission, pacte/projet et version du catalogue.
3. Régénérer le site jusqu'au timestamp contrôlé.
4. Déterminer pollen effectivement disponible/chargé/transporté.
5. Appliquer effet site + ledger contribution exactement une fois.
6. Écrire résultat de phase, révisions, outbox.
7. Passer à Retour et prévoir ETA de retour.
~~~

Le chargement et le dépôt sur deux sites **doivent être distincts** ; possibilité de réserver la cargaison à la source puis de valider à destination. Si crash entre ces étapes, l'ID de livraison et les étapes durables évitent de délivrer deux fois le même pollen.

## Séquence retour et récompense

~~~text
1. Vérifier phase/heure/identité.
2. Résoudre pertes/escorte déjà enregistrées, cargaison finale.
3. Appliquer crédit portefeuille, release civils et championne/escorte une fois.
4. Marquer completed/claimed, journaliser receivedBalance.
5. Transaction commit, puis réponse/replay stable.
~~~

Si modèle manuel choisi, autoriser claim depuis écran ; ne pas laisser les réservations bloquées indéfiniment à cause d'un joueur absent (auto-release au retour recommandé, récompense persistante réclamable).

## Cas limites incontournables

- Deux utilisateurs attaquent/collectent le même site : décisions déterministes, pas d'underflow.
- Deux clients du **même** compte tentent lancer ; le second respecte les effectifs réservés.
- Une cohorte mûrit pendant que le serveur est coupé : règlement sur lecture/commande suivante.
- Une alliance se dissout en plein projet : historique conservé, projets clôturés/gelés, civils en retour.
- Un pacte expire à mi-parcours : appliquer une politique publiée, sans perte arbitraire.
- Une cible fleurit puis devient dormante avant l'arrivée : retour/attente/remboursement défini.
- Une championne est affectée à une autre activité : service de réservation central, refus de double affectation.
- Deux missions arrivent au même instant : ordre déterministe et contrôle de stock.
- Nouvelle version de catalogue pendant mission : figer version et valeurs applicables au lancement, ou migration de mission explicite.
- Échec d'un push temps réel : API GET reconstruira l'état sans manque.
- Récupération d'une mission après plusieurs jours : ne pas décrémenter deux fois les récompenses ou pénalités de fatigue.
- Changement fuseau horaire client : invariants absolus UTC.

## Matrice de récupération

| Coupure au moment de… | Résultat attendu |
|---|---|
| avant transaction start | aucune mission ni débit |
| après commit, avant réponse | même key retourne même mission |
| après consommation pollen, avant dépôt | pollen réservé une fois, reprise de livraison |
| après dépôt, avant mise à jour projet | outbox/reconciliation applique une fois |
| après rewards commit, avant réponse | replay du receipt, zéro double gain |
| client fermeture brutale | statut/ETA restitués par GET |
| flag OFF pendant mission | drainage/retour, aucun essaim perdu |
| extinction alliance | droits nouveaux coupés, règlement historique sûr |

## Tests d'automates

Pour chaque transition : test d'état initial, test heure trop tôt, test revision obsolète, test replay même clé, test replay clé différente, crash/reprise et invariant de conservation.

