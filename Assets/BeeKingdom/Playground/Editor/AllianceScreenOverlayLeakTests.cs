using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    /// <summary>
    /// M058-CL - fuite de drapeaux d'input du perimetre Alliance.
    ///
    /// Bug reel reproduit dans Environment2D5D_HiveMap_Test : les trois overlays propres a
    /// l'ecran Centre d'Alliance (profil de membre, panneau d'action rapide, tiroir de chat)
    /// ne sont DESSINES que par DrawAllianceHeadquartersScreen, qui ne tourne que tant que
    /// activeHiveMenu == Alliance. Mais ils sont LUS par PremiumUiBlocksWorldInput(), qui garde
    /// la camera HiveMap (pan + zoom, via BuildingPerspectiveCamera). Toute sortie du menu
    /// Alliance qui ne les nettoyait pas - CloseAllianceOverlayForExternalHost(), ou un
    /// changement de menu par ActivateHiveMenu() par-dessus un profil ouvert - laissait
    /// allianceMemberProfileOpen a vrai sans plus rien a l'ecran pour le refermer : camera
    /// morte definitivement, alors que batiments et menus (qui passent par
    /// AllianceOverlayOpenForExternalHost) repondaient encore. C'est la meme famille que le
    /// bug 6 de M056A-CL, mais SANS changement de scene : le hook de scene ne pouvait pas aider.
    ///
    /// Ces tests verrouillent l'invariant : apres n'importe quelle sortie du perimetre
    /// Alliance, le monde 3D doit redevenir pilotable.
    /// </summary>
    public sealed class AllianceScreenOverlayLeakTests
    {
        [SetUp]
        public void Reset() => HiveViewProductUiPresenter.ResetPremiumScreensForProof();

        [TearDown]
        public void ResetAfter() => HiveViewProductUiPresenter.ResetPremiumScreensForProof();

        [Test]
        public void ClosingAllianceOverlayWhileMemberProfileOpenReleasesWorldInput()
        {
            HiveViewProductUiPresenter.OpenAllianceOverlayForExternalHost();
            HiveViewProductUiPresenter.OpenAllianceMemberProfileForProof("Testeur");

            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.True,
                "Le monde doit rester bloque tant que le profil de membre est affiche.");

            HiveViewProductUiPresenter.CloseAllianceOverlayForExternalHost();

            Assert.That(HiveViewProductUiPresenter.AllianceOverlayOpenForExternalHost, Is.False,
                "L'ecran Centre d'Alliance n'est plus dessine.");
            Assert.That(HiveViewProductUiPresenter.AllianceMemberProfileOpenForProof, Is.False,
                "Le profil de membre ne doit pas survivre a la fermeture de l'ecran qui le dessine.");
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False,
                "La camera HiveMap doit redevenir pilotable apres fermeture du Centre d'Alliance.");
        }

        [Test]
        public void SwitchingToAnotherHiveMenuOverAnOpenProfileReleasesWorldInput()
        {
            HiveViewProductUiPresenter.OpenAllianceOverlayForExternalHost();
            HiveViewProductUiPresenter.OpenAllianceMemberProfileForProof("Testeur");

            // Le joueur clique un autre batiment (Recherche) alors que le profil est ouvert.
            HiveViewProductUiPresenter.OpenResearchOverlayForExternalHost();
            Assert.That(HiveViewProductUiPresenter.AllianceMemberProfileOpenForProof, Is.False,
                "Quitter le menu Alliance doit liberer ses overlays internes.");

            HiveViewProductUiPresenter.CloseResearchOverlayForExternalHost();

            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False,
                "Aucun drapeau orphelin ne doit rester apres le detour par un autre ecran.");
        }

        [Test]
        public void AllianceBackStackStillClosesProfileFirstThenTheScreen()
        {
            // Non-regression M043O-CL : le retour depuis un profil revient au Centre d'Alliance,
            // il ne quitte PAS l'ecran entier. Le monde reste donc bloque apres le 1er retour.
            HiveViewProductUiPresenter.OpenAllianceOverlayForExternalHost();
            HiveViewProductUiPresenter.OpenAllianceMemberProfileForProof("Testeur");

            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.AllianceMemberProfileOpenForProof, Is.False);
            Assert.That(HiveViewProductUiPresenter.AllianceOverlayOpenForExternalHost, Is.True,
                "Le Centre d'Alliance reste ouvert sous le profil.");
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.True,
                "Le monde reste legitimement bloque tant que le Centre d'Alliance est affiche.");

            Assert.That(HiveViewProductUiPresenter.ClosePremiumScreensForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.AllianceOverlayOpenForExternalHost, Is.False);
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False,
                "Le 2e retour quitte l'ecran et rend la camera au joueur.");
        }

        [Test]
        public void OtherPremiumScreensDoNotLeaveOrphanFlagsAfterAnAllianceDetour()
        {
            // Ouverture/fermeture entrelacee de plusieurs ecrans premium : aucun ne doit laisser
            // de drapeau derriere lui une fois la sequence terminee.
            HiveViewProductUiPresenter.OpenAllianceOverlayForExternalHost();
            HiveViewProductUiPresenter.OpenAllianceMemberProfileForProof("Testeur");
            HiveViewProductUiPresenter.CloseAllianceOverlayForExternalHost();

            HiveViewProductUiPresenter.OpenBestiaryCodexOverlayForProof();
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.True);
            HiveViewProductUiPresenter.CloseBestiaryCodexOverlayForProof();
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False);

            HiveViewProductUiPresenter.OpenMilestoneEventOverlayForProof();
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.True);
            HiveViewProductUiPresenter.CloseMilestoneEventOverlayForProof();
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False);

            HiveViewProductUiPresenter.OpenResearchOverlayForExternalHost();
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.True);
            HiveViewProductUiPresenter.CloseResearchOverlayForExternalHost();
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False,
                "Etat de repos attendu apres la sequence complete.");
        }

        [Test]
        public void ClosingAllianceOverlayReleasesCapturedGuiControls()
        {
            HiveViewProductUiPresenter.OpenAllianceOverlayForExternalHost();
            HiveViewProductUiPresenter.OpenAllianceMemberProfileForProof("Testeur");
            GUIUtility.hotControl = 77;
            GUIUtility.keyboardControl = 77;

            HiveViewProductUiPresenter.CloseAllianceOverlayForExternalHost();

            Assert.That(GUIUtility.hotControl, Is.EqualTo(0));
            Assert.That(GUIUtility.keyboardControl, Is.EqualTo(0));
        }
    }
}
