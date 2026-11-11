using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Data;
using VelocityRush.EditorTools;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Zero-allocation guard for Garage roster navigation (T-8.10).
    /// </summary>
    /// <remarks>
    /// The test drives the real <see cref="GarageView"/> + <see cref="GaragePresenter"/> pair.
    /// Construction (tree build, one-time string cache) is excluded; the measured loop is the
    /// exact per-selection path used by the scene adapter.
    /// </remarks>
    public sealed class GarageViewAllocationTests
    {
        [Test]
        public void RepeatedSelection_AfterWarmup_AllocatesZeroBytes()
        {
            var model = new GarageCatalogModel(LoadRoster());
            var root = new VisualElement();
            var view = new GarageView(root, model);
            var presenter = new GaragePresenter(model, view);

            // Warm-up covers JIT, style resolution, and any one-time panel/layout setup.
            for (var i = 0; i < 240; i++)
            {
                presenter.Next();
                if ((i & 1) == 0)
                {
                    presenter.Confirm();
                }
            }

            var before = System.GC.GetAllocatedBytesForCurrentThread();
            const int selections = 600;
            for (var i = 0; i < selections; i++)
            {
                if ((i & 1) == 0)
                {
                    presenter.Next();
                }
                else
                {
                    presenter.Previous();
                }
            }

            var allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L),
                "Garage selection must allocate 0 B after warm-up; measured " + allocated +
                " B over " + selections + " selections.");
        }

        [Test]
        public void View_ShowsOneDetailPanelAtATime_AndConfirmMatchesSelection()
        {
            var definitions = LoadRoster();
            var model = new GarageCatalogModel(definitions);
            var root = new VisualElement();
            var view = new GarageView(root, model);
            var presenter = new GaragePresenter(model, view);

            Assert.That(view.ShownIndex, Is.EqualTo(0));
            Assert.That(FindPanel(root, definitions[0].VehicleId).style.display.value,
                Is.EqualTo(DisplayStyle.Flex));

            presenter.Next();
            presenter.Next();
            Assert.That(view.ShownIndex, Is.EqualTo(2));
            Assert.That(FindPanel(root, definitions[2].VehicleId).style.display.value,
                Is.EqualTo(DisplayStyle.Flex));
            Assert.That(FindPanel(root, definitions[0].VehicleId).style.display.value,
                Is.EqualTo(DisplayStyle.None));

            presenter.Confirm();
            Assert.That(presenter.ConfirmedIndex, Is.EqualTo(2));
            var confirmation = root.Q<Label>("GarageConfirmation");
            Assert.That(confirmation, Is.Not.Null, "Garage view must expose the confirmation label.");
            Assert.That(confirmation.text, Does.Contain(definitions[2].DisplayNameEn.ToUpperInvariant()));
        }

        private static VisualElement FindPanel(VisualElement root, string vehicleId)
        {
            var panel = root.Q("GarageDetail_" + vehicleId);
            Assert.That(panel, Is.Not.Null, "Missing detail panel for " + vehicleId);
            return panel;
        }

        private static List<VehicleDefinition> LoadRoster()
        {
            var guids = AssetDatabase.FindAssets("t:VehicleDefinition",
                new[] { VehicleRosterBuilder.RosterFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(System.StringComparer.Ordinal);

            var definitions = new List<VehicleDefinition>(paths.Count);
            for (var i = 0; i < paths.Count; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(paths[i]);
                Assert.That(definition, Is.Not.Null, "Could not load " + paths[i]);
                definitions.Add(definition);
            }

            return definitions;
        }
    }
}
