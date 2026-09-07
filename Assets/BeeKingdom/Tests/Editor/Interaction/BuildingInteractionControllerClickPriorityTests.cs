using System;
using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Tests.Editor.Interaction
{
    // M045F-CL: BuildingInteractionController.HandlePointer() itself needs a live
    // Camera/Physics.Raycast/Input.GetMouseButtonDown setup that isn't practical to simulate in
    // an EditMode test - DispatchClick(BuildingDefinition) is the extracted, directly-callable
    // decision point ("preempted vs. normal dispatch") HandlePointer delegates to once it has
    // already resolved which building was hit, so these tests exercise the real click-priority
    // rule without needing a scene/raycast.
    public sealed class BuildingInteractionControllerClickPriorityTests
    {
        private GameObject _hostGo;
        private BuildingInteractionController _controller;
        private int _clickedCount;
        private BuildingDefinition _lastClicked;

        [SetUp]
        public void SetUp()
        {
            _hostGo = new GameObject("BuildingInteractionControllerClickPriorityTests_Host");
            _controller = _hostGo.AddComponent<BuildingInteractionController>();
            _clickedCount = 0;
            _lastClicked = null;
            _controller.Selection.BuildingClicked += building => { _clickedCount++; _lastClicked = building; };
            BuildingInteractionController.InteractionPreemptionHook = null;
        }

        [TearDown]
        public void TearDown()
        {
            BuildingInteractionController.InteractionPreemptionHook = null;
            if (_hostGo != null) UnityEngine.Object.DestroyImmediate(_hostGo);
        }

        [Test]
        public void NoHookInstalled_ClickDispatchesNormally()
        {
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery);

            _controller.DispatchClick(building);

            Assert.That(_clickedCount, Is.EqualTo(1));
            Assert.That(_lastClicked, Is.SameAs(building));
            Assert.That(_controller.Selection.CurrentSelection, Is.SameAs(building),
                "normal building opening must still work when nothing is completion-ready");
        }

        [Test]
        public void HookReturnsTrue_ClickIsFullyConsumed_BuildingWindowDoesNotOpen()
        {
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack);
            BuildingInteractionController.InteractionPreemptionHook = _ => true;

            _controller.DispatchClick(building);

            Assert.That(_clickedCount, Is.EqualTo(0),
                "a preempted click must never also fire BuildingClicked - the building window must not open on the same click");
            Assert.That(_controller.Selection.HasSelection, Is.False,
                "a preempted click must not select/open the building either");
        }

        [Test]
        public void HookReturnsFalse_NormalOpeningIsPreserved()
        {
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack);
            BuildingInteractionController.InteractionPreemptionHook = _ => false;

            _controller.DispatchClick(building);

            Assert.That(_clickedCount, Is.EqualTo(1),
                "a building whose own upgrade is not AwaitingCompletion must open normally");
        }

        [Test]
        public void HookOnlyPreemptsItsOwnTargetBuilding_OtherBuildingsStayClickable()
        {
            BuildingDefinition ready = BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack);
            BuildingDefinition other = BuildingCatalog.GetByBuildingType(BuildingTypes.Nursery);
            // Mirrors the real hook's shape: only the building matching the one ready-to-complete
            // operation is preempted - no global "any upgrade ready blocks every building" rule.
            BuildingInteractionController.InteractionPreemptionHook = clicked => ReferenceEquals(clicked, ready);

            _controller.DispatchClick(other);
            Assert.That(_clickedCount, Is.EqualTo(1), "a different building must still open normally");

            _controller.DispatchClick(ready);
            Assert.That(_clickedCount, Is.EqualTo(1), "the ready building's click must stay consumed, not add a second BuildingClicked");
        }

        [Test]
        public void RepeatedClicksOnReadyBuilding_EachConsumedExactlyOnce_HookInvokedOncePerClick()
        {
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack);
            int hookCalls = 0;
            BuildingInteractionController.InteractionPreemptionHook = _ => { hookCalls++; return true; };

            _controller.DispatchClick(building);
            _controller.DispatchClick(building);

            Assert.That(hookCalls, Is.EqualTo(2), "one hook invocation per real click - never batched/duplicated per click");
            Assert.That(_clickedCount, Is.EqualTo(0), "double click on a still-ready building must not double-open or double-fire BuildingClicked");
        }

        [Test]
        public void AfterCompletionHookStopsMatching_NextClickOnSameBuildingOpensNormally()
        {
            // Simulates "validated -> operation cleared -> ready badge disappears": the same
            // hook a real bootstrap installs re-evaluates server state fresh on every click, so
            // once nothing is AwaitingCompletion anymore, the very next click on that same
            // building must open it normally - no persistent interception.
            BuildingDefinition building = BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack);
            bool stillReady = true;
            BuildingInteractionController.InteractionPreemptionHook = clicked => stillReady;

            _controller.DispatchClick(building);
            Assert.That(_clickedCount, Is.EqualTo(0), "first click while ready must be consumed (validation)");

            stillReady = false;
            _controller.DispatchClick(building);
            Assert.That(_clickedCount, Is.EqualTo(1), "next click after completion must open the building normally");
        }

        // M049B-CL: RegisterCompletionPreemption/UnregisterCompletionPreemption generalize the
        // single InteractionPreemptionHook above so Construction's and Research's own completion
        // handlers can coexist as two independent registrants of the SAME hook, instead of
        // Research needing a second, competing click router.
        [Test]
        public void RegisterCompletionPreemption_TwoIndependentHandlersCoexist_EachOnlyPreemptsOwnBuilding()
        {
            BuildingDefinition constructionReady = BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack);
            BuildingDefinition researchReady = BuildingCatalog.GetByBuildingType(BuildingTypes.Research);
            Func<BuildingDefinition, bool> constructionHandler = clicked => ReferenceEquals(clicked, constructionReady);
            Func<BuildingDefinition, bool> researchHandler = clicked => ReferenceEquals(clicked, researchReady);

            try
            {
                BuildingInteractionController.RegisterCompletionPreemption(constructionHandler);
                BuildingInteractionController.RegisterCompletionPreemption(researchHandler);

                _controller.DispatchClick(constructionReady);
                Assert.That(_clickedCount, Is.EqualTo(0), "Construction's own handler must preempt its ready building");

                _controller.DispatchClick(researchReady);
                Assert.That(_clickedCount, Is.EqualTo(0), "Research's own handler must preempt its ready building too, via the same single hook");

                BuildingDefinition other = BuildingCatalog.GetByBuildingType(BuildingTypes.Warehouse);
                _controller.DispatchClick(other);
                Assert.That(_clickedCount, Is.EqualTo(1), "a building neither handler matches must still open normally");
            }
            finally
            {
                BuildingInteractionController.UnregisterCompletionPreemption(constructionHandler);
                BuildingInteractionController.UnregisterCompletionPreemption(researchHandler);
            }
        }

        [Test]
        public void RegisterCompletionPreemption_DuplicateRegistrationIgnored()
        {
            int calls = 0;
            Func<BuildingDefinition, bool> handler = _ => { calls++; return true; };
            try
            {
                BuildingInteractionController.RegisterCompletionPreemption(handler);
                BuildingInteractionController.RegisterCompletionPreemption(handler);

                _controller.DispatchClick(BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack));

                Assert.That(calls, Is.EqualTo(1), "registering the same handler twice must not evaluate it twice per click");
            }
            finally
            {
                BuildingInteractionController.UnregisterCompletionPreemption(handler);
            }
        }

        [Test]
        public void UnregisterCompletionPreemption_RemovesOnlyThatHandler_OthersKeepWorking()
        {
            BuildingDefinition a = BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack);
            BuildingDefinition b = BuildingCatalog.GetByBuildingType(BuildingTypes.Research);
            Func<BuildingDefinition, bool> handlerA = clicked => ReferenceEquals(clicked, a);
            Func<BuildingDefinition, bool> handlerB = clicked => ReferenceEquals(clicked, b);

            try
            {
                BuildingInteractionController.RegisterCompletionPreemption(handlerA);
                BuildingInteractionController.RegisterCompletionPreemption(handlerB);
                BuildingInteractionController.UnregisterCompletionPreemption(handlerA);

                _controller.DispatchClick(a);
                Assert.That(_clickedCount, Is.EqualTo(1), "unregistered handler must no longer preempt its building");

                _controller.DispatchClick(b);
                Assert.That(_clickedCount, Is.EqualTo(1), "the remaining registered handler must still preempt its own building");
            }
            finally
            {
                BuildingInteractionController.UnregisterCompletionPreemption(handlerB);
            }
        }

        [Test]
        public void UnregisterCompletionPreemption_LastHandlerRemoved_HookClearsAndClicksOpenNormally()
        {
            Func<BuildingDefinition, bool> handler = _ => true;
            BuildingInteractionController.RegisterCompletionPreemption(handler);
            BuildingInteractionController.UnregisterCompletionPreemption(handler);

            Assert.That(BuildingInteractionController.InteractionPreemptionHook, Is.Null,
                "removing the last registered handler must clear the shared hook, not leave an empty-but-installed router");

            _controller.DispatchClick(BuildingCatalog.GetByBuildingType(BuildingTypes.Barrack));
            Assert.That(_clickedCount, Is.EqualTo(1));
        }
    }
}
