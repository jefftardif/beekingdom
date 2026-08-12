# BeeKingdom — Alpha Readiness Review

**Date :** 2026-08-03
**Portée :** audit complet, aucune ligne de code écrite, aucun fichier Unity modifié.
**Question à laquelle ce document répond :** *que manque-t-il réellement avant que BeeKingdom puisse être donné à 20 testeurs externes pendant une semaine ?*

**Méthode :** six recherches indépendantes ont été menées sur le dépôt réel (code serveur, code client, fichiers de configuration commités, documentation interne), sans supposer que la documentation existante est à jour — chaque affirmation ci-dessous est vérifiée par lecture directe du code ou de la configuration, avec fichier et ligne à l'appui quand c'est pertinent.

---

## Constat central (à lire avant tout le reste)

**Le problème n° 1 n'est pas un manque de fonctionnalités. C'est que le jeu, tel qu'il est actuellement commité, ne peut être donné à personne d'externe.**

Preuves convergentes, trouvées indépendamment par plusieurs recherches :

- La connexion elle-même est désactivée par défaut : `AccountSessionReadiness` vaut `NotLive` / `AccountCreationAllowed=false` / `SessionCreationAllowed=false` dans **tous** les fichiers `appsettings.json` commités (base, Development, Production). Sans une variable d'environnement non commitée, l'écran de connexion n'affiche même pas les champs email/mot de passe.
- Il n'existe **aucun endpoint de création de compte email/mot de passe** en dehors d'un outil de développement (`/dev/seed-account`, désactivé hors Development). La seule vraie création de compte en libre-service est la connexion Google — fonctionnelle, mais jamais testée en dehors de la machine d'un seul développeur.
- Le seul fichier de configuration client (`MobileAccountSessionRuntime.asset`) pointe vers `http://localhost:5067` — l'adresse de la machine d'un développeur. Aucun testeur externe ne peut l'atteindre.
- Comptes, sessions et colonies utilisent un stockage **en mémoire pure** par défaut (tout est perdu au redémarrage du serveur) ; l'état des ruches retombe sur des fichiers JSON locaux (ne survit pas à un redéploiement, ne fonctionne pas avec plusieurs instances).
- Presque **tous** les drapeaux de fonctionnalités de contenu (Combat, Construction, Collecte mondiale, Recherche, Recrutement, Production hors-ligne, Chemin stratégique, etc.) sont `Enabled: false` avec un **catalogue vide** dans les trois fichiers `appsettings*.json` commités.
- Aucun build Windows n'a jamais été produit — et un bug de code confirmé casserait la connexion sur un vrai exécutable Windows (le stockage protégé du jeton de rafraîchissement n'a une implémentation que pour Android/iOS/Éditeur).
- L'APK Android a été généré avec succès mais **jamais installé ni lancé sur un vrai appareil** (état explicitement noté « PENDING » dans la documentation QA existante).

Conséquence directe : **avant de discuter du contenu, il faut d'abord rendre le jeu atteignable.** C'est du travail d'infrastructure/configuration, pas du gameplay — ce qui est une bonne nouvelle : c'est rapide à corriger comparé à construire une fonctionnalité manquante.

---

## 1. Systèmes déjà suffisamment complets (« Alpha Ready »)

Aucune amélioration n'est proposée sur cette liste. Ce sont des acquis solides sur lesquels s'appuyer.

