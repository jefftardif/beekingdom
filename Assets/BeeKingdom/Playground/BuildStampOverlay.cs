using UnityEngine;

namespace BeeKingdom.Playground
{
    // M056-CL : identification de build.
    //
    // Objectif : quand un screenshot ou un rapport de bug nous revient d'un testeur
    // externe, savoir EXACTEMENT quelle build a ete utilisee.
    //
    // Deux canaux, volontairement redondants :
    //   1. une ligne ecrite dans le Player.log au demarrage (survit meme si le testeur
    //      ne pense pas a screenshoter le coin de l'ecran) ;
    //   2. un petit label discret en bas a droite.
    //
    // Le label est un GUI.Label NON interactif (aucun controle IMGUI cliquable), donc il
    // ne peut pas capturer de clic destine au jeu. Il est dessine avec une depth elevee,
    // ce qui le place SOUS les autres OnGUI du jeu : il ne peut jamais recouvrir ni gener
    // une fenetre de HiveViewProductUiPresenter.
    public static class BuildStampOverlay
    {
        private const string ResourcePath = "BeeKingdom/BuildStamp";
        private static string _stamp;

        public static string Stamp
        {
            get
            {
                if (_stamp == null) _stamp = LoadStamp();
                return _stamp;
            }
        }

        private static string LoadStamp()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            string fromAsset = asset != null ? asset.text : null;
            if (!string.IsNullOrWhiteSpace(fromAsset)) return fromAsset.Trim();
            // Repli : au minimum la version des Player Settings.
            return "BeeKingdom " + Application.version;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Debug.Log("[BuildStamp] " + Stamp);

            var go = new GameObject("BuildStampOverlay");
            go.hideFlags = HideFlags.DontSave;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<BuildStampOverlayBehaviour>();
        }
    }

    internal sealed class BuildStampOverlayBehaviour : MonoBehaviour
    {
        private GUIStyle _style;

        private void OnGUI()
        {
            // Depth NEGATIVE = dessine par-dessus les autres OnGUI du jeu. Verifie en build
            // standalone : avec une depth elevee (sous le reste), le label etait entierement
            // masque par l'IMGUI plein ecran de HiveViewProductUiPresenter.
            //
            // Sur la regle CLAUDE.md des overlays IMGUI : elle vise les CONTROLES qui peuvent
            // capturer un clic (Button, Box, etc.). GUI.Label ne consomme aucun evenement
            // souris - il n'alloue pas de control ID interactif - donc le dessiner au-dessus
            // ne peut pas voler un clic destine au jeu.
            GUI.depth = -1000;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.alignment = TextAnchor.LowerRight;
                _style.fontSize = 11;
                _style.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
            }

            string text = BuildStampOverlay.Stamp;
            const float width = 520f;
            const float height = 18f;
            // Legerement remonte pour ne pas tomber sur le liseré du menu du bas.
            var rect = new Rect(Screen.width - width - 10f, Screen.height - height - 26f, width, height);

            // Legere ombre pour rester lisible sur fond clair comme sur fond sombre.
            Color previous = _style.normal.textColor;
            _style.normal.textColor = new Color(0f, 0f, 0f, 0.35f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _style);
            _style.normal.textColor = previous;
            GUI.Label(rect, text, _style);
        }
    }
}
