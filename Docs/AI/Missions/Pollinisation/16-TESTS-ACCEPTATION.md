# 16 — Plan de tests, sécurité et acceptation fonctionnelle

Statut : cas de tests proposés pour les futurs lots. **Aucun de ces tests n'est déclaré passé** par la seule présence de ce fichier.

## Identité et sécurité

- QA-001 Un compte A ne lit/mute pas la population privée du compte B.
- QA-002 Une ruche appartenant à un autre joueur retourne refus propre.
- QA-003 Droits de chef/officier : seul rôle habilité ouvre projet, pose relais ou pacte.
- QA-004 Compte exclu d'une alliance ne conserve pas les droits de lancer une nouvelle mission privée.
- QA-005 Jeton expiré : client re-authentifie selon procédure existante, pas de double lancement.
- QA-006 Modification de récompense dans le JSON de la requête n'a aucun effet.

## Économie et populations

- QA-007 Démarrage de cohorte débite exactement une fois ; double clic renvoie le même reçu.
- QA-008 Même cohorte réclamée deux fois : aucun double ajout de civils.
- QA-009 Somme des civils par états = population conservée ; aucune unité négative.
- QA-010 Deux missions tentent de réserver le même essaim : une seule réussit.
- QA-011 Réserve civile n'interfère pas avec la réserve militaire ou collecte officielle.
- QA-012 Formation/soins/retour rendent les civils disponibles une seule fois.
- QA-013 La capacité de stockage et de cargaison est respectée sans inventer de ressources.
- QA-014 Un village/compte existant migre avec zéro civils gratuits et les soldes anciens conservés.
- QA-015 Arrêt / redémarrage du serveur pendant une maturation ne fait pas perdre la cohorte.

## Sites, écologie et missions

- QA-016 Deux joueurs prennent les dernières ressources du même site : somme récoltée <= réserve.
- QA-017 Fertilité et biodiversité restent bornées, réserves jamais négatives.
- QA-018 Mode intensif, équilibré et restauration ont effets différents documentés.
- QA-019 Compatibilité source/destination rejetée avant coût irréversible si invalide.
- QA-020 Mission ferme Unity puis se termine ; retour et reçu officiels retrouvés.
- QA-021 Deux requêtes fin/claim simultanées ne créditent qu'une fois.
- QA-022 Site réapprovisionné/régénéré n'excède pas sa capacité.
- QA-023 Route rendue inaccessible en transit applique le comportement défini (repli, remboursement éventuel).
- QA-024 Reconnexion sous une autre ruche ne révèle jamais l'état de l'ancienne.
- QA-025 Aucun client ne crée du pollen ou des points de projet en modifiant sa propre horloge.
- QA-026 Clés de sites stables et identiques entre Unity et serveur.

## Collectif / alliance / diplomatie

- QA-027 Un joueur seul, même max niveau, ne peut compléter le seuil distinct régional.
- QA-028 Deux joueurs réels augmentent le même compteur partagé sans écraser les mises à jour.
- QA-029 Joueur hors alliance peut travailler dans un site public.
- QA-030 Niveau d'alliance verrouille/déverrouille les projets corrects.
- QA-031 Niveau de joueur change de façon mesurable l'efficacité, dans des caps attendus.
- QA-032 Une mission ne crédite pas deux fois deux projets sauf règle explicite.
- QA-033 Changement d'alliance ne réécrit pas les contributions historiques.
- QA-034 Pacte expiré ou rompu bloque les nouvelles entrées sans corrompre les missions en vol.
- QA-035 Coalition de petites alliances peut réussir l'objectif multi-alliance.
- QA-036 Un projet terminé ne verse jamais deux fois la récompense à la même ruche.

## Conflits et événements

- QA-037 Escortes militaires et essaim civil ne doublent pas leurs pools.
- QA-038 Civils novices protégés contre attaques non autorisées.
- QA-039 Défaite d'escorte entraîne issue civile définie, sans essaim fantôme.
- QA-040 Capture de relais transfère contrôle sans remettre fertilité à zéro.
- QA-041 Événement mondial d'ours accepte plusieurs alliances, classe séparément et attribue les récompenses une seule fois.
- QA-042 Tanière reste permanente même événement terminé ; BearVisible/ActiveEvent reflètent l'état réel à terme.
- QA-043 Pucerons/grande floraison n'altèrent pas définitivement les ressources de base.

## UX, performance et régressions

- QA-044 Toutes chaînes FR-CA/EN-US localisées, accents corrects, aucun U+FFFD.
- QA-045 Pas de clignotement des boutons lors de refresh des données.
- QA-046 Scroll et fenêtre plein écran fonctionnent, pas de clic à travers.
- QA-047 Zoom, filtres et marqueurs affichent états justes sans toucher aux tuiles.
- QA-048 Deux sessions voient la même fertilité/progression avec délai acceptable.
- QA-049 Nombreuses missions concurrentes n'induisent ni double crédit ni saturation non maîtrisée.
- QA-050 Ancien système de collecte, patrouille, combat, NurseryCohorts, Académie, Recherche, Championnes et Alliances inchangés fonctionnellement.
- QA-051 Désactivation d'un flag interdit les nouvelles missions et permet un drainage cohérent des missions lancées.
- QA-052 API erreurs 401/403/404/409/503 et retries donnent feedback utile.
- QA-053 Migration et rollback ne détruisent aucune donnée hors périmètre.
- QA-054 Bilan d'économie montre absence de boucle d'enrichissement exponentielle via multi-alliances.
- QA-055 Un joueur inactif reste admissible aux protections/régénération de base sans sanction imprévue.

## Parcours E2E de recette Alpha (minimum)

1. Joueur A (novice) démarre une cohorte civile, se déconnecte, revient et la réclame.
2. A envoie essaim sur site public, voit temps et coût, ferme le jeu, revient, obtient rapport.
3. Joueurs A et B pollinisent le même projet : deux contributions distinctes, jauge serveur partagée.
4. Le projet requiert davantage de participants : A seul ne peut en simuler plusieurs.
5. Joueur C plus avancé apporte plus de valeur **mais reste soumis aux caps** ; alliance de niveau suffisant ouvre palier.
6. Soldes et population sont inchangés lors de double clic/retry/reconnexion.
7. Tests en FR/EN, smartphone paysage et résolutions habituelles.
8. Aucun build/publish sans preuve d'intégrité des systèmes existants.

## Recette Bêta supplémentaire

Conquête d'un relais, pacte, coalition, escorte, dissolution, bataille multi-joueurs, événement ours, floraison rare, protections anti-griefing et charge significative avec journaux/mesures.

## Collecte de preuves

Stocker logs de tests, états avant/après, capture d'UI, révisions, reçus idempotents et version serveur/client dans un dossier QA dédié. Ne jamais écrire « PASS » sans avoir exécuté le scénario correspondant.

