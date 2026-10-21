# 02 — Boucles de jeu : de la ruche à la prospérité régionale

## Objectif de design

**[APPROUVÉ]** La carte doit offrir de nombreuses choses à faire ; la pollinisation doit être un axe fort de l'économie et de la vie des alliances, pas un mini-jeu isolé. L'activité solitaire peut préparer le terrain ; la transformation régionale exige un groupe.

## Boucle individuelle (une session courte)

1. Ouvrir la carte et activer éventuellement le filtre **Pollinisation**.
2. Repérer un bosquet, un champ ou un projet d'alliance : état, réserve, fertilité, distance, danger, disponibilité, récompense.
3. Choisir l'objectif : gagner des ressources, entretenir le champ, aider son alliance, découvrir une fleur rare.
4. Affecter un **essaim civil disponible**, éventuellement une championne et une escorte militaire séparée.
5. Choisir un mode de mission : **intensive**, **équilibrée**, **restauration** (recommandations).
6. Confirmer coût, temps serveur, rendement estimé, impact écologique, risque et itinéraire.
7. Les unités se déplacent sur la carte ; le serveur réserve l'effectif, synchronise les autres joueurs et résout le résultat même application fermée.
8. Recevoir un rapport : pollen transporté, nectar/miel et pollen gagnés selon catalogues, variation des indicateurs, contribution d'alliance, éventuels blessés.
9. Réaffecter ses abeilles ou les laisser se reposer/récupérer ; voir les opportunités suivantes.

## Boucle collective d'alliance

1. Le responsable habilité sélectionne un **projet floral** associé à un ensemble de sites.
2. Le projet indique : objectif de contribution, **nombre de participants distincts** requis, route(s) nécessaires, fenêtre d'activité et dépenses collectives.
3. Les membres contribuent selon leurs capacités. Le niveau de l'alliance augmente la taille des projets et leur coordination, sans transformer un joueur en armée de participants.
4. Les seuils successifs débloquent **stades de floraison** et avantages temporels, soumis à l'entretien écologique.
5. Les contributions sont tracées dans un journal lisible ; les récompenses de participation et de performance sont distinctes.
6. Si l'objectif dépasse les capacités d'une alliance, le jeu met en avant recrutement, progression ou **pacte avec une autre alliance**.
7. Les régions productives attirent des compétiteurs, ce qui provoque diplomatie, défense, commerce, pactes et parfois combat.

## Boucle longue du royaume

- Jour : butinage, expéditions, retour des abeilles, soins et contribution.
- Semaine : amélioration d'un secteur, construction d'un relais, objectifs d'alliance, progression des championnes.
- Saison/cycle : grandes floraisons, sites rares, impacts des conditions naturelles, modifications des routes économiques.
- Événement mondial : coopération de multiples alliances avec classements, notamment la tanière de l'ours ; compétition encadrée.

## Scénarios de jeu exemplaires

**Joueur solo débutant :** envoie 1 petite unité civile sur un bosquet public et reçoit des ressources. Son impact régional reste faible, mais visible ; aucun mur d'alliance obligatoire au début.

**Joueur civil avancé :** dispose de butineuses formées, une championne florale et des technologies, contribue davantage et ouvre des routes plus éloignées.

**Alliance active niveau intermédiaire :** 15 joueurs réalisent le second stade d'une prairie, alors qu'un joueur seul ne peut dépasser un plafond de contribution sur un cycle.

**Coalition :** une grande floraison nécessite deux familles florales et plusieurs relais ; une alliance détient les butineuses, l'autre les escortes, une troisième les accès.

**Conflit :** une alliance rivale capture un relais, mais la fertilité régionale est conservée et les droits économiques changent.

## Décisions de friction produit

- Missions planifiables (option recommandée) plutôt que micro-clics ; notification de fin, historique d'actions.
- Au moins une action civile utile accessible même aux joueurs sans alliance.
- Le temps de connexion ne doit pas être une condition d'achèvement ; serveur UTC officiel.
- La durée d'une mission doit créer un choix, pas une attente artificielle de dix minutes.
- Les missions disponibles et inaccessibles doivent afficher des raisons compréhensibles (essaim réservé, site épuisé, pacte manquant, danger, niveau insuffisant).
- Prévoir un mini tutoriel de pollinisation aligné avec notre tutoriel actuel ; ne pas imposer le PvP pour la première boucle.

## Critères d'acceptation

- Une mission complète existe : préparer, lancer, voyager, travailler, revenir, créditer, journaliser.
- Deux joueurs indépendants contribuent au même projet sans écraser leurs actions mutuelles.
- Un joueur ferme Unity et retrouve l'état correct côté serveur.
- Solo utile ; projet régional impossible par un seul compte, même puissant.
- Impact économique et écologique explicitement visible dans un rapport.

