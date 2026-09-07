using System.Reflection;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    // M056A-CL : garde de regression pour le defaut "HiveMap -> WorldMap -> HiveMap : la carte
    // ne se deplace plus et ne zoome plus, alors que les batiments et les menus restent
    // cliquables" (rapporte par un testeur externe, reproduit en Play Mode par le CEO).
    //
    // CAUSE : HiveViewProductUiPresenter est une classe STATIQUE. Ses booleens "ecran ouvert"
    // survivent aux changements de scene. Un overlay ouvert dans la WorldMap (par exemple
    // combatPatrolOverlayOpen, ouvert par le bouton ATTAQUER) restait donc a true au retour
    // dans la ruche. Or ce drapeau est lu par PremiumUiBlocksWorldInput(), qui garde
    // UNIQUEMENT la camera - pas les batiments ni les menus, qui passent par
    // HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking(). D'ou l'asymetrie exacte du
    // rapport. Et comme la HiveMap ne redessine jamais ces overlays, plus rien ne pouvait les
    // refermer : l'etat etait irrecuperable sans redemarrer le jeu.
    //
    // Ces tests verrouillent les deux moities de la correction : le reset couvre bien les
    // drapeaux qui tuent la camera, et il est bien branche sur le seam de chargement de scene.
    public sealed class HiveMapSceneReentryInputTests
    {
        private static FieldInfo Flag(string name)
        {
            FieldInfo field = typeof(HiveViewProductUiPresenter).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, "Drapeau attendu introuvable : " + name);
            return field;
        }

        [TearDown]
        public void ResetPresenterState()
        {
            HiveViewProductUiPresenter.ResetPremiumScreensForProof();
        }

        // Le coeur du bug : un overlay ouvert ailleurs bloque la camera, et le reset doit le lever.
        [TestCase("combatPatrolOverlayOpen")]
        [TestCase("communicationPanelOpen")]
        [TestCase("missionsCenterOpen")]
        [TestCase("resourceInventoryOpen")]
        [TestCase("bestiaryCodexOverlayOpen")]
        public void StaleOverlayFlag_BlocksCamera_AndIsClearedByReset(string flagName)
        {
            HiveViewProductUiPresenter.ResetPremiumScreensForProof();
            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False,
                "Etat de depart : aucun overlay ouvert, la camera doit repondre");

            FieldInfo flag = Flag(flagName);
            flag.SetValue(null, true);

            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.True,
                flagName + " doit bien bloquer l'input camera - sinon ce test ne prouve rien");

            // C'est exactement ce que fait desormais le chargement de scene de la ruche.
            HiveViewProductUiPresenter.ResetPremiumScreensForProof();

            Assert.That(HiveViewProductUiPresenter.PremiumWorldInputBlockedForProof, Is.False,
                "Apres retour dans la ruche, " + flagName + " doit etre relache : sans ca, le pan " +
                "et le zoom restent morts definitivement alors que l'UI repond encore");
            Assert.That((bool)flag.GetValue(null), Is.False);
        }

        [Test]
        public void SceneLoadSeam_ResetsPresenterOverlayState()
        {
            MethodInfo seam = typeof(HiveMapRuntimeBootstrapInitializer).GetMethod(
                "ResetPresenterOverlayStateForSceneEntry",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(seam, Is.Not.Null,
                "HiveMapRuntimeBootstrapInitializer doit remettre a zero l'etat d'overlay du " +
                "presenter a chaque chargement de scene ruche : c'est le seul point ou l'etat " +
                "statique herite d'une autre scene peut etre relache");
        }
    }
}
