# Runtime Scenario Data Layer - UI P6 Counter-Review

Date locale: 2026-07-15
Role: UI-Relay read-only
Portee: contre-revue documentaire UI de P6. Aucun fichier Unity, PNG, terrain Wave5, BearDen, APK, serveur ou donnee reelle modifie.

## Sources lues

- Spec UI P6: `Docs/UIRelay/WorldMapScenarioLab_UI_Spec.md`
- Rapport P6: `Docs/WorldMapRuntimeEntitiesWave1/RuntimeScenarioDataLayer_Report.md`
- Recu P6: `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/RuntimeScenarioDataLayerProof/RuntimeScenarioDataLayerProofReceipt.md`

## Verdict synthetique

UI_P6_COUNTER_REVIEW=PASS_WITH_NOTES

P6 respecte l'intention principale de la spec UI: scenarios locaux configurables, deux ruches test editables, badge local/non officiel, presets, absence de serveur/gain officiel et preservation P1-P5. Les preuves disponibles sont suffisantes pour consommation Builder-A, avec une reserve mineure: elles valident surtout les gates de donnees/runtime et ne documentent pas encore une capture dediee des etats visuels HUD tablette/telephone.

## Conformite spec UI P6

- Signal `LOCAL - NON OFFICIEL`: PASS. Le rapport P6 indique badge visible et provider `local_demo`; le recu confirme `Server/remote: ABSENT` et `official_gain: false`.
- Presets `Collecte R3`, `Duel`, `Raid T7`: PASS. Le rapport liste `Collecte R3`, `Duel ruches`, `Raid T7`; gate `SCENARIO_PRESETS=PASS`.
- Edition des deux ruches test: PASS. Rapport et recu confirment `PLAYER_TEST_HIVE / ENEMY_TEST_HIVE editables`.
- Etats modifie/reset/erreur: PASS_WITH_NOTES. Les gates couvrent scenarios absents, IDs invalides, coordonnees invalides et reset legacy; le rapport ne fournit pas de preuve visuelle separee des etats HUD.
- Coexistence avec `LECTURE CARTE`, filtres, `Proche`, legende: PASS_WITH_NOTES. `Legacy P1-P5 regression: PASS` couvre les outils P2/P5; pas de capture specifique de collision panneau.
- BearDen: PASS. Le rapport declare BearDen preserve et aucune modification source.
- Absence de masque terrain: PASS_WITH_NOTES. Aucun terrain 50x50, PNG terrain ou master terrain modifie; la lisibilite HUD/tablette/telephone reste a confirmer par preuve visuelle dediee.

## Risques UI residuels

- Verifier en device/screenshot que `LAB LOCAL` ouvert ne masque pas le centre carte quand `LECTURE CARTE` est aussi present.
- Verifier que les libelles `Duel ruches` et `Duel` restent coherents dans le HUD final.
- Verifier que les etats `modifie`, `reset`, `erreur`, `disabled` restent visibles par symbole + couleur et pas uniquement par texte/couleur.
- Verifier en portrait qu'un seul panneau est pleinement ouvert et que la carte conserve au moins 55% de hauteur visible.

## Criteres d'acceptation UI pour cloture absolue

- Capture ou preuve Play Mode montrant les trois presets dans `LAB LOCAL` avec badge `LOCAL - NON OFFICIEL`.
- Capture ou preuve des deux ruches editables avec une valeur modifiee puis `Apply local` et `Reset local`.
- Capture ou preuve d'une erreur inline et d'un etat disabled/focus.
- Capture ou preuve de coexistence `LECTURE CARTE` + `LAB LOCAL` + BearDen sans collision ni masque terrain, en paysage et portrait.

## Conclusion

P6 est consommable par la suite UI/Builder-A. Les notes sont des demandes de preuve visuelle supplementaire, pas des blocages fonctionnels.
