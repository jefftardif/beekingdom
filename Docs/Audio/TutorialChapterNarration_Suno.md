# Bee Kingdom - Narration des chapitres

## Source de verite

Le texte a narrer provient exclusivement de la cle `tutorial.chapter_XX.intro.narration` du catalogue de langue. Il ne doit pas etre recopie puis modifie independamment dans Suno, un prefab ou un script Unity.

Les identifiants runtime sont:

* `tutorial.chapter_01.intro.<locale>`
* `tutorial.chapter_02.intro.<locale>`
* `tutorial.chapter_03.intro.<locale>`
* `tutorial.chapter_04.intro.<locale>`
* `tutorial.chapter_05.intro.<locale>`
* `tutorial.chapter_06.intro.<locale>`
* `tutorial.chapter_07.intro.<locale>`

Exemples: `tutorial.chapter_04.intro.fr-CA` et `tutorial.chapter_04.intro.en-US`.

## Livraison audio

Convention de chemin proposee:

`Assets/BeeKingdom/Audio/VoiceOver/<locale>/tutorial.chapter_XX.intro.ogg`

Pour chaque piste, conserver aussi le fichier maitre non compresse hors du build. La voix commence apres le fade-in initial et lit le texte complet, meme si l'effet visuel de machine a ecrire est plus rapide. Le premier clic revele instantanement tout le texte sans couper la voix; le second ferme la page avec son fade-out. Une future commande audio pourra interrompre proprement la narration si le joueur ferme, change de chapitre ou change de langue.

## Effets sonores

Le runtime expose l'identifiant de piste, le texte complet, le nombre de caracteres visibles et l'opacite de la page. Cette interface permet:

* un son d'ouverture doux pendant le fade-in;
* un son de frappe discret cadence par les nouveaux caracteres, avec limite de frequence;
* un son de validation lorsque le recit est complet;
* un son de fermeture pendant le fade-out.

Le mode mouvement reduit affiche tout le texte immediatement et ne doit pas produire une rafale de sons de frappe.
