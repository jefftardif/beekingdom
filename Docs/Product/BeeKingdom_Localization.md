# Bee Kingdom - Localisation

## Principe

Le francais canadien (`fr-CA`) est la langue source. L'anglais americain (`en-US`) est le premier catalogue secondaire et sert aussi a verifier que l'interface supporte des textes de longueur differente.

Les textes visibles ne doivent plus etre ajoutes directement dans les presenters. Chaque nouveau bouton, message, menu, ressource, batiment et texte narratif recoit une cle stable dans les catalogues JSON:

* `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
* `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`

Le service runtime est `BeeKingdom.Localization.BeeLocalization`. Une cle manquante dans la langue active retombe sur `fr-CA`, puis sur le texte de repli fourni par l'appelant. La langue peut etre changee sans relancer la scene.

## Conventions de cles

* `common.*`: commandes partagees (`close`, `continue`, `upgrade`).
* `nav.*`: navigation principale.
* `resource.*`: miel, cire, pollen et autres ressources.
* `bee.*`: roles et types d'abeilles.
* `building.<hotspot_id>.*`: nom, role et action d'un batiment.
* `ui.<surface>.*`: textes propres a une interface.
* `tutorial.chapter_XX.intro.*`: introduction narrative d'un chapitre.
* `chat.*`: commandes et etats du chat.

Les deux catalogues doivent toujours contenir exactement les memes cles. Les variables utilisent les marqueurs `{0}`, `{1}`, etc. et sont formatees avec la culture active.

## Ajouter une langue

1. Dupliquer le catalogue source et remplacer `locale` par le code BCP 47 choisi.
2. Traduire les valeurs sans modifier les cles ni les marqueurs de format.
3. Ajouter le code a `BeeLocalization.SupportedLocales`.
4. Creer les voix narratives correspondantes en suivant `Docs/Audio/TutorialChapterNarration_Suno.md`.
5. Verifier paysage, portrait, textes longs, caracteres accentues, changement de langue en cours de scene et absence de cle visible a l'ecran.

## Traduction du chat

La localisation de l'interface et la traduction des messages de joueurs sont deux fonctions distinctes. La traduction du chat reste planifiee pendant que le chantier serveur est en pause.

Comportement produit attendu:

* `Traduire` apparait seulement lorsqu'un message est dans une autre langue que celle du joueur.
* Le texte original reste immuable et peut toujours etre restaure avec `Voir l'original`.
* L'interface affiche `Traduit du {langue}` et ne presente jamais la traduction comme le texte de l'auteur.
* La moderation s'applique au texte original; la traduction ne contourne ni blocage ni signalement.
* Les erreurs et langues non supportees conservent le message original visible.

Contrat serveur recommande, a confirmer lors de la reprise du chat:

* cle de cache: `message_id + target_locale + translation_model_version`;
* donnees: langue source detectee, langue cible, texte traduit, fournisseur/version, statut et horodatage;
* traduction demandee explicitement par le joueur, authentifiee et limitee en debit;
* une traduction deja calculee est reutilisee pour les autres lecteurs de la meme langue;
* aucun nouveau message de chat n'est cree par la traduction.

Les cles `chat.translate`, `chat.show_original`, `chat.translated_from`, `chat.translation_loading` et `chat.translation_unavailable` sont deja disponibles en francais et en anglais.

## Etat de migration

Les sept introductions de chapitre, leurs cartes explicatives, les noms/roles/actions des quatorze batiments de LivingHive, les ressources principales, la navigation de base, la file d'attente et les commandes de traduction du chat utilisent maintenant les catalogues. Les anciens textes de preview encore presents dans le grand presenter LivingHive seront migres par surface au moment de leur prochaine passe fonctionnelle, afin d'eviter une modification massive difficile a verifier.
