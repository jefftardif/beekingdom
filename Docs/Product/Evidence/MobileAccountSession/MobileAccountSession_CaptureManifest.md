# Mobile Account Session - manifeste de captures

- Scene: `Assets/Scenes/LivingHive.unity`
- Etats `not_configured`: configuration runtime absente, aucun formulaire, identifiant, jeton ou statut live invente
- Etats `*_qa`: transport et coffre strictement en memoire dans le harnais de capture, aucun appel serveur, aucune persistence, marques comme apercu QA
- Mot de passe affiche ou inscrit au manifeste: `false`
- Jeton affiche ou inscrit au manifeste: `false`
- Production: opt-in par ressource runtime absente; portes serveur fermees; aucun compte reel
- Appareil production cible: access token memoire, refresh Android Keystore, identifiant installation aleatoire non autoritaire
- Serveur: identite, session, expiration, rotation, revocation et autorite de jeu
- Terrain 50x50, image de ruche et scenes modifies: `false`

- `MobileAccountSession_NotConfigured_FR_390x844.png`: `390x844`, locale `fr-CA`, etat `not_configured`, SHA-256 `f5b97beba1a2ab27f2a38dc27d9219cce9d8830d9f9be63d7b5d7315961f6730`
- `MobileAccountSession_ReadyFormQA_FR_390x844.png`: `390x844`, locale `fr-CA`, etat `ready_form_qa`, SHA-256 `07964b92dfcedf76011f7781227b9d02c294f12093580b0bc558bee1603b780e`
- `MobileAccountSession_NotConfigured_EN_1600x900.png`: `1600x900`, locale `en-US`, etat `not_configured`, SHA-256 `05ceb3021207f7c708517c61eb6dbc78408c4189c9a5c5aa9ed90a57e5b94256`
- `MobileAccountSession_AuthenticatedPreviewQA_EN_1600x900.png`: `1600x900`, locale `en-US`, etat `authenticated_preview_qa`, SHA-256 `3b8821d407e2b262055e63480a6337e907488184e639a267bad47a9ec6abc294`
