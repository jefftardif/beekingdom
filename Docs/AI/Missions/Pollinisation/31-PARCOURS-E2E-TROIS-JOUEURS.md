# 31 — Scénarios de référence : un joueur, une alliance, une coalition

Les nombres de cet exemple sont **illustratifs** et ne doivent pas être livrés comme équilibrage approuvé.

## Scénario 1 : novice sans alliance

État initial : joueur A a une ruche avec Nurserie opérationnelle, 120 butineuses civiles disponibles, aucun soldat disponible, 200 miel.

1. Depuis la Nurserie, il voit 120 civils en réserve et une cohorte prochaine.
2. Il ouvre la carte du monde et clique sur « Bosquet des Trèfles » public.
3. La fiche indique fertilité 65 %, 300 nectar disponibles et aucune menace.
4. Il prépare une mission équilibrée avec 80 butineuses, coût 10 miel et ETA 12 minutes.
5. Le serveur vérifie et réserve les 80 ; stock disponible passe à 40, mission « outbound ».
6. Le joueur quitte Unity ; 12 minutes passent sur l'horloge serveur.
7. L'expédition consomme au plus le nectar/pollen réellement disponible et produit un rapport.
8. Les 80 reviennent ou passent temporairement en récupération ; honey/pollen wallet crédités une seule fois.
9. La fertilité progresse légèrement ; le projet **régional** n'est pas accompli par ce seul joueur.
10. Il peut recommencer plus tard ou améliorer sa Nurserie.

**Conditions** : zéro double gain, zéro armée obligatoire, horaires corrects sans client ouvert.

## Scénario 2 : deux joueurs d'alliance, niveau différent

État initial : alliance B a accès à une petite prairie, joueurs A et C sont membres ; C a Académie plus avancée et affinité génétique trèfle.

1. L'officier ouvre un projet floral avec une exigence de points **et au moins 2 participants qualifiés**.
2. A contribue 100 points utiles ; C avec même effectif contribue 140 points en raison de ses compétences.
3. Les deux expéditions ont leurs propres reçus, récompenses, réservations et effets sur un **site partagé**.
4. Même si C effectue 50 missions, il ne peut remplacer l'identité d'A pour le seuil distinct.
5. Un jalon débloque un bonus temporaire de floraison, visible à A et C.
6. Un bug réseau rejoue la requête de C ; le serveur renvoie la même fin sans doubler le score.
7. C quitte ensuite l'alliance : sa contribution passée reste attribuée selon la politique au moment de la livraison, pas transférée au nouveau groupe.

**Conditions** : progression joueur effective, identités comptées une fois, ledger partagé.

## Scénario 3 : alliance forte mais inactive vs petite alliance active

Alliances D (100 membres, 5 actifs) et E (30 membres, 25 actifs).

1. D a éventuellement un niveau plus élevé/plus de relais, mais ne reçoit aucun score automatique de ses 95 membres inactifs.
2. E mobilise un plus grand nombre de joueurs, de routes et de projets.
3. E peut atteindre une floraison que D n'atteint pas si D n'active pas suffisamment de membres, malgré l'avantage en capacité.
4. Le niveau d'alliance ouvre un projet ; **seule la contribution qualifiée le réalise**.
5. D peut réagir par recrutement, pacte, spécialisation ou intervention militaire autorisée.

**Conditions** : équilibre anti-monopole et coopération récompensée.

## Scénario 4 : Pacte de Floraison et guerre de relais

Nectarines N (économie), Gardiens G (militaire), Ombre O (adversaire).

1. N identifie deux champs complémentaires, l'un situé près d'un Relais floral contrôlé par G.
2. N/G signent un pacte précisant accès aux routes, escorte de G, durée et partage du score.
3. N envoie des civils ; G réserve des soldats officiels pour escorter, séparés du pool civil.
4. O attaque le relais dans une fenêtre de vulnérabilité annoncée.
5. Si O gagne, **seul le contrôle/logistique** change ; la fertilité ne se téléporte pas et n'est pas détruite.
6. Les missions N/G engagées avant le changement suivent une politique publiée de retrait ou de poursuite ; aucun civil disparu.
7. Les contributions déjà réglées sont figées au ledger ; aucune nouvelle route privée sans droit.
8. La guerre est motivée par l'économie florale et ses infrastructures, pas une cible décorative.

**Conditions** : permissions serveur et protection des civils de débutant.

## Scénario 5 : Grande floraison et tanière de l'ours

1. Plusieurs alliances pollinisent une région et une Grande floraison temporaire s'annonce.
2. Les alliances organisent routes, protection et logistique pour profiter des ressources.
3. À un événement séparé, l'ours sort de sa tanière. Des alliances coopèrent contre lui en raid mondial, avec objectifs militaires et logistiques.
4. Le raid doit produire récompenses de participation et classement compétitif séparés ; une coalition ne monopolise pas automatiquement le podium.
5. La carte revient à un état normal à la fin de l'événement ; la tanière demeure un repère permanent.

**Conditions** : intention fondatrice de l'ours respectée, événement pas simulé par un « loot NPC » solo.

## Données et révisions à capturer sur tous les scénarios

- snapshots avant/après (hive, civil, site, projet, alliance), revisions et timeUTC ;
- reçus pour start/delivery/claim, source d'autorisation, bonus calculés ;
- diff des ressources, du nombre civil disponible/réservé, de la fertilité ;
- preuve FR/EN ; captures de carte et de l'interface ;
- logs de l'opération et métriques sans fuite de données privées ;
- résultat attendu/pass/fail avec explication du décalage.

## Tests à scénarios paramétrés

Rejouer chaque scénario avec un délai réseau, un double clic, un refus 409, un retour de serveur et un changement d'identité. Les résultats économiques doivent converger vers le **même état final**.

