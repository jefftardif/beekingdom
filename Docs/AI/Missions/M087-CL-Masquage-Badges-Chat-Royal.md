# M087-CL — Masquage temporaire des badges dans Chat Royal

Sur demande du CEO, pour évaluer visuellement la fenêtre sans distraction :

- Le badge "● SERVEUR / ○ DEMO" est masqué dans Chat Royal (appel retiré,
  fonction conservée intacte, facile à rebrancher).
- Le filigrane de version/build (bas d'écran) est masqué uniquement pendant
  que Chat Royal est ouvert - il reste actif partout ailleurs, seule raison
  d'être de ce fichier étant la traçabilité des rapports de bug externes.

Les deux sont réversibles en une ligne si besoin de les revoir plus tard.

- Fichiers : `HiveViewProductUiPresenter.cs`, `BuildStampOverlay.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
