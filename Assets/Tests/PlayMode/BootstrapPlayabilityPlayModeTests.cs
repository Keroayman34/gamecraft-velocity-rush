using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.Economy;
using VelocityRush.Gameplay.Traffic;

namespace VelocityRush.Tests.PlayMode
{
    [TestFixture]
    public sealed class BootstrapPlayabilityPlayModeTests
    {
        private GameObject _bootstrapGo;
        private GameCompositionRoot _compRoot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _bootstrapGo = new GameObject("Bootstrap");
            _compRoot = _bootstrapGo.AddComponent<GameCompositionRoot>();
            _compRoot.EnsureDependencies();
            _compRoot.Initialize(new InMemorySaveStore());
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_bootstrapGo != null)
            {
                Object.Destroy(_bootstrapGo);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootstrapBoot_AssignsPanelSettings_AndFallbackCameraRenders()
        {
            var uiDoc = _bootstrapGo.GetComponent<UIDocument>();
            Assert.That(uiDoc, Is.Not.Null);
            Assert.That(uiDoc.panelSettings, Is.Not.Null, "UIDocument.panelSettings must not be null.");
            Assert.That(_compRoot.ActivePanelSettings, Is.Not.Null);

            Assert.That(_compRoot.FallbackCamera, Is.Not.Null, "Fallback camera must exist.");
            Assert.That(_compRoot.FallbackCamera.enabled, Is.True, "Fallback camera must be enabled in menus.");
            Assert.That(_compRoot.FallbackCamera.depth, Is.EqualTo(-10));
            Assert.That(_compRoot.FallbackCamera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));

            // Verify MainMenu screen is active with styled buttons
            var ui = _bootstrapGo.GetComponent<CommercialUiController>();
            Assert.That(ui, Is.Not.Null);
            Assert.That(ui.MainMenuRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(ui.MainMenuRoot.childCount, Is.GreaterThan(0));

            var playButton = ui.MainMenuRoot.Q<Button>("main-menu-play");
            Assert.That(playButton, Is.Not.Null);
            Assert.That(playButton.text, Is.Not.Null.Or.Empty);

            var garageButton = ui.MainMenuRoot.Q<Button>("main-menu-garage");
            Assert.That(garageButton, Is.Not.Null);

            yield return null;
        }

        [UnityTest]
        public IEnumerator FallbackCamera_DisablesInRaceWithActiveCamera_AndReEnablesOnExit()
        {
            // Spawn a simulated race scene camera tagged MainCamera
            var raceCamGo = new GameObject("RaceMainCamera");
            raceCamGo.tag = "MainCamera";
            var raceCam = raceCamGo.AddComponent<Camera>();
            raceCam.enabled = true;

            // Transition to Race state
            _compRoot.GameFlow.TryTransitionTo(AppState.Race);
            _compRoot.FindActiveRaceComponents();
            yield return null;

            Assert.That(_compRoot.FallbackCamera.enabled, Is.False, "Fallback camera should disable when race camera is active.");

            // Return to MainMenu
            _compRoot.GameFlow.TryTransitionTo(AppState.MainMenu);
            yield return null;

            Assert.That(_compRoot.FallbackCamera.enabled, Is.True, "Fallback camera should re-enable on returning to menus.");

            Object.Destroy(raceCamGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RaceSceneAutoSpawn_WhenPhase12ComponentsMissing_AutoSpawnsHighwaySystems()
        {
            // Create a track layout and vehicle
            var playerGo = new GameObject("PlayerVehicle");
            playerGo.tag = "Player";
            var playerBody = playerGo.AddComponent<Rigidbody>();
            var player = playerGo.AddComponent<VehicleController>();

            var layout = ScriptableObject.CreateInstance<TrackLayout>();
            var segs = new[]
            {
                new TrackSegment(TrackSegmentType.Straight, 100f, 0f, 15f),
                new TrackSegment(TrackSegmentType.Straight, 100f, 0f, 15f)
            };
            layout.ConfigureForAuthoring("autospawn-test", "AutoSpawn Test", 30f, segs);

            // Attach RaceGridController with layout
            var gridGo = new GameObject("RaceGrid");
            var grid = gridGo.AddComponent<RaceGridController>();
            grid.ConfigureForAuthoring(null, player, null, layout, EnvironmentLayoutVariant.Signature, 20f, 0.5f);

            // Trigger FindActiveRaceComponents in Race state
            _compRoot.GameFlow.TryTransitionTo(AppState.Race);
            _compRoot.FindActiveRaceComponents();
            yield return null;

            Assert.That(_compRoot.ActiveRoadCoinManager, Is.Not.Null, "RoadCoinManager should be auto-spawned.");
            Assert.That(Object.FindAnyObjectByType<TrafficLaneManager>(), Is.Not.Null, "TrafficLaneManager should be auto-spawned.");
            Assert.That(Object.FindAnyObjectByType<NearMissDetector>(), Is.Not.Null, "NearMissDetector should be auto-spawned.");

            // Cleanup
            Object.Destroy(gridGo);
            Object.Destroy(playerGo);
            Object.Destroy(layout);
            yield return null;
        }
    }
}
