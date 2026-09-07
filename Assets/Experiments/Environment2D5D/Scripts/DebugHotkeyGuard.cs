using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BeeKingdom.Playground;

namespace BeeKingdom.Experiments.Environment2D5D
{
    // M056B-CL : garde partagee pour les raccourcis clavier de DEBUG.
    //
    // POURQUOI CETTE CLASSE EXISTE (bug reel, confirme par deux testeurs independants)
    // --------------------------------------------------------------------------------
    // FrontalBackdrop lisait Keyboard.current.xKey.wasPressedThisFrame dans Update() pour
    // basculer sa grille jaune de diagnostic. Or le nouvel Input System lit le PERIPHERIQUE
    // BRUT : il ignore totalement le fait qu'un champ de saisie ait le focus clavier. Le
    // compositeur de CHAT ROYAL est un GUI.TextField IMGUI ("chatComposer") : IMGUI consomme
    // bien la frappe pour son propre rendu, mais cela n'empeche en RIEN Keyboard.current de
    // voir la meme touche le meme frame. Consequence : taper un simple message contenant un
    // "x" (par exemple le prenom "Alex") activait silencieusement la grille de debug par
    // dessus tout le jeu, en Play Mode comme en build standalone Windows.
    //
    // La garde couvre les quatre cas, du plus general au plus specifique :
    //   1. build non-developpement -> les raccourcis de debug n'existent tout simplement pas ;
    //   2. un champ de saisie IMGUI a le focus clavier (Chat Royal, chat d'alliance, recherche) ;
    //   3. un champ de saisie uGUI/TMP a le focus (Canvas LivingHiveMenu) ;
    //   4. une fenetre Premium est ouverte - on reutilise la convention DEJA en place dans ce
    //      build (HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, utilisee par
    //      BuildingPerspectiveCamera) plutot que d'inventer un second drapeau.
    //
    // Toute nouvelle lecture clavier de confort/diagnostic doit passer par cette garde.
    public static class DebugHotkeyGuard
    {
        // Le joueur est en train de TAPER : la frappe appartient au champ de saisie, jamais
        // a un raccourci. A utiliser aussi pour l'input de JEU (camera), car cette propriete
        // ne depend PAS du type de build - elle ne desactive rien en Release.
        public static bool TextInputHasFocus
        {
            get
            {
                // Focus clavier IMGUI : c'est LE cas du bug Chat Royal (GUI.TextField).
                if (GUIUtility.keyboardControl != 0) return true;

                // Focus clavier uGUI / TextMeshPro (Canvas LivingHiveMenu).
                EventSystem events = EventSystem.current;
                if (events != null)
                {
                    GameObject selected = events.currentSelectedGameObject;
                    if (selected != null)
                    {
                        if (selected.GetComponent<InputField>() != null) return true;
                        if (selected.GetComponent<TMP_InputField>() != null) return true;
                    }
                }

                return false;
            }
        }

        // true = ignorer la frappe. RESERVE aux raccourcis de DEBUG : contient une garde de
        // type de build qui doit rester hors de l'input de jeu.
        public static bool Blocked
        {
            get
            {
                // 1. Une build livree (Release / Alpha-Internal) n'expose aucun raccourci de
                //    debug actif : meme un appui volontaire ne peut plus rien declencher.
                //    Dans l'editeur, Debug.isDebugBuild vaut true, donc l'outil reste
                //    utilisable par un developpeur - c'est le but.
                if (!Application.isEditor && !Debug.isDebugBuild) return true;

                // 2 et 3. Un champ de saisie a le focus.
                if (TextInputHasFocus) return true;

                // 4. Fenetre Premium ouverte (Chat, Courrier, Recherche, Alliance...).
                if (HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof) return true;

                return false;
            }
        }
    }
}
