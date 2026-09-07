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
