using System;
using System.Reflection;
using BeeKingdom.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    // M056A-CL : garde de regression pour le defaut "la fleche jaune du tutoriel survit a la
    // fermeture" (rapporte par un testeur externe sur la build Windows, reproduit en Play Mode).
    //
    // Cause exacte corrigee : TutorialDialoguePresenter.Hide() ne remettait a zero que son
    // propre etat. Il n'avait aucune reference vers TutorialArrowPresenter ni vers le bloqueur
    // d'input plein ecran de FtueTutorialBootstrap. Sur une etape Require* (dialogue affiche
    // SANS callback de continuation), fermer la bulle ne completait donc jamais l'etape, et
    // rien n'appelait _arrow.Hide() : la fleche et le bloqueur restaient orphelins a l'ecran.
    //
    // Le chemin de fermeture reel passe par des boutons IMGUI (OnGUI), non simulables hors
    // Play Mode. Ces tests verrouillent donc les DEUX invariants qui rendent la correction
    // possible : le contrat de visibilite de la fleche, et le cablage de fermeture entre le
    // presenter de dialogue et le bootstrap. Si quelqu'un retire l'un des deux, la regression
    // redevient possible et ces tests echouent.
    public sealed class FtuePresentationCleanupTests
    {
        [TestCase(320, 568, 0, 20, 320, 528)]
        [TestCase(390, 844, 0, 34, 390, 763)]
        [TestCase(1170, 2532, 0, 102, 1170, 2289)]
        [TestCase(844, 390, 47, 21, 750, 369)]
        [TestCase(1600, 900, 0, 0, 1600, 900)]
        [TestCase(320, 568, 0, 0, 0, 0)]
        public void DialogueLayout_KeepsReadableTextAndTouchActionsInsideSafeArea(
            int width, int height, int x, int y, int safeWidth, int safeHeight)
        {
            Rect safe = new Rect(x, y, safeWidth, safeHeight);
            object layout = typeof(TutorialDialoguePresenter).GetMethod("CalculateLayout", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { (float)width, (float)height, safe });
            Rect panel = LayoutRect(layout, "Panel");
            Rect text = LayoutRect(layout, "Text");
            Rect action = LayoutRect(layout, "Continue");
            Rect skip = LayoutRect(layout, "Skip");
            float scale = (float)layout.GetType().GetField("Scale").GetValue(layout);
            Rect guiSafe = safeWidth == 0 ? new Rect(0, 0, width, height) : new Rect(x, height - safe.yMax, safeWidth, safeHeight);

            Assert.That(guiSafe.Contains(panel.min) && guiSafe.Contains(panel.max), Is.True);
            foreach (Rect child in new[] { text, action, skip, LayoutRect(layout, "Name"), LayoutRect(layout, "Portrait") })
                Assert.That(panel.Contains(child.min) && panel.Contains(child.max), Is.True);
            Assert.That(text.width / scale, Is.GreaterThanOrEqualTo(248f), "Text must not collapse beside the portrait.");
            Assert.That(text.height / scale, Is.GreaterThanOrEqualTo(90f));
            Assert.That(action.height / scale, Is.GreaterThanOrEqualTo(48f));
            Assert.That(text.Overlaps(action) || text.Overlaps(skip) || action.Overlaps(skip), Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DialogueAction_InvokesOnlyItsIntendedCallbackOnce(bool canContinue)
        {
            var go = new GameObject("ftue-dialogue-action-test");
            try
            {
                var dialogue = go.AddComponent<TutorialDialoguePresenter>();
                var arrow = go.AddComponent<TutorialArrowPresenter>();
                int continued = 0;
                int dismissed = 0;
                dialogue.DismissRequested = () => { dismissed++; arrow.Hide(); };
                arrow.Show("building.test");
                dialogue.Show("unknown", "Long tutorial dialogue", canContinue ? (Action)(() => continued++) : null);
                MethodInfo action = typeof(TutorialDialoguePresenter).GetMethod("ContinueOrDismiss", BindingFlags.NonPublic | BindingFlags.Instance);
                action.Invoke(dialogue, null);
                action.Invoke(dialogue, null);
                Assert.That(continued, Is.EqualTo(canContinue ? 1 : 0));
                Assert.That(dismissed, Is.EqualTo(canContinue ? 0 : 1));
                Assert.That(TutorialDialoguePresenter.IsAnyDialogueVisible, Is.False);
                if (!canContinue) Assert.That(arrow.IsVisible, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static Rect LayoutRect(object layout, string field)
        {
            return (Rect)layout.GetType().GetField(field).GetValue(layout);
        }

        [Test]
        public void ArrowHide_ClearsVisiblePresentationState()
        {
            var go = new GameObject("ftue-arrow-test");
            try
            {
                TutorialArrowPresenter arrow = go.AddComponent<TutorialArrowPresenter>();

                arrow.Show("some-target");
                Assert.That(arrow.IsVisible, Is.True, "Show() doit rendre la fleche visible");

                arrow.Hide();
                Assert.That(arrow.IsVisible, Is.False,
                    "Hide() doit retirer la fleche : c'est l'artefact orphelin rapporte par le testeur");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ArrowShow_WithEmptyTarget_StaysHidden()
        {
            var go = new GameObject("ftue-arrow-empty-test");
            try
            {
                TutorialArrowPresenter arrow = go.AddComponent<TutorialArrowPresenter>();

                arrow.Show(string.Empty);

                Assert.That(arrow.IsVisible, Is.False,
                    "Une cible vide ne doit jamais afficher de fleche sans point d'ancrage");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DialoguePresenter_ExposesDismissHook()
        {
            FieldInfo hook = typeof(TutorialDialoguePresenter).GetField(
                "DismissRequested",
                BindingFlags.Public | BindingFlags.Instance);

            Assert.That(hook, Is.Not.Null,
                "TutorialDialoguePresenter doit exposer DismissRequested : c'est le seul moyen " +
                "pour une fermeture de bulle de retirer AUSSI la fleche et le bloqueur d'input");
            Assert.That(hook.FieldType, Is.EqualTo(typeof(Action)));
        }

        [Test]
        public void Bootstrap_ExposesSinglePresentationTeardownEntryPoint()
        {
            MethodInfo dismiss = typeof(FtueTutorialBootstrap).GetMethod(
                "DismissPresentation",
                BindingFlags.Public | BindingFlags.Instance);

            Assert.That(dismiss, Is.Not.Null,
                "FtueTutorialBootstrap doit exposer DismissPresentation() : point d'entree unique " +
                "qui retire fleche + dialogue + bloqueur sans toucher a la progression FTUE");
            Assert.That(dismiss.GetParameters(), Is.Empty);
        }
    }
}