| Système | Pourquoi il est prêt |
|---|---|
| **Architecture serveur-autoritaire** (idempotence, revision, mutations atomiques) | Motif appliqué de façon cohérente sur tous les sous-systèmes, 167 tests serveur verts, aucune faille de confiance trouvée dans l'audit. |
| **Contenu des 7 chapitres du tutoriel** | Rédigé intégralement (collecte, couvain, recrutement, amélioration, défense, carte du monde, butinage), aucun chapitre inachevé ou provisoire trouvé. |
| **Carnet du Bestiaire** (structure, historique personnel, badges) | Fonctionnalité complète et testée cette session, y compris souvenir de dernier combat et progression. |
| **Championnes** (biographies, rôles, voix, barks contextuels) | 5 championnes documentées, système de voix livré et vérifié, rareté des interventions respectée. |
| **Carte du monde — rendu, navigation, Points d'Intérêt, ambiance météo, présence ambiante, mémoire visuelle des ressources** | Toutes ces couches fonctionnent et sont visibles sans configuration supplémentaire une fois le serveur atteignable. |
| **Pipeline de build Android** | Produit un APK valide et installable (archive vérifiée, manifeste correct) — seule l'étape « tester sur un vrai appareil » manque (listée en section 2). |
| **Infrastructure de localisation** | 1512/1512 clés en parité FR/EN pour tout ce que couvre le tutoriel — un vrai travail de traduction, pas un copier-coller. |
| **Mécanique brute du Combat de Patrouille** (calcul déterministe, avantages/désavantages de troupes, pertes/récupération) | Code sain, sans exploit trouvé — la profondeur en tant qu'expérience joueur est un sujet séparé (section 2), mais le moteur lui-même n'a pas besoin d'être retouché. |

---

## 2. Systèmes incomplets, classés par ordre d'importance

Légende : 🔴 Bloquant · 🟠 Important · 🟢 Peut attendre la Bêta

### 🔴 2.1 — Le jeu est injoignable pour un testeur externe
**Pourquoi indispensable :** sans ceci, rien d'autre dans ce document n'a d'importance — aucun testeur ne peut même démarrer une session.
**Ce qui manque concrètement :** un serveur hébergé et atteignable publiquement (pas `localhost`) ; `AccountSessionReadiness` basculé sur `Live` dans la configuration réellement déployée ; le fichier `MobileAccountSessionRuntime.asset` du build remis aux testeurs pointant vers cette adresse réelle, pas vers la machine d'un développeur.
**Difficulté :** faible à moyenne — aucun nouveau code requis, uniquement de la configuration et de l'hébergement. La difficulté vient de l'absence totale de plan d'hébergement documenté à ce jour, pas de la complexité technique.
**Gain joueur :** total et immédiat — c'est la condition d'existence de tout le reste.

### 🔴 2.2 — Aucun chemin de création de compte réel pour un testeur externe
**Pourquoi indispensable :** la seule création de compte en libre-service qui fonctionne est « Connexion avec Google » ; il n'existe aucun enregistrement email/mot de passe en dehors d'un outil de développement.
**Ce qui manque concrètement :** soit accepter que Google soit le seul chemin pour cette alpha (et le communiquer clairement aux 20 testeurs), soit construire un vrai enregistrement email/mot de passe.
**Difficulté :** faible si on accepte « Google uniquement » pour cette alpha ; moyenne si un enregistrement email/mot de passe réel est exigé.
**Gain joueur :** élevé — un testeur qui ne peut pas créer de compte ne teste rien.

### 🔴 2.3 — Fiabilité de la sauvegarde (comptes, sessions, colonies)
**Pourquoi indispensable :** dans la configuration commitée, comptes/sessions/colonies vivent uniquement en mémoire — un redémarrage serveur (planifié ou non) efface tout le monde. L'état des ruches retombe sur des fichiers JSON locaux, qui ne survivent pas à un redéploiement.
**Ce qui manque concrètement :** activer réellement le fournisseur SQL déjà écrit dans le code (`SqlAccountRepository`, `SqlColonyRepository`, etc. existent, ils ne sont simplement jamais sélectionnés par défaut) pour l'environnement de test.
**Difficulté :** faible — le code existe déjà, il s'agit de configuration et d'une vraie base SQL accessible en continu.
**Gain joueur :** total — perdre sa progression pendant une semaine de test détruirait la confiance des testeurs immédiatement.

