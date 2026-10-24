# 07 — Routes de pollinisation et expéditions civiles

## Pourquoi des routes ?

**[RECOMMANDÉ]** Polliniser doit demander plus que cliquer « récolter » : il s'agit de transporter du pollen entre **fleurs compatibles**, avec temps de trajet, capacité, danger, possibilités de coopération et rendement futur.

La route est une **mission sur des sites existants de notre carte** ; aucune refonte du paysage.

## Mission type

1. **Préparation** : vérifier identités, appartenance éventuelle à une alliance, disponibilité d'un essaim civil, championne, risque, droits d'accès, capacité, ressources pour partir.
2. **Chargement/source** : sélectionner le site source, sa famille et une charge de pollen possible ; réserver l'essaim et une part autorisée des ressources du site.
3. **Déplacement vers source** : le client affiche la marche de butinage, le serveur décide du temps.
4. **Butinage** : prélever la cargaison réelle, appliquer pression écologique selon le mode.
5. **Trajet vers destination** : destination florale compatible ; étapes intermédiaires/relais selon le niveau.
6. **Dépôt de pollen/pollinisation** : incrémenter contribution écologique et progression de projet, sans créditer deux fois un même chargement.
7. **Retour** : transporter les ressources autorisées et libérer l'essaim à l'arrivée.
8. **Récompense et rapport** : soldes/missions/écologie/projet actualisés, reçu idempotent et historique.

En Alpha minimal, il est possible de commencer par « un champ et sa zone locale de pollinisation », puis étendre aux **deux sites distincts** ; mais la structure de données doit permettre source -> destination -> retour sans migration invasive.

## Compatibilité florale

Famille de fleurs identifiée par un code stable, sans se reposer sur la couleur de l'icône : trèfle, fleurs sauvages, arbre mellifère, plantes rares (premières familles proposées).

Un trajet pollinisateur n'est valide que si :
- la source possède du pollen utilisable ;
- la destination reçoit cette famille ou une compatibilité explicitement définie ;
- la fenêtre de floraison est active ;
- l'essaim possède portée, charge et durée adéquates ;
- le trajet n'est pas interdit par le contrôle territorial / état de combat / pacte.

La compatibilité doit être lisible dans Unity avant validation, avec raisons de blocage.

## Modes et arbitrages

| Mode | Gain immédiat | Fertilité future | Usage typique |
|---|---|---|---|
| Intensif | Élevé | pression supérieure | urgence économique |
| Équilibré | Moyen | positif | routine durable |
| Restauration | Faible | fortement positif | projet collectif |

**[À TRANCHER]** La restauration nécessite-t-elle forcément une route inter-sites ou peut-elle agir localement avec pollen du même secteur ? Ne pas figer en code sans décision.

## Mécanique de coopération

Un projet de prairie peut demander **plusieurs routes actives complémentaires** : une route de fleurs communes, une de fleurs rares, ou un volume minimal de pollen provenant de plusieurs sources. Cela pousse les joueurs à se répartir le travail, notamment via pactes.

Un joueur seul reste limité par ses effectifs, sa portée, ses slots et un **plafond de contribution individuelle** au projet ; le nombre de participants qualifiés est calculé indépendamment des points.

## Automate de mission proposé

Brouillon -> Validée -> Civils réservés -> Aller source -> Butinage -> Transit -> Dépôt -> Retour -> Résolution -> Terminée.

États alternatifs : Refusée (aucun débit), Interrompue (politique de retour), Attaquée (résolution escorte/danger), Annulée avant départ (remboursement prévu), Expirée (rattrapage idempotent). Chaque transition a un horodatage serveur, revision et reçu.

## Gestion de concurrence et déconnexion

- Ne jamais dépendre d'une minuterie Unity pour la progression réelle.
- Reconnexion et fermeture de jeu n'annulent pas une expédition.
- Un nouveau lancement avec la même clé renvoie la même mission ; avec une autre clé, refuse si civils insuffisants.
- Deux butinages concurrents ne peuvent tous deux prélever le **dernier** pollen d'un site.
- Une modification de pacte ou un relais capturé **en cours de route** doit déclencher une règle déterministe (retour sûr recommandé), jamais une perte de données.
- Fin/claim rejouée : aucun double gain et aucune double libération de civils.
- Limiter les changements d'itinéraire pour éviter de transformer la carte en simulateur microgestion.

## Visualisation recommandée

Lignes fines ou trajets optionnels, marqueurs de source/destination, essaim animé, temps estimé serveur, statut visible à différents zooms. Les soldats d'escorte utilisent leurs marqueurs propres. Le filtre « Pollinisation » isole ces informations de la foule d'icônes.

## Cas Alpha concret

Un site source « Trèfles blancs », un champ de destination compatible, une ruche de joueur, un essaim de 100 butineuses (chiffre illustratif), 1 mission, 1 compteur de projet partagé par plusieurs participants : vérifier lancement, mouvement, dépôt, retour, crédit et enrichissement régional réel.

