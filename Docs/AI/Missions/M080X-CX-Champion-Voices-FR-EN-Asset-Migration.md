# M080X-CX — Champion Voices FR/EN

## Masters et copies
Source : `C:\projets\beekingdom\BIBLE\10_Championnes`. Les exceptions Zephyra `Voix\*.mp3` et Aurelia `fr\*.mp3` sont affectées au français conformément aux précisions du CEO ; aucun original déplacé ou modifié.

| Championne | BIBLE FR copiés | BIBLE EN copiés | Assets FR final | Assets EN final |
| --- | ---: | ---: | ---: | ---: |
| Striga | 22 | 17 | 23 | 17 |
| Zephyra | 19 (5 + 14) | 0 | 20 | 0 |
| Ambra | 19 | 14 | 19 | 14 |
| Nectaria | 22 | 0 | 22 | 0 |
| Aurelia | 21 | 0 | 21 | 0 |

134 masters copiés sans conversion ; 136 MP3 runtime au total. Les langues absentes restent vides, sans faux audio.

## Anciennes voix préservées
Les 44 anciens MP3 et leurs `.meta` sont conservés à l'identique. Seuls ces deux clips sont absents de BIBLE ; ils ont été déplacés de `ftue` vers `fr` sous `Assets/BeeKingdom/Playground/Resources/PremiumBeeReference/ChampionVoices/` :

- `striga/fr/striga_ftue.intro.barrack_intro.mp3`
- `zephyra/fr/zephyra_ftue.intro.welcome.mp3`

## Structure et suppressions
Structure finale : `ChampionVoices/<champion>/fr|en/<voiceKey>.mp3`, dix dossiers de langue à plat. Suppression des 14 anciens dossiers et de leurs `.meta` : Ambra `cit, move, select, spawn` ; Striga et Zephyra `cit, ftue, move, select, spawn`. Les dossiers vides sont conservés dans Git par `.gitkeep`.

## Code
`ChampionVoiceBarkController.cs` résout la langue via `BeeLocalization.CurrentLocale`, filtre les catégories par nom et sépare les caches FR/EN. `TutorialDialoguePresenter.cs` charge les FTUE à plat avec un cache localisé ; seules les deux lignes de diagnostic select/spawn ont changé dans `HiveViewProductUiPresenter.cs`. Une langue sans clip reste silencieuse.

## Validation
Unity 6000.5.3f1 ouvert : recompilation terminée, `scriptCompilationFailed=False`, aucune erreur console. Import de 136/136 AudioClip vérifié ; changement FR → EN → FR, cache select, deux FTUE et Aurelia `collect3` validés. Aucun Full Test Runner.

SHA-256 : 134 masters intacts et copies identiques ; 44 anciennes voix et métadonnées intactes. Structure, absence de `.meta` orphelins et diff C# validés ; l'ajout OC `PeekOwnedChampionBeeIdsForWorldMap` est strictement préservé et exclu du commit, ainsi que le fichier World Map.

## Commit et points non résolus
Commit local d'implémentation : `b763f3531d19d354afb076d7023f8b92b27abd5a`. Aucun push. Points non résolus : None ; retest visuel/écoute réservé au CEO.

READY FOR CEO CHAMPION VOICES STRUCTURE RETEST
