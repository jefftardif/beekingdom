# LivingHive — choix de langue mobile au premier lancement

Date de ratification : 22 juillet 2026  
Statut : réalisé et ratifié sous Unity 6000.5.3f1

## Résultat produit

L’entrée de `LivingHive` propose désormais deux commandes tactiles `FR` et `EN` avant toute connexion locale ou ouverture du tutoriel. La langue active est soulignée en or, l’ensemble de l’accueil/authentification locale bascule immédiatement, puis le choix est mémorisé pour les prochaines sessions.

- cibles FR/EN d’au moins `44x44 px` en 390x844 et 1600x900;
- français `fr-CA` et anglais `en-US`;
- préférence sauvegardée dans `PlayerPrefs` sous une clé versionnée;
- en l’absence de préférence, initialisation depuis la langue système;
- préférence valide prioritaire sur la langue système;
- locale inconnue refusée sans écraser la préférence valide;
- changement de langue répercuté sur la narration active du tutoriel;
- aucun changement de coût, délai, économie, progression ou contenu de jeu.

Les 40 nouveaux libellés `splash.*` localisent les onglets, titres, champs, commandes, avertissements et messages d’état. Les catalogues contiennent chacun 620 entrées uniques, sans doublon ni asymétrie; les 40 références d’entrée existent dans les deux langues.

## Frontière mobile / serveur

Cette préférence relève exclusivement de l’appareil :

- appareil : langue active, préférence locale, rendu et texte traduit;
- serveur : aucune autorité et aucune donnée ajoutée pour ce jalon;
- synchronisation de compte : volontairement absente tant qu’un vrai profil serveur de préférences n’existe pas;
- hors ligne : le choix reste entièrement fonctionnel.

Le thread Intégrateur n’a donc reçu aucun ajout `Server/` pour cette tranche. Le bloc Communication, ses tests et ses documents n’ont pas été modifiés.

## Validation

- compilation C# générée : `Assembly-CSharp-Editor.csproj`, 0 erreur; 217 avertissements historiques;
- F8 LivingHive : `Artifacts/LivingHiveLanguageSelectorF8.log`, sortie 0, un marqueur de succès, zéro échec, zéro `error CS`;
- captures : `Artifacts/LivingHiveLanguageSelectorCapture.log`, sortie 0, un marqueur de succès, zéro échec, zéro `error CS`;
- preuves : `Docs/Product/Evidence/LivingHiveLanguageSelector`;
- manifeste : `Docs/Product/Evidence/LivingHiveLanguageSelector/LivingHiveLanguageSelectorManifest.md`;
- quatre PNG exacts : deux 390x844 et deux 1600x900;
- inspection visuelle finale : texte FR/EN lisible, boutons sans collision, avertissement court sous le logo, langue active soulignée.

Empreintes finales :

- `LivingHive_Language_fr-CA_390x844.png` : `EA08EE5161BAD7FB85A764EF7504387050734757EA5F1C01AB9A8A75F0E35D95`;
- `LivingHive_Language_en-US_390x844.png` : `83C2FC5B5CAED5D51426AFF190C9699666F12DA7E2812510E003FEDA1C4A9C4F`;
- `LivingHive_Language_fr-CA_1600x900.png` : `DCEA950E8E2CCEF6A3CD9E0DB696254D4AA1023536767540FEA45BCFCA7EF367`;
- `LivingHive_Language_en-US_1600x900.png` : `D0096D595D046BEC650D37EC89EE8B978996F8392C48C17647096A56878A376D`.

## Fichiers de code et de données

- `Assets/BeeKingdom/Localization/BeeLocalization.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxSplashLanguageCapture.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxSplashLanguageCapture.cs.meta`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`

## Fondations préservées

- scène canonique 50x50 : inchangée, 7 776 octets, SHA-256 `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- image de base LivingHive : inchangée, SHA-256 `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`;
- aucune image terrain, tuile, scène canonique ou image de ruche régénérée, recadrée ou remplacée.