### 🔴 2.4 — Aucun build Windows viable
**Pourquoi indispensable :** la feuille de route exemple de Jeff liste Windows comme une case séparée d'Android ; or aucun build Windows n'a jamais été produit, et un bug de code confirmé bloquerait la connexion sur un tel build (le stockage protégé du jeton n'existe que pour Android/iOS/Éditeur).
**Ce qui manque concrètement :** soit une implémentation Windows du stockage de jeton protégé, soit retirer Windows du périmètre de cette alpha et le documenter explicitement.
**Difficulté :** faible à moyenne (un stockage de secours suffisant existe déjà pour l'Éditeur — le même principe peut s'étendre à un vrai build Windows).
**Gain joueur :** dépend du périmètre voulu — si des testeurs Windows sont prévus, c'est bloquant ; sinon, ce point disparaît en retirant Windows du 0.1.

### 🔴 2.5 — L'APK Android n'a jamais tourné sur un vrai appareil
**Pourquoi indispensable :** toute la validation existante est statique (archive, manifeste) — la vérification tactile, les performances réelles et la variance d'écran entre appareils réels n'ont jamais été mesurées.
**Ce qui manque concrètement :** installer et jouer sur au moins 2-3 téléphones Android réels de gammes différentes avant la semaine de test.
**Difficulté :** faible — c'est une tâche de test manuel, pas de développement.
**Gain joueur :** élevé — évite de découvrir un problème d'interface tactile ou de performance en même temps que 20 testeurs.

### 🟠 2.6 — Économie non activée et non validée
**Pourquoi indispensable :** presque tous les catalogues de coûts/récompenses (construction, collecte mondiale, plusieurs autres) sont vides et désactivés dans la configuration commitée ; là où des chiffres existent (combat), ils sont explicitement auto-qualifiés de provisoires dans le code lui-même. Aucun document de calibrage n'existe.
**Ce qui manque concrètement :** décider quels systèmes sont réellement activés pour cette alpha, remplir leurs catalogues avec de vraies valeurs, et jouer une vraie session complète pour sentir le rythme avant l'envoi aux testeurs.
**Difficulté :** moyenne — pas de nouvelle architecture, mais un vrai travail de calibrage et de décision produit.
**Gain joueur :** élevé — une économie vide ou mal réglée casse la progression dès la première session.

### 🟠 2.7 — Le combat est fonctionnel mais mécaniquement superficiel
**Pourquoi c'est important, pas bloquant :** le moteur est sain (déterministe, sans exploit), mais réduit à 2 décisions réelles (palier + composition), sans état d'échec véritable, et le cycle avantage/désavantage à 3 familles s'apprend une fois pour toutes en quelques combats. Pour une alpha de découverte, c'est jouable ; ce n'est pas encore « satisfaisant » au sens où Jeff l'entend.
**Ce qui manque concrètement :** rien de bloquant pour un 0.1 — mais un chantier de profondeur (variance, vrai état d'échec, décision supplémentaire) devrait suivre rapidement après le retour des testeurs.
**Difficulté :** moyenne à élevée selon l'ambition.
**Gain joueur :** moyen à élevé sur la durée, faible impact immédiat sur une semaine de découverte.

### 🟠 2.8 — Construction très incomplète
**Pourquoi c'est important :** sur 14 emplacements de bâtiment visibles dans la ruche, 7 sont des placeholders explicites (« Fonctionnalité à venir »), et parmi les 4 reliés à un vrai mécanisme d'amélioration serveur, tous les 4 sont actuellement désactivés avec un catalogue vide. Les 3 restants pointent vers d'autres systèmes eux-mêmes désactivés.
**Ce qui manque concrètement :** activer et peupler le catalogue des 4 bâtiments réels (déjà couvert par 2.6) ; pour les 7 placeholders, aucune action requise pour cette alpha — ce sont des promesses de contenu futur, pas un manque de l'alpha.
**Difficulté :** faible pour les 4 bâtiments réels (config + catalogue) ; les 7 placeholders sont hors périmètre.
**Gain joueur :** élevé pour les 4 bâtiments réels, nul pour les 7 placeholders tant qu'ils restent étiquetés comme tels.

### 🟠 2.9 — Aucun moyen de savoir ce qui s'est passé pendant la semaine de test
**Pourquoi indispensable :** le rapport de crash et l'analytique Unity sont explicitement désactivés dans les paramètres du projet ; aucun SDK tiers de crash/analytique n'est intégré ; les journaux serveur sont du texte console non structuré, sans identifiant de corrélation ; aucun bouton de signalement de bug n'existe dans le client.
**Ce qui manque concrètement :** au minimum, un canal de retour (Discord/formulaire) partagé avec les testeurs, et une procédure manuelle pour retrouver l'état serveur d'un testeur donné à partir de son identifiant de compte.
**Difficulté :** faible — pas besoin d'un SDK complexe pour un minimum viable ; un SDK de crash reporting reste une amélioration à moindre effort si le temps le permet.
**Gain joueur :** indirect mais critique pour l'équipe — sans ça, la semaine de test ne produit presque aucune information exploitable.

### 🟠 2.10 — Le tutoriel n'explique ni les Championnes ni le Bestiaire
**Pourquoi indispensable :** ce sont deux systèmes livrés et jouables, mais totalement absents de la progression guidée — un testeur ne les découvrira que par hasard.
**Ce qui manque concrètement :** un chapitre ou une étape courte introduisant les deux, ou a minima une incitation ponctuelle (déjà dans l'esprit des sprints d'ambiance récents).
**Difficulté :** faible à moyenne.
**Gain joueur :** moyen — ces systèmes existent déjà, il s'agit seulement de les rendre découvrables.

### 🟠 2.11 — Sécurité de base de l'authentification absente
**Pourquoi c'est important :** aucune récupération de mot de passe n'existe ; aucune limitation de débit sur les tentatives de connexion. Sur une semaine avec 20 comptes réels, un testeur qui oublie son mot de passe est bloqué sans recours, et rien ne protège contre des tentatives de connexion répétées.
**Ce qui manque concrètement :** au minimum un flux de réinitialisation de mot de passe et une limitation de débit basique sur `/auth/login`.
**Difficulté :** faible à moyenne.
**Gain joueur :** moyen — évite une frustration évitable et un risque de sécurité inutile pour un test à petite échelle.

### 🟢 2.12 — Localisation incomplète sur 3 écrans
**Pourquoi ça peut attendre :** le Carnet du Bestiaire, le Combat de Patrouille et les Défis de la ruche affichent du texte français même en mode anglais (aucune clé enregistrée pour ces écrans, contrairement au tutoriel qui est complet à 100%).
**Difficulté :** faible.
**Gain joueur :** faible si les 20 testeurs sont francophones ; moyen sinon.

### 🟢 2.13 — Cadre du « MMO » et des « Alliances » à recalibrer dans la communication, pas dans le code
**Pourquoi ça peut attendre :** aucune interaction réelle entre joueurs n'existe au-delà d'une présence ambiante (« Colonie #XXXX ») affichée une fois à la connexion ; le bouton « Alliance » ouvre un panneau qui dit lui-même « aucune alliance live connectée ». Ce n'est pas un bug — le jeu ne prétend jamais l'inverse dans son propre texte — mais si ces mots sont utilisés sans nuance auprès des 20 testeurs, attendre de la confusion.
**Ce qui manque concrètement :** rien à coder pour cette alpha ; seulement présenter ces systèmes aux testeurs pour ce qu'ils sont réellement.
**Difficulté :** nulle (communication uniquement).
**Gain joueur :** évite une déception évitable.

### 🟢 2.14 — Chat : incohérence de configuration à trancher
**Pourquoi ça peut attendre :** le système de chat est en fait plus abouti qu'il n'y paraît (vrai pipeline temps réel), mais la configuration Production commitée l'active alors qu'une règle de gouvernance antérieure disait explicitement de ne pas le faire sans autorisation — un probable oubli, pas une décision.
**Ce qui manque concrètement :** une décision explicite (l'activer sciemment pour l'alpha, ou le désactiver comme prévu à l'origine).
**Difficulté :** nulle — juste une décision et un interrupteur.
**Gain joueur :** faible à moyen selon la décision.

### 🟢 2.15 — Volume de contenu limité pour une semaine complète
**Pourquoi ça peut attendre :** 7 paliers de combat, 3 nœuds de ressource et 5 championnes constituent une boucle correcte pour une découverte, mais un testeur assidu peut en faire le tour en 1-2 jours. C'est normal et attendu pour une toute première alpha — ce n'est pas un défaut à corriger avant l'envoi, seulement une réalité à connaître à l'avance.
**Difficulté :** élevée (c'est un chantier de contenu à part entière).
**Gain joueur :** élevé, mais pour la Bêta plutôt que pour ce premier test.

### 🟢 2.16 — Hygiène des données de test
**Pourquoi ça peut attendre :** d'anciens comptes de développement/QA pourraient apparaître comme des « fantômes » dans l'échantillon de présence ambiante si les mêmes données servent à l'alpha.
**Difficulté :** faible.
**Gain joueur :** faible mais évite une confusion cosmétique.

### 🟢 2.17 — Filtre de contenu sur les pseudonymes
**Pourquoi ça peut attendre :** déjà noté par l'équipe elle-même comme « à considérer avant un vrai lancement public » — pour un groupe fermé de 20 testeurs connus, le risque est faible.
**Difficulté :** faible.
**Gain joueur :** faible pour cette alpha, deviendra important pour un lancement public.

---

## 3. Feuille de route Alpha

### ALPHA 0.1 — « Le jeu est atteignable et ne perd rien »
*Condition de sortie : un testeur externe peut créer un compte, jouer une session, fermer l'application, revenir le lendemain et retrouver exactement sa progression.*

- [ ] 🔴 Serveur hébergé et atteignable publiquement (2.1)
- [ ] 🔴 `AccountSessionReadiness` = Live sur cet hébergement réel (2.1)
- [ ] 🔴 Build client pointant vers le serveur réel, pas `localhost` (2.1)
- [ ] 🔴 Chemin de création de compte confirmé pour les testeurs (Google, ou email/mot de passe si exigé) (2.2)
- [ ] 🔴 Persistance réelle activée pour comptes/sessions/colonies/ruches (2.3)
- [ ] 🔴 Décision Windows : corriger le bug de connexion, ou retirer Windows du périmètre (2.4)
- [ ] 🔴 APK testé sur au moins 2-3 appareils Android réels (2.5)

### ALPHA 0.2 — « Il y a une vraie progression à tester »
*Condition de sortie : un testeur peut avancer dans l'économie et la construction sans rencontrer de mur artificiel dû à une configuration désactivée.*

- [ ] 🟠 Catalogues économiques peuplés et activés pour les systèmes retenus (2.6)
- [ ] 🟠 Les 4 bâtiments à mécanisme réel activés et testés en jeu (2.8)
- [ ] 🟠 Canal de retour testeurs + procédure de diagnostic minimal (2.9)
- [ ] 🟠 Tutoriel étendu pour introduire Championnes et Bestiaire (2.10)
- [ ] 🟠 Réinitialisation de mot de passe + limitation de débit sur la connexion (2.11)

### ALPHA 0.3 — « Poli et honnête »
*Condition de sortie : rien dans le jeu ne prétend être plus que ce qu'il est ; ce qui manque de traduction ne dérange pas les testeurs prévus.*

- [ ] 🟢 Localisation complétée sur Bestiaire/Combat/Défis (2.12)
- [ ] 🟢 Communication testeurs clarifiée sur la portée réelle du « MMO »/Alliances (2.13)
- [ ] 🟢 Décision explicite sur le chat en production (2.14)
- [ ] 🟢 Nettoyage des données de test avant handoff (2.16)
- [ ] 🟢 Filtre de contenu sur les pseudonymes (2.17)

### Vers la Bêta (hors périmètre Alpha 0.1-0.3, à ne pas commencer avant)
- Profondeur du combat (2.7)
- Extension du volume de contenu (2.15)
- Construction des 7 emplacements de bâtiment encore « à venir »
- Vraies alliances / interactions joueur-à-joueur

---

## 4. Objectif final

**Aucun sprint futur ne devrait être choisi sans se référer explicitement à une case de ce document.** Le prochain sprint recommandé est le premier bloc non coché d'ALPHA 0.1 : rendre le jeu atteignable, avec une sauvegarde qui ne perd rien. Tant que cette base n'est pas cochée, aucune amélioration de contenu, de combat ou d'ambiance ne peut être réellement testée par personne d'externe — c'est la seule vraie priorité tant que ALPHA 0.1 n'est pas entièrement cochée.
