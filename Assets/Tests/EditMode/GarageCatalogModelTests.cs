using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.EditorTools;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Stage-4 Garage model coverage (T-8.9): roster projection, wrap-around browsing, explicit
    /// selection, and confirmation state.
    /// </summary>
    public sealed class GarageCatalogModelTests
    {
        [Test]
        public void Catalog_ProjectsTheFullRosterInDeterministicOrder()
        {
            var definitions = LoadRoster();
            var model = new GarageCatalogModel(definitions);

            Assert.That(model.Count, Is.EqualTo(VehicleRosterRules.RequiredVehicleCount));
            Assert.That(model.SelectedIndex, Is.EqualTo(0));
            Assert.That(model.ConfirmedIndex, Is.EqualTo(-1));

            for (var i = 0; i < definitions.Count; i++)
            {
                var summary = model[i];
                Assert.That(summary.VehicleId, Is.EqualTo(definitions[i].VehicleId));
                Assert.That(summary.Class, Is.EqualTo(definitions[i].Class));
                Assert.That(summary.Mass, Is.EqualTo(definitions[i].PhysicsProfile.Mass).Within(0.0001f));
                Assert.That(summary.MotorTorque,
                    Is.EqualTo(definitions[i].PhysicsProfile.MotorTorque).Within(0.0001f));
            }
        }

        [Test]
        public void NextAndPrevious_WrapAroundWithoutLosingTheIndex()
        {
            var model = new GarageCatalogModel(LoadRoster());

            for (var i = 1; i < model.Count; i++)
            {
                Assert.That(model.Next(), Is.True);
                Assert.That(model.SelectedIndex, Is.EqualTo(i));
            }

            Assert.That(model.Next(), Is.True);
            Assert.That(model.SelectedIndex, Is.EqualTo(0));

            Assert.That(model.Previous(), Is.True);
            Assert.That(model.SelectedIndex, Is.EqualTo(model.Count - 1));
        }

        [Test]
        public void Select_RejectsInvalidAndUnchangedIndices_AndConfirmRecordsCurrent()
        {
            var model = new GarageCatalogModel(LoadRoster());

            Assert.That(model.Select(0), Is.False);
            Assert.That(model.Select(-1), Is.False);
            Assert.That(model.Select(model.Count), Is.False);

            Assert.That(model.Select(4), Is.True);
            Assert.That(model.SelectedIndex, Is.EqualTo(4));
            Assert.That(model.ConfirmedIndex, Is.EqualTo(-1));

            Assert.That(model.Confirm(), Is.EqualTo(4));
            Assert.That(model.ConfirmedIndex, Is.EqualTo(4));
        }

        [Test]
        public void Catalog_RequiresANonEmptyRoster()
        {
            Assert.Throws<System.ArgumentNullException>(() => new GarageCatalogModel(null));
            Assert.Throws<System.ArgumentException>(
                () => new GarageCatalogModel(new List<VehicleDefinition>()));
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
