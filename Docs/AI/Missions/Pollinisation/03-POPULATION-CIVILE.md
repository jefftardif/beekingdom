# 03 — Population civile : naissance, effectifs, états et cycle de vie

## Décision fondatrice

**[APPROUVÉ]** Les abeilles affectées à la pollinisation sont des **essaims civils spécialisés**, et ne sont pas des troupes de la Caserne. Une escorte militaire peut accompagner une expédition, mais les deux pools et leurs réservations restent distincts.

## Modèle de population recommandé

Une colonie gère un **stock civil total** (unités d'abeilles ou lots/escouades abstraits selon l'échelle retenue), dont les effectifs sont comptés de manière conservatrice :

- **En réserve** : abeilles adultes disponibles, non assignées.
- **En formation** : adultes réservées dans l'Académie ; indisponibles pour mission si formation individuelle retenue.
- **En expédition** : civils engagés en déplacement, au site ou en retour.
- **En récupération** : butineuses fatiguées, indisponibles jusqu'à fin de repos.
- **Blessées** : état récupérable selon règles de risque ; lien éventuel Infirmerie.
- **Perdues** : perte définitive uniquement si le Game Design l'autorise, jamais implicitement par un bug réseau.

**Invariant obligatoire :** Total civil durable = réserve + formation + expéditions + récupération + blessées + pertes comptées dans l'historique. Ne jamais compter le même civil disponible à deux endroits.

Le terme « population civile » ne doit pas être confondu avec les cohortes existantes, la population décorative du monde, les troupes militaires ni les comptes joueurs.

## Origine et cycle de création

**[RECOMMANDÉ]** La Nurserie devient le point d'origine des **cohortes civiles**. Un cycle d'élevage consomme des ressources déterminées par le serveur, dure une période contrôlée et produit des adultes disponibles lorsque le joueur **réclame** sa cohorte, selon le contrat choisi.

Cycle proposé :
1. Capacité et prérequis (niveau Nurserie, place libre, ressources et santé de la colonie) validés.
2. Démarrage transactionnel du lot : retrait des ressources **une seule fois** et création d'une cohorte civile distincte.
3. Maturation sur temps serveur ; même fermé, le compte garde sa cohorte.
4. Réclamation/achèvement idempotent ; la population réservée est transférée au stock adulte.
5. Les futurs niveaux de Nurserie augmentent capacité, nombre ou cadence des lots avec plafonds.

**[IMPORTANT]** NurseryCohortService.cs existe et gère un prototype de cohortes/réserve. Avant intégration, vérifier son usage exact et ses liens au recrutement. NE PAS utiliser RecruitsInReserve comme « civils gratuits » sans contrat explicite. Préférer un nouveau CivilCohortState additive/versionné ou une extension strictement compatible.

## Représentation des essaims

- **Population** : quantité d'abeilles civiles possédées.
- **Essaim** : groupe de taille N, composé d'une ou plusieurs spécialisations compatibles, créé lors de l'affectation (pas une duplication de population).
- **Emplacement d'expédition** : limite de missions civiles simultanées ; indépendante de la limite des marches militaires.
- **Formation d'essaim** : sélection N + rôle + championne facultative + escorte facultative ; allocations validées serveur.
- **Spécialisation** : attribut de compétence ou sous-type (à choisir), sans création gratuite d'effectifs.
- **Changement de spécialisation** : seulement pour civils non réservés, avec éventuel coût et délai ; pas de conversion instantanée en armée militaire.

## Capacité et progression

Facteurs candidats : Nurserie (population maximale / maturations), Académie (qualité), Recherche (santé/endurance), Génétique (aptitudes), technologie de logistique (distance), Palais royal (pré requis globaux).

Conserver la distinction entre :
- limite de **population** ;
- limite de **civils affectés** ;
- nombre de **missions simultanées** ;
- quantité de **civils par essaim** ;
- capacité de **stockage de récolte**.

Ne pas confondre ces 5 paramètres : une amélioration de l'Académie ne doit pas créer des abeilles automatiquement.

## Règles de blessure, récupération, décès

**[RECOMMANDÉ]** Pas de perte automatique de toute la population civile en cas de combat. Lors d'une attaque valide en zone dangereuse, il peut y avoir retraite, cargaison perdue, fatigue accrue ou blessés. L'Infirmerie pourrait accélérer les soins, sans détourner les blessés militaires existants.

Si une mission est abandonnée/expire/échoue, les réservations doivent être libérées exactement une fois. Si une route n'est plus accessible, appliquer une politique de retour au foyer, jamais effacer l'essaim.

## Automate d'états civil recommandé

Réserve -> Réservé -> En route -> Sur site -> Retour -> (Réserve | Récupération | Blessé).  
Formation : Réserve -> En formation -> Réserve améliorée.  
Maturation : Cohorte démarrée -> Prête -> Réclamée -> Réserve.

Commandes concurrentes doivent verrouiller par ruche et vérifier revision + clef d'idempotence ; la même abeille ne peut être utilisée deux fois.

## Anti-abus et expérience utilisateur

- Pas de gain de population par rafraîchissement de l'UI ou appel rejoué.
- Durée de maturation et pénalités calculées par horloge serveur.
- Aucune naissance négative, débordement numérique ou « population fantôme ».
- Un joueur ne perd pas des civils uniquement parce qu'il était hors ligne, sans événement autorisé clairement visible.
- Indiquer les effectifs disponibles et engagés, les soins et le prochain lot ; historique des pertes/récupérations.

## À arbitrer

Unité de population (abeille ou lot), nombre de rôles à l'Alpha, limites de population, coût/temps de maturation, mortalité durable ou seulement blessures, traitement des civils si capacité réduite, partage des infrastructures de la Nurserie avec la filière militaire.

