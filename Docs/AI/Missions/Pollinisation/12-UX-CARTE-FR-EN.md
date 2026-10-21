# 12 — UX de la carte, fenêtres premium et localisation FR/EN

## Objectif confirmé

La carte actuelle est **visuellement satisfaisante** (paysage et tuiles) ; le problème est le manque d'activités et la lisibilité des objets, des zones et des possibilités d'action. **Ne pas refondre le terrain.**

## Ajouts de carte recommandés

- Points d'intérêt floraux de tailles et rangs différents, avec icônes premium clairement différenciées.
- Relais/balises d'alliance sur la carte, marqueurs de propriété et de pacte.
- Essaims civils en mouvement, escortes militaires distinctes, routes de pollinisation lorsque l'utilisateur les sélectionne.
- États de site : disponible, floraison, appauvri, en restauration, contesté, événement actif, accès réservé.
- Indicateurs de danger, contribution, capacité et durée ; marqueurs **sans scintillement** lors des rafraîchissements réseau.
- Filtre « Pollinisation », « Ressources », « Alliances », « Menaces », « Événements » ; légende optionnelle en complément, pas substitut aux informations visibles.

## Hiérarchie selon le zoom

- **Zoom éloigné** : régions fertiles, grandes floraisons, projets actifs, tanière/événements mondiaux, frontières d'alliance lisibles.
- **Zoom moyen** : champs, relais, routes utiles, objectifs collectifs et dangers.
- **Zoom rapproché** : icônes/essaims individuels, états et feedback d'interaction.
- Les informations complexes restent dans les fiches, jamais toutes affichées simultanément sur le terrain.

## Fiche de site floral — données nécessaires

Nom localisé, type et niveau du site, région, contrôle/droits, fertilité, biodiversité, réserves, famille florale, fenêtre de floraison, capacité, progression collective, nombres de participants actifs/qualifiés, récompenses estimées, durée, distance, risque et expéditions en cours.

Actions contextuelles : **Préparer expédition**, **Rejoindre projet**, **Afficher les routes**, **Voir pacte/contrôle**, **Rapport précédent**. Les boutons interdits affichent **la raison** et la condition à atteindre.

## Fenêtre d'expédition

Choix essaim civil N, formation, championne facultative, rôle, destination et mode ; escorte militaire dans bloc séparé. Afficher coûts, gain probable, impact écologique, réserve de population, temps serveur et risque AVANT validation.

Validation finale = action authentifiée serveur ; rafraîchir le modèle officiel à la réponse. Le client n'applique pas de crédits simulés.

## Fenêtre Alliance / projets floraux

Même vocabulaire et qualité premium que **Activités v4**. Sous-onglets possibles : Aperçu, Projets, Territoires, Relais, Pactes, Contributions, Classements. Intégrer prudemment avec l'interface Alliance existante (navigation plein écran, scroll fonctionnel, profil et chat préservés).

Le chef/dirigeant habilité peut ouvrir un projet ; un membre voit tâches utiles et sa propre contribution. Un non-membre peut voir les projets publics autorisés, sans accès aux commandes réservées.

## Communication des effets

Après chaque mission : « Nectar/miel récolté », « Pollen transporté », « Fertilité gagnée », « Contribution au projet », « Pertes/soins », « Civils libérés », « Prochain palier ». Les valeurs viennent du reçu serveur.

Pas de « 100 % pollinisé » qui cache l'épuisement des réserves : les jauges ont des noms distincts et une info-bulle. Présenter les bonus de progression explicables.

## Localisation et robustesse

- Toutes les nouvelles chaînes en **fr-CA et en-US** ; terminologie cohérente : Pollinisation / Pollination ; Butineuses / Foragers ; Essaim civil / Civilian swarm ; Relais floral / Floral outpost ; Floraison / Bloom ; Fertilité / Fertility ; Pacte de floraison / Bloom pact.
- Chaînes stockées en UTF-8 ; préserver **é, è, à, œ, apostrophes**, pas de caractères de remplacement.
- Changement à chaud de langue : titre, liste, boutons et messages sans perdre l'état ni l'ascenseur.
- Fenêtres plein écran : pas de clic à travers, couches UI correctes, scrollbar visible, pas de double scroll, compatibilité petits écrans.
- Mise à jour réseau : ne pas redessiner les boutons si leur état n'a pas changé, conserver focus/selection et positions.
- Navigation clavier/souris/écran tactile ; filtres persistants tant que carte ouverte.
- Badges d'état + formes et textes, ne pas baser la signification uniquement sur une couleur (daltonisme).

## Alpha : écrans minimum

Carte avec 2-3 types de sites, fiche de site, préparation/lancement, suivi de mission, rapport, projet collectif dans Alliance, mode FR/EN. Pas de refonte de l'interface générale ni des scènes.

