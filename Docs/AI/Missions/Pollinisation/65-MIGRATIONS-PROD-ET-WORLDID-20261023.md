# P14 - Production SQL et identifiant officiel du monde (23 octobre 2026)

## Migrations effectuees sur SRVESDT
- Exécution sous le compte SQL administrateur autorisé SRVESDT\rdp_jeff.
- Sauvegarde complete pre-migration : BeeKingdom-Pollination-PreMigration-20261023-153619.bak.
- RESTORE VERIFYONLY WITH CHECKSUM : PASS, 14 251 520 octets.
- Les migrations 094 et 095 etaient absentes du registre, mais toutes leurs structures Chat Royal existaient. Validation des colonnes, index et FK AVANT application des scripts idempotents et du registre.
- Migrations 096, 097, 098 appliquees chacune en transaction avec lock BeeKingdom.Database.Migrations, tous les checks de fin PASS.
- Journal de preuve : C:\BeeKingdom\staging\BK-Pollination-Migrate-094-098-20261023.result.txt.
- Desactiver les flags reste le mode de production actuel. Aucune nouvelle sortie civile n'a ete provoquee.

## Correction monde officiel UUID
L'API /health de production annonce defaultWorldId=00000000-0000-0000-0000-000000000101.
L'ancien validateur floral refusait les UUIDs commencant par un chiffre et validait seulement les slugs.
Le serveur et le client Unity reconnaissent maintenant:
- les slugs existants, pour ne pas casser les etats pilotes et tests anterieurs;
- les UUIDs en format canonique 'D' minuscule, non vides;
- les identifiants de sites restent soumis aux restrictions existantes.
Cette modification concerne FloralSiteOptions, les missions civiles et les projections du client Unity.

## Recette unifiee
- Build serveur Release : PASS.
- HiveOperations : 328/328 PASS.
- API hors SQL opt-in : 714 PASS et neuf ignores par conception.
- API SQL opt-in sur LocalDB disposable : 9/9 PASS.
- Contrat client P5 (.NET) avec UUID officiel : PASS.
- Il reste la validation Unity GameView et l'integration non destructive des changements locaux du main.

## Suite et prudence
- Le depot Unity main n'a pas ete modifie.
- Le correctif API doit passer par la branche deploy et l'executor GitHub dedie, avec sauvegarde IIS et health check.
- Avant ouverture des flags de pollinisation : catalogue officialise ancre sur la carte Wave6, prise en charge des identites multi-mondes, tests d'expeditions de deux comptes connectes.
- Ne pas annoncer cette feature comme jouable sans preuve de terrain.