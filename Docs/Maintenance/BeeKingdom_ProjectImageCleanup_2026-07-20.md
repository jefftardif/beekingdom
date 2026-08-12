# Nettoyage des images et rendus de carte - 2026-07-20

## Resultat

Environ **50,9 Gio** de fichiers ont ete supprimes. Le nettoyage a ete limite aux
anciennes generations de carte, aux apercus de validation devenus inutiles et aux
caches produits par ces validations.

La carte 50x50 actuelle, la ruche, les icones et les autres graphismes actifs n'ont
pas ete modifies.

## Elements proteges

- Paquet de carte actif:
  `Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview`
- Paquet canonique de base:
  `Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v1`
- Superpanneau source final 12288 px:
  `Artifacts/UIB_ImmenseContinuousMaster50x50_wave5method_restart_staging/scaleup_superpanel_12288x12288/wave5method_scaleup_superpanel_fused_12288x12288.png`
- Ruche actuelle:
  `Assets/BeeKingdom/Playground/Resources/PremiumBeeReference/background_hive.png`
- Bibliotheques `PremiumBeeIcons`, `UI031Icons`, `PremiumBeeWorldMap` et
  `PremiumBeeReference`
- Scene actuelle `WorldMapWave6Wave5Method12288Preview` et scene `LivingHive`

## Controles d'integrite

Les empreintes SHA-256 apres nettoyage correspondent aux empreintes prises avant
suppression:

| Element | SHA-256 |
| --- | --- |
| Manifeste carte active | `880B30C432D44803BA118C29ADAE0B0A6F0093D1E64A2707FC46D5395B3F230D` |
| Tuile active R00C00 | `13934166A4AB5ED1565BBE51602F23880553FE7E5F193ED038E3B700809C3746` |
| Tuile active R24C24 | `57C5CA27EC9ED59C6735A8A7DD4D3707E30D0429AF606B467C80A914E08270C5` |
| Tuile active R49C49 | `24AF61ABF9EE7EA940218CEB0759A3B2DC84900C50004244337ADFD064706E04` |
| Manifeste base v1 | `1CBA34B261E02461726ED573DA991C10ABC8D7D871EBB00D7A8B737F8A0E18AB` |
| Superpanneau final | `3CE816052FFF97BCDE78251FA930C4D725DC622120D3644C806A9C1BE1330697` |
| Image de ruche | `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6` |

Le paquet actif contient toujours **2500 PNG**, **2500 fichiers meta** et ses quatre
fichiers de manifeste/validation.

## Familles supprimees

- Anciennes generations 5x5, 15x15 et 25x25.
- Iterations premium 50x50 v2, v3, v4 et toutes les passes intermediaires de phase 2.
- Production complete v4 devenue obsolete.
- Preuves et apercus `route_lock`, `support_center`, `v2i`, `v2o`, `v3d`, `v3e`,
  `v3m` et `v3o`.
- Sauvegardes horodatees du paquet Wave5 Method 12288.
- Anciens paquets d'execution Wave3 et Wave5.
- Intermediaires 4096/8192, preuves et copie source du superpanneau actuel; le rendu
  final 12288, son manifeste et ses recus ont ete conserves.
- Copie generee `Artifacts/UnityValidationProject`, dont 2,087 Gio de cache Unity.
- Sorties temporaires `Temp/bin` et `Temp/obj` creees pendant la verification.

## Configuration et validation

Les anciennes scenes d'audit dont les images ont ete supprimees ont ete retirees de
`ProjectSettings/EditorBuildSettings.asset`. Les scripts et scenes historiques sont
conserves comme outils de regeneration, mais ne sont plus livres dans la build.

- Compilation `Assembly-CSharp.csproj`: reussie, 0 erreur.
- Compilation `Assembly-CSharp-Editor.csproj`: reussie, 0 erreur.
- Les avertissements observes sont preexistants (API obsoletes et champs serialises).
- Aucun lancement visuel Unity n'a ete effectue pendant le nettoyage; Unity etait
  ferme afin d'eviter toute modification ou verrouillage de ressources.

## Elements volontairement conserves

- Le cache `Library` du projet principal, pour ne pas imposer une reimportation
  complete au prochain lancement de Unity.
- Les preuves visuelles et captures dans `Docs`, utiles pour l'historique du projet.
- Les graphismes de carte, de ruche, de tutoriel, d'interface et les icones actuels.
