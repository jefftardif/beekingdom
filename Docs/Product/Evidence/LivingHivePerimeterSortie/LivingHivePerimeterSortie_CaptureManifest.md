# LivingHive Perimeter Sortie - manifeste de captures

- Scene: `Assets/Scenes/LivingHive.unity`
- Etats `not_configured`: controleur de production indisponible par defaut, aucun signal ni statut invente
- Etats `active_qa`, `ready_qa` et `debrief_*_qa`: donnees synthetiques de mise en page, marquees `APERCU QA`, controleur sans effet et aucun appel serveur
- Etat `offline_qa`: dernier GET synthetique marque `APERCU QA`, consultation seulement, toutes les actions serveur neutralisees
- Debriefs QA: recus synthetiques destines uniquement a prouver le rendu plein/partiel; aucun credit local
- Appareil: rendu, langue, navigation, selection et compte a rebours relatif seulement
- Serveur: session, cycle, revision, reservation, heure, recompense et credit officiels
- Mutation locale de ressource, reservation, sortie ou recompense: `false`
- Terrain 50x50, image de ruche et scenes modifies: `false`

- `LivingHive_PerimeterSortie_NotConfigured_FR_390x844.png`: `390x844`, locale `fr-CA`, etat `not_configured`, SHA-256 `835fb20c9e867f310c7fdecfaba23be2b4c399d4757958b768b60f44a556aac6`
- `LivingHive_PerimeterSortie_ActiveQA_FR_390x844.png`: `390x844`, locale `fr-CA`, etat `active_qa`, SHA-256 `9d819534e36d076f908aedd406c2656969b89f4c2226e3c4ebe341688eb75b69`
- `LivingHive_PerimeterSortie_OfflineReadOnlyQA_FR_390x844.png`: `390x844`, locale `fr-CA`, etat `offline_qa`, SHA-256 `710685fcd0632db8f5ac05a4807e8f6cc30c4a801911af7dcb1ef4753b73f41b`
- `LivingHive_PerimeterSortie_DebriefPartialQA_FR_390x844.png`: `390x844`, locale `fr-CA`, etat `debrief_partial_qa`, SHA-256 `a4ad41ce7274843169432b2e9eccd7c3b05feb8837cee56e97ed70a87cc245f2`
- `LivingHive_PerimeterSortie_NotConfigured_EN_1600x900.png`: `1600x900`, locale `en-US`, etat `not_configured`, SHA-256 `58fc2a1ee19887982fb2c9d02b2ee6c4e347284d7f06f17f40b8cbbc60b034fd`
- `LivingHive_PerimeterSortie_ReadyQA_EN_1600x900.png`: `1600x900`, locale `en-US`, etat `ready_qa`, SHA-256 `ef4e232d4cc785b0217e506951d87c7129b9a3f1663828f3db5c4d894bb7f05e`
- `LivingHive_PerimeterSortie_OfflineReadOnlyQA_EN_1600x900.png`: `1600x900`, locale `en-US`, etat `offline_qa`, SHA-256 `bb7c2a2ce5b0b17710a1686424aa5f6a15130e58e3ebd569832ec5cc77532d31`
- `LivingHive_PerimeterSortie_DebriefFullQA_EN_1600x900.png`: `1600x900`, locale `en-US`, etat `debrief_full_qa`, SHA-256 `35eacd2150dc8e6e6151b73bfc7eba9bed3e48b4a6149acb40d155bb9ada5eac`
