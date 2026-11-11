#if UNITY_EDITOR
using System;
using UnityEngine;
using VelocityRush.Gameplay;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Authors the budgeted slice particle systems (tyre smoke, ground dust, impact sparks) under
    /// an instantiated vehicle and wires <see cref="VehicleVfxController"/> (T-7.9).
    /// </summary>
    /// <remarks>
    /// Automatic emission is disabled on every system; <see cref="VehicleVfxController"/> owns all
    /// emission so the per-tick caps are enforceable. Max-particle counts are deliberately small
    /// (30–60) to bound overdraw on integrated GPUs and WebGL.
    /// </remarks>
    public static class SliceVfxKit
    {
        /// <summary>Builds the three systems and returns the configured controller.</summary>
        /// <param name="vehicleRoot">Instantiated vehicle root.</param>
        /// <param name="materials">Shared slice material set (particle materials).</param>
        /// <param name="smokeAnchors">Rear wheel visuals used as smoke emission points.</param>
        public static VehicleVfxController Build(
            GameObject vehicleRoot,
            SliceMaterialSet materials,
            Transform[] smokeAnchors)
        {
            if (vehicleRoot == null)
            {
                throw new ArgumentNullException(nameof(vehicleRoot));
            }

            if (materials == null)
            {
                throw new ArgumentNullException(nameof(materials));
            }

            var smoke = CreateSystem(
                vehicleRoot.transform,
                "Vfx Tyre Smoke",
                materials.Smoke,
                maxParticles: 40,
                startSpeed: 0.5f,
                minSize: 0.45f,
                maxSize: 1.15f,
                startColor: new Color(0.82f, 0.80f, 0.77f, 0.42f),
                gravity: -0.02f);

            var dust = CreateSystem(
                vehicleRoot.transform,
                "Vfx Ground Dust",
                materials.Dust,
                maxParticles: 60,
                startSpeed: 0.9f,
                minSize: 0.9f,
                maxSize: 1.9f,
                startColor: new Color(0.85f, 0.72f, 0.5f, 0.5f),
                gravity: -0.05f);

            var sparks = CreateSystem(
                vehicleRoot.transform,
                "Vfx Impact Sparks",
                materials.Spark,
                maxParticles: 30,
                startSpeed: 5.5f,
                minSize: 0.05f,
                maxSize: 0.1f,
                startColor: new Color(1f, 0.75f, 0.3f, 1f),
                gravity: 1.1f);

            var controller = vehicleRoot.AddComponent<VehicleVfxController>();
            var serialized = new UnityEditor.SerializedObject(controller);
            SetReference(serialized, "telemetrySource", vehicleRoot.GetComponent<VelocityRush.Gameplay.VehicleController>());
            SetReference(serialized, "smoke", smoke);
            SetReference(serialized, "dust", dust);
            SetReference(serialized, "sparks", sparks);
            SetReference(serialized, "sparkOrigin", null);

            var anchors = serialized.FindProperty("smokeAnchors");
            if (anchors != null)
            {
                var usable = smokeAnchors ?? Array.Empty<Transform>();
                anchors.arraySize = usable.Length;
                for (var i = 0; i < usable.Length; i++)
                {
                    anchors.GetArrayElementAtIndex(i).objectReferenceValue = usable[i];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static ParticleSystem CreateSystem(
            Transform parent,
            string name,
            Material material,
            int maxParticles,
            float startSpeed,
            float minSize,
            float maxSize,
            Color startColor,
            float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var system = go.AddComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startSpeed = startSpeed;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = startColor;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = system.emission;
            emission.enabled = false;

            var shape = system.shape;
            shape.enabled = false;

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateFadeGradient());

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = 0f;

            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return system;
        }

        private static Gradient CreateFadeGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static void SetReference(UnityEditor.SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyName + "' on " + serialized.targetObject.GetType().Name);
            }

            property.objectReferenceValue = value;
        }
    }
}
#endif
