# 30 — Parcours UX complets de la pollinisation

## Intention

**[APPROUVÉ]** La carte et les tuiles ne sont pas à refaire. La carte doit devenir **vivante et utile**, avec activité, marqueurs distincts, buts, visibilité des zones et interactions de coopération/conflits. Réutiliser le chrome premium Activités v4, sans introduire de fenêtres à clic traversant, boutons qui clignotent ou accents brisés.

## Écran A : carte principale — découverte

Éléments proposés :
- Filtre Pollinisation / Ressources / Alliances / Événements / Menaces / Tous.
- Marqueur de champ : espèce, taille et phase visibles à zoom moyen.
- Badge d'état : fleurit / épuisé / en restauration / besoin d'aide / sous menace / occupé.
- Ligne de périmètre d'alliance visible au niveau de zoom adapté, avec nom/tag du propriétaire ; pas nécessaire de retoucher les tuiles.
- Petit marqueur « Projet d'alliance » flottant sur site prioritaire, mais priorité d'affichage limitée.
- Au zoom faible : compteurs agrégés par région, grosse floraison et tanière de l'ours ; ne pas montrer 200 petits marqueurs superposés.
- Position d'essaims civils officiels et militaires séparés, animations optionnelles.

Interactions :
- clic/tap site -> fiche en lecture seule d'abord ; bouton préparer si autorisé ;
- double tap pas de double commande, seule confirmation engage ressources ;
- sélection d'une mission active -> statut/ETA/option rappeler ;
- filtre « Mon alliance » fait ressortir projet et relais, sans cacher tous les sites publics à portée.

## Écran B : fiche site floral

Structure recommandée :
1. En-tête premium : nom localisé, niveau du site, type public/allié/hostile, état et icône.
2. Indicateurs distincts : Fertilité (écologie future), Biodiversité (diversité), Réserves (récolte actuelle), Progression du projet (objectif de groupe).
3. Dangers : événements actifs, attaque possible, escorte conseillée, raison d'interdiction.
4. Sources/destinations compatibles et routes possibles (si activées en lot courant).
5. Projets en cours : progression, participants qualifiés/distincts, nombre nécessaire, alliance, temps restant.
6. Bouton principal « Préparer expédition » ; secondaires « Rejoindre projet », « Routes », « Informations ».

Chaque jauge a libellé + unité + info-bulle ; une couleur ne suffit pas.

## Écran C : préparation d'expédition

Sections :
- **Essaim civil** : civils disponibles, affectés, taille choisie, spécialisation, portée, rendement projeté.
- **Championne** : liste de championnes admissibles, restrictions d'exclusivité et bonus.
- **Mode** : intensif, équilibré, restauration, avec conséquence **qualitative et numérique validée**.
- **Route** : source/destination, famille florale, compatibilité, distance/ETA.
- **Protection** : escorte séparée, slots militaires/civils, danger et pertes possibles.
- **Coûts et revenus estimés** : débit miel/pollen/cire et cargaison probable, sans promettre un gain non garanti.
- **Confirmation** : action unique authentifiée, progress spinner stable, idempotency key conservée au retry.

Cas particuliers :
- Si service inactif : « Fonction en cours de préparation », pas bouton factice.
- Si révision conflict : réactualiser sans vider tout le formulaire si encore valide.
- Si source appauvrie : rendre le bouton indisponible avec raison et autre champ suggéré.
- Si compte change : fermer l'écran et effacer anciennes données privées.

## Écran D : expédition active

- ETA restant dérivé de serverTimeUtc et des timestamps officiels.
- Phase actuelle : En route / Récolte / Livraison du pollen / Retour / Prête / Complétée / Interrompue.
- Civils immobilisés, lieu courant, escorte, cargaison et risque.
- Lien vers site/projet, rappel si possible, historique.
- Aucune interpolation d'état créant gains avant le règlement serveur.

## Écran E : rapport et récompenses

- Date UTC transformée en heure locale du joueur.
- Civils revenus, civils blessés/récupération, escorte, cargaison perdue si applicable.
- Débit initial, ressources gagnées, soldes officiels résultants.
- Points écologiques + variation de fertilité/biodiversité, avec explication de l'impact différé.
- Contribution personnelle et pourcentage d'objectif, nombre total de contributeurs qualifiés.
- Liens « Nouvelle expédition » / « Voir mon projet d'alliance ».

Les récompenses doivent pouvoir être consultées de nouveau après fermeture d'UI.

## Écran F : Nurserie / population civile

Sections :
- bandeau « Population civile », différent du roster militaire ;
- adultes disponibles, en mission, en formation/récupération ;
- cohorte courante avec durée serveur ; capacité et coût de la prochaine ;
- notifications et explications de blocage ;
- aucun changement aux commandes originales Nourrir/Stabiliser.

## Écran G : projets Alliance Activités v4

Sous-onglet/section « Pollinisation » proposé :
- Projets actifs avec jauges points + participants distincts + routes.
- Objectifs prioritaires définis par chef/officier habilité.
- Missions suggérées : sites prioritaires à portée du joueur.
- Contributions par membre, récompenses communes, fin de cycle et historique.
- Niveau d'alliance si officiel et effets d'organisation réellement actifs.
- Pactes, relais et classement masqués/désactivés si leur lot n'est pas activé.

## Écran H : territoire / relais / diplomatie (Bêta)

- Contrôle d'installation, propriétaire, relations diplomatiques, accès civil, protection militaire.
- Autorisations de construction/attaque/pacte selon rôle.
- Afficher nettement « fertilité du secteur » séparément de « contrôle du relais ».
- Détails de coalition et clauses du pacte accessibles aux membres autorisés.

## Comportement de mise à jour

Utiliser une machine de présentation : Loading / Ready / StaleReadOnly / Mutating / Error / Forbidden. Mise à jour ciblée des libellés, sans reconstruction systématique de toute la fenêtre. Pendant POST en cours, même bouton bloqué et clé de retry stable.

Ne pas polluer la scène par une deuxième scrollbar dans une fenêtre plein écran ; maintenir le titre hors des boutons retour, la couche bloque les clics dessous ; composants tactiles de taille raisonnable.

## Critères QA visuels

FR et EN ; accents, Unicode ; changement de langue en plein écran ; toutes tailles téléphone/PC ; scroll, overlay, navigation retour, retour d'auth, zoom/filtres ; cas zéro ressource, projet saturé, alliance absente, deux appels simultanés, perte de connexion.

