# P15 - Integration pollinisation et restauration Git reproductible

Date : 23 octobre 2026.
Branche : integration/pollinisation-main-snapshot-20261023.
Base : main afc9c31e. Publication GitHub en attente de commit.

## Contenu exact de la livraison
- 36 fichiers Unity du client P12, y compris le panneau royal, les ancrages floraux, les images et les paires .meta.
- 47 fichiers serveur de pollinisation et de compatibilite avec l'Academie.
- 72 fichiers de conception, de tests et de suivi de la pollinisation.
- 6 fichiers Unity (.cs et .meta) necessaires au controleur de l'Academie deja present dans les modifications locales.
- Correctif du workflow deploy.yml : ne jamais ecraser appsettings.Production.json propre au serveur SRVESDT lors d'une future promotion.
- Les nombreux changements locaux non lies a la pollinisation dans le main PCJEFF ne font PAS partie de cette branche.

## Recettes reellement executees
- Serveur Release de la branche reconstructible : compilation PASS / 0 erreur.
- Tests serveur metier de la copie complete : 328/328 PASS.
- Tests API complets de main : 722/723 puis reprise isolee du cas echec 1/1 PASS. Echec original : acces au fichier JSON refuse dans une dotation de la fondation ; reprise de suite complete en cours pour confirmer l'absence de regression.
- Unity sur worktree reconstructible : Pollination EditMode 11/11 PASS.
- Unity WorldMap : 7/8 lors du premier essai (TaskCanceledException du module MCP tiers), puis 8/8 PASS sans changement de code.
- Integrite des couples asset/.meta de ce worktree : 7334/7334 PASS. Les ressources volumineuses ignorees par Git sont restaurees des copies de reference sur PCJEFF, pas publiees par cette branche.
- Unity officiel main : les 11 tests distincts de pollinisation ont passe en plusieurs campagnes (P5, panneau, rendu et P8), sans GameView authentifiee avec compte serveur.

## Production reelle SRVESDT
- SQL Server : migrations 096-098 et six tables florales verifiees sous SRVESDT\rdp_jeff.
- Sauvegarde pre-migration BeeKingdom-Pollination-PreMigration-20261023-153619.bak validee RESTORE VERIFYONLY.
- API /health HTTP 200.
- FloralSites.Enabled = true : lecture de catalogue protegee par authentification, sans token HTTP 401.
- CivilPopulation.Enabled = false, CivilExpeditions.Enabled = false, FloralAllianceProjects.Enabled = false.
- Catalogue temporaire de deux sites ancre sur la surface Wave6 et defaultWorldId officiel, valeurs d'equilibrage encore provisoires.
- Sauvegarde configuration precedente : C:\BeeKingdom\deploy-backups\FloralReadPilot-20261023-205639.
- Aucun butinage, achat de civils, depense de miel/pollen ou gain d'alliance live valide.

## Criteres de passage encore ouverts
1. Faire un test contre SRVESDT avec un compte valide et verifier la liste JSON des deux sites (retour 200).
2. Faire un parcours GameView authentifie avec les fenetres Champs / Essaims / Alliance en francais et anglais sur la carte.
3. Valider les regles finales de production des civils, l'economie, le stock et la progression d'alliance.
4. Ouvrir les flags mutateurs seulement apres une nouvelle sauvegarde SQL et tests reels de deux joueurs distincts.
5. Ne pas promouvoir la branche deploy ni fusionner la PR sans controle de regressions et reconciliation des autres travaux locaux.