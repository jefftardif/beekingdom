# 29 — Niveau d'alliance : proposition de système concret

## Décision imposée par le gameplay

**[APPROUVÉ]** Le niveau de l'alliance ET le niveau de ses joueurs ont des effets réels sur la pollinisation. Il faut distinguer **l'activité des membres** et la simple taille de roster.

### Constat vérifié

Le record AllianceEntity du dépôt inspecté a un champ MemberCount et MaxMembers, ainsi qu'une Revision, mais **pas de Level ou XP d'alliance explicite**. Le système possède des recherches d'alliance et leurs bonus ; ces recherches ne sont pas synonymes de niveau d'alliance.

L'agent doit traiter « AllianceProgression » comme **fonction nouvelle à définir**, à moins de découvrir un autre système officiel de niveau non aperçu à l'audit.

## Modèle recommandé

**AllianceFloralProgression**
- allianceId, progressionRevision ;
- level int >=1, experience int64 >=0 ;
- completedProjectsCount, activeMemberContributionsByPeriod, lastSettledAtUtc ;
- maximumActiveFloralProjects, maximumRelays, routeCoordinationTier ;
- activeFloralResearchBonuses et permissions (via recherche d'alliance officielle si approprié) ;
- history/receipts auditables pour récompenses et rétrogradations.

**Ne pas créer un niveau en convertissant mécaniquement le nombre de membres.** Niveau gagné par des actions authentiques et contributions vérifiables, idéalement plusieurs domaines à terme (alliance globale vs réputation florale à arbitrer).

## Deux modèles possibles à arbitrer

**Option A : niveau d'alliance général (recommandation long terme).** Les dons, objectifs d'alliance, recherches, assistance, participation aux événements et pollinisation génèrent de l'expérience selon une politique anti-abus. La pollinisation s'appuie sur ce niveau commun.

**Option B : maîtrise florale d'alliance séparée.** Niveaux floraux liés aux grands projets ; plus riche en spécialisation, mais risque de double niveau/UI complexe.

**Par défaut pour une Alpha légère :** un niveau général durable si aucun autre n'existe, avec progression limitée aux objectifs collectifs validés, puis extensions pour intégrer autres activités. Inscrire une ADR avant code.

## Effets du niveau proposés

- Débloquer taille/niveau maximal du projet et nombre de projets actifs.
- Débloquer les relais floraux (ultérieurement).
- Augmenter la portée de la **coordination collective**, sans tricher sur la distance personnelle du joueur.
- Augmenter la capacité de conclure des pactes/coordonner plusieurs sites.
- Ajouter éventuellement un bonus limité d'efficacité (catalogue versionné).
- Permettre projets écologiques de restauration plus complexes et récompenses d'alliance variées.

## Gains d'XP : prévenir le farming

Gagner de l'XP d'alliance à partir :
1. d'objectifs de projet floraux **réellement atteints** avec contributions distinctes ;
2. d'initiatives d'entretien où plusieurs membres apportent de la valeur ;
3. d'événements mondiaux ;
4. de réussites durables, pas une somme infinie de lancements de mission.

Règle anti-exploit : limiter XP par même site/projet/période, aucune XP pour annulation, insuffisance de participation ou spam de missions de 1 abeille. Ne pas punir les contributions légitimes avec des restrictions invisibles.

## Éviter le monopole

- Les niveaux débloquent la capacité mais les objectifs requièrent des participants distincts.
- Les projets immenses demandent parfois plus d'une alliance, même si le leader est max niveau.
- L'entretien des relais augmente avec expansion.
- Les petites alliances ont accès à des projets proportionnels à leurs moyens et à des pactes.
- Pas de revenu passif illimité parce qu'une alliance a atteint un certain rang.

## Niveau d'alliance vs niveau des joueurs

**Niveau d'alliance :** types de projets, slots et logistique de groupe.  
**Niveau individuel :** puissance/capacité/qualité de son essaim, recherche et adaptation génétique.  
**Coordination réelle :** nombre de contributeurs distincts, routes complémentaires et durée.

Formule qualitative : succès régional = capacité ouverte par alliance + somme des efforts individuels bornés + diversité des joueurs/rôles + conditions du monde.

## Propriété/adhésion

- Une contribution d'un membre reste attribuée à l'alliance **au moment de l'action**, pas transférée quand il la quitte.
- Un membre exclu ne peut pas ouvrir un nouveau projet ni emprunter un relais privé après expiration des droits.
- Une alliance dissoute garde l'historique de ses succès ; ses projets actifs suivent une règle de clôture, sans disparition d'argent ou de civils.
- Une alliance avec membres inactifs ne devient pas compétitive via un roster dormant.

## Fonction initiale à intégrer à Alliance Activités v4

Un onglet/section **Pollinisation** affichant : niveau/progression d'alliance (si implémenté), 1 projet prioritaire, contribution actuelle, nombre de participants qualifiés, prochaine étape, liste des missions utiles, rapport des récompenses et statut des partenaires autorisés.

Le niveau et le progrès ne doivent jamais être affichés comme « réels » avant que le serveur puisse en fournir l'état.

## Tests d'acceptation

- A 100 comptes inactifs niveau de roster élevé, B 30 contributeurs actifs -> B peut progresser plus vite à activité/compétence comparables.
- 1 compte max niveau ne franchit pas seuil de membres distincts.
- 8 membres + alliance partenaire réalisent un objectif interalliance si pacte valide.
- Une contribution rejouée n'ajoute jamais deux fois expérience ou points.
- Leadership transféré ne perd pas l'XP d'alliance.
- Révocation d'un compte ne supprime pas les progrès historiques.

