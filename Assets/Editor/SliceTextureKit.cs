#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Deterministic production material set for the Desert Circuit slice (T-7.6).
    /// </summary>
    /// <remarks>
    /// Every material is internal-only art: textures are generated procedurally from fixed seeds
    /// and stored as project PNG assets, and materials are built from the Built-in Render
    /// Pipeline's Standard shader (D-032). No external asset, package, or texture download is
    /// involved.
    /// </remarks>
    public sealed class SliceMaterialSet
    {
        public Material Asphalt;
        public Material TrackLine;
        public Material Sand;
        public Material Rock;
        public Material Metal;
        public Material Hazard;
        public Material HeroPaint;
        public Material HeroDark;
        public Material Rubber;
        public Material Chrome;
        public Material Headlight;
        public Material TailLight;
        public Material Smoke;
        public Material Dust;
        public Material Spark;
    }

    /// <summary>
    /// Creates the slice's procedural textures and materials (T-7.6/T-7.7/T-7.9).
    /// </summary>
    /// <remarks>
    /// Regeneration is idempotent: an existing asset is updated in place rather than deleted, so
    /// scene references survive a rebuild. Textures are authored at low resolution with repeat
    /// wrapping; the visual read comes from tiling, baked GI, and fog rather than texture detail,
    /// which keeps the WebGL memory and bandwidth budget small.
    /// </remarks>
    public static class SliceTextureKit
    {
        public const string GeneratedFolder = "Assets/Generated/HandlingCourse";
        public const string TextureFolder = GeneratedFolder + "/Textures";

        private const int AsphaltSize = 512;
        private const int SandSize = 256;
        private const int MetalSize = 256;
        private const int HazardSize = 128;
        private const int SoftDotSize = 64;

        private const int AsphaltSeed = 71201;
        private const int SandSeed = 71202;
        private const int MetalSeed = 71203;
        private const int HazardSeed = 71204;

        /// <summary>Creates or updates every slice texture and material and returns the set.</summary>
        public static SliceMaterialSet EnsureMaterialSet()
        {
            EnsureFolder("Assets/Generated");
            EnsureFolder(GeneratedFolder);
            EnsureFolder(TextureFolder);

            var asphaltTexture = EnsureTexture("SliceAsphalt", AsphaltSize, CreateAsphalt, repeat: true, aniso: 4, sRGB: true);
            var sandTexture = EnsureTexture("SliceSand", SandSize, CreateSand, repeat: true, aniso: 4, sRGB: true);
            var metalTexture = EnsureTexture("SliceMetal", MetalSize, CreateMetal, repeat: true, aniso: 2, sRGB: true);
            var hazardTexture = EnsureTexture("SliceHazard", HazardSize, CreateHazard, repeat: true, aniso: 2, sRGB: true);

            var set = new SliceMaterialSet
            {
                Asphalt = EnsureMaterial(
                    "SliceAsphalt",
                    asphaltTexture,
                    metallic: 0f,
                    smoothness: 0.16f,
                    tiling: new Vector2(2f, 0.125f)),

                TrackLine = EnsureMaterial(
                    "SliceTrackLine",
                    null,
                    metallic: 0f,
                    smoothness: 0.3f,
                    color: new Color(0.93f, 0.92f, 0.88f)),

                Sand = EnsureMaterial(
                    "SliceSand",
                    sandTexture,
                    metallic: 0f,
                    smoothness: 0.06f,
                    color: new Color(1f, 1f, 1f),
                    tiling: new Vector2(60f, 60f)),

                Rock = EnsureMaterial(
                    "SliceRock",
                    sandTexture,
                    metallic: 0f,
                    smoothness: 0.08f,
                    color: new Color(0.88f, 0.82f, 0.72f),
                    tiling: new Vector2(2f, 2f)),

                Metal = EnsureMaterial(
                    "SliceMetal",
                    metalTexture,
                    metallic: 0.65f,
                    smoothness: 0.42f),

                Hazard = EnsureMaterial(
                    "SliceHazard",
                    hazardTexture,
                    metallic: 0.1f,
                    smoothness: 0.28f),

                HeroPaint = EnsureMaterial(
                    "SliceHeroPaint",
                    null,
                    metallic: 0.25f,
                    smoothness: 0.62f,
                    color: new Color(0.86f, 0.16f, 0.09f)),

                HeroDark = EnsureMaterial(
                    "SliceHeroDark",
                    null,
                    metallic: 0.35f,
                    smoothness: 0.35f,
                    color: new Color(0.07f, 0.07f, 0.08f)),

                Rubber = EnsureMaterial(
                    "SliceRubber",
                    null,
                    metallic: 0f,
                    smoothness: 0.12f,
                    color: new Color(0.05f, 0.05f, 0.055f)),

                Chrome = EnsureMaterial(
                    "SliceChrome",
                    null,
                    metallic: 0.9f,
                    smoothness: 0.8f,
                    color: new Color(0.78f, 0.8f, 0.84f)),

                Headlight = EnsureEmissiveMaterial(
                    "SliceHeadlight",
                    new Color(0.9f, 0.94f, 1f),
                    new Color(1.6f, 1.75f, 2f)),

                TailLight = EnsureEmissiveMaterial(
                    "SliceTailLight",
                    new Color(0.55f, 0.05f, 0.04f),
                    new Color(2.4f, 0.2f, 0.12f))
            };

            var softDot = EnsureTexture("SliceSoftDot", SoftDotSize, CreateSoftDot, repeat: false, aniso: 1, sRGB: false);
            set.Smoke = EnsureParticleMaterial("SliceVfxSmoke", softDot, new Color(0.72f, 0.70f, 0.66f, 0.42f), additive: false);
            set.Dust = EnsureParticleMaterial("SliceVfxDust", softDot, new Color(0.82f, 0.68f, 0.46f, 0.5f), additive: false);
            set.Spark = EnsureParticleMaterial("SliceVfxSpark", softDot, new Color(1f, 0.72f, 0.25f, 1f), additive: true);

            AssetDatabase.SaveAssets();
            return set;
        }

        /// <summary>Loads the material set from its committed asset paths, or null when incomplete.</summary>
        public static SliceMaterialSet LoadMaterialSet()
        {
            var set = new SliceMaterialSet
            {
                Asphalt = Load<Material>("SliceAsphalt"),
                TrackLine = Load<Material>("SliceTrackLine"),
                Sand = Load<Material>("SliceSand"),
                Rock = Load<Material>("SliceRock"),
                Metal = Load<Material>("SliceMetal"),
                Hazard = Load<Material>("SliceHazard"),
                HeroPaint = Load<Material>("SliceHeroPaint"),
                HeroDark = Load<Material>("SliceHeroDark"),
                Rubber = Load<Material>("SliceRubber"),
                Chrome = Load<Material>("SliceChrome"),
                Headlight = Load<Material>("SliceHeadlight"),
                TailLight = Load<Material>("SliceTailLight"),
                Smoke = Load<Material>("SliceVfxSmoke"),
                Dust = Load<Material>("SliceVfxDust"),
                Spark = Load<Material>("SliceVfxSpark")
            };

            return set.Asphalt != null && set.Sand != null && set.Metal != null ? set : null;
        }

        /// <summary>Project-relative path of a generated texture.</summary>
        public static string TexturePath(string name)
        {
            return TextureFolder + "/" + name + ".png";
        }

        /// <summary>Project-relative path of a generated material.</summary>
        public static string MaterialPath(string name)
        {
            return GeneratedFolder + "/" + name + ".mat";
        }

        private static T Load<T>(string name) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(MaterialPath(name));
        }

        private static Material EnsureMaterial(
            string name,
            Texture texture,
            float metallic,
            float smoothness,
            Color? color = null,
            Vector2? tiling = null)
        {
            var path = MaterialPath(name);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                if (shader == null)
                {
                    throw new InvalidOperationException("No usable Built-in shader found for " + name + ".");
                }

                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.mainTexture = texture;
            material.mainTextureScale = tiling ?? Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            material.color = color ?? Color.white;
            material.SetFloat("_Mode", 0f);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureEmissiveMaterial(string name, Color albedo, Color emission)
        {
            var material = EnsureMaterial(name, null, metallic: 0.1f, smoothness: 0.5f, color: albedo);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureParticleMaterial(string name, Texture texture, Color tint, bool additive)
        {
            var path = MaterialPath(name);
            var shader = additive
                ? Shader.Find("Particles/Standard Unlit")
                : Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.mainTexture = texture;
            material.SetColor("_Color", tint);
            material.SetColor("_TintColor", tint);
            material.SetFloat("_Mode", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D EnsureTexture(
            string name,
            int size,
            Func<int, Color[]> painter,
            bool repeat,
            int aniso,
            bool sRGB)
        {
            var path = TexturePath(name);
            var pixels = painter(size);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, !sRGB);
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Texture import failed: " + path);
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = sRGB;
            importer.alphaIsTransparency = !repeat;
            importer.mipmapEnabled = true;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = aniso;
            importer.maxTextureSize = Mathf.Max(64, Mathf.NextPowerOfTwo(size));
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Color[] CreateAsphalt(int size)
        {
            var pixels = new Color[size * size];
            var random = new System.Random(AsphaltSeed);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)size;
                    var v = y / (float)size;

                    var broad = TileableNoise(u * 6f, v * 6f, 6, AsphaltSeed);
                    var medium = TileableNoise(u * 24f, v * 24f, 24, AsphaltSeed + 1);
                    var fine = TileableNoise(u * 96f, v * 96f, 96, AsphaltSeed + 2);

                    var value = 0.17f + (broad * 0.055f) + (medium * 0.035f) + (fine * 0.02f);

                    // Aggregate speckles and occasional tar seams break up the flat value.
                    if (random.NextDouble() < 0.035)
                    {
                        value += 0.09f;
                    }

                    var seam = Mathf.Abs(Mathf.Sin((u * 3f + (medium * 0.5f)) * Mathf.PI));
                    if (seam < 0.012f)
                    {
                        value -= 0.03f;
                    }

                    pixels[(y * size) + x] = new Color(value, value, value * 1.02f);
                }
            }

            return pixels;
        }

        private static Color[] CreateSand(int size)
        {
            var pixels = new Color[size * size];
            var random = new System.Random(SandSeed);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)size;
                    var v = y / (float)size;

                    var dunes = TileableNoise(u * 4f, v * 2f, 4, SandSeed);
                    var ripples = TileableNoise(u * 20f, v * 8f, 20, SandSeed + 1);
                    var grain = (float)random.NextDouble();

                    var shade = 0.74f + (dunes * 0.16f) + (ripples * 0.06f) + ((grain - 0.5f) * 0.05f);
                    pixels[(y * size) + x] = new Color(
                        Mathf.Clamp01(shade * 1.02f),
                        Mathf.Clamp01(shade * 0.87f),
                        Mathf.Clamp01(shade * 0.66f));
                }
            }

            return pixels;
        }

        private static Color[] CreateMetal(int size)
        {
            var pixels = new Color[size * size];
            var random = new System.Random(MetalSeed);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)size;
                    var v = y / (float)size;

                    var brushed = TileableNoise(u * 128f, v * 3f, 128, MetalSeed);
                    var grime = TileableNoise(u * 8f, v * 8f, 8, MetalSeed + 1);
                    var shade = 0.52f + (brushed * 0.14f) + (grime * 0.08f) + ((float)random.NextDouble() - 0.5f) * 0.03f;

                    // Panel seams every quarter of the tile.
                    var panel = Mathf.Min(
                        Mathf.Min(Mathf.Abs(u - 0.25f), Mathf.Abs(u - 0.5f)),
                        Mathf.Min(Mathf.Abs(u - 0.75f), Mathf.Abs(v - 0.5f)));
                    if (panel < 0.006f)
                    {
                        shade *= 0.68f;
                    }

                    pixels[(y * size) + x] = new Color(shade * 0.93f, shade * 0.96f, shade);
                }
            }

            return pixels;
        }

        private static Color[] CreateHazard(int size)
        {
            var pixels = new Color[size * size];
            var random = new System.Random(HazardSeed);
            var yellow = new Color(0.95f, 0.78f, 0.13f);
            var dark = new Color(0.09f, 0.08f, 0.07f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var stripe = (x + y) % 64;
                    var baseColor = stripe < 32 ? yellow : dark;
                    var grime = 1f + (((float)random.NextDouble() - 0.5f) * 0.16f);

                    pixels[(y * size) + x] = new Color(
                        Mathf.Clamp01(baseColor.r * grime),
                        Mathf.Clamp01(baseColor.g * grime),
                        Mathf.Clamp01(baseColor.b * grime));
                }
            }

            return pixels;
        }

        private static Color[] CreateSoftDot(int size)
        {
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var alpha = Mathf.Clamp01(1f - distance);
                    alpha *= alpha;
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            return pixels;
        }

        private static float TileableNoise(float x, float y, int period, int seed)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var tx = x - x0;
            var ty = y - y0;
            tx = tx * tx * (3f - (2f * tx));
            ty = ty * ty * (3f - (2f * ty));

            var a = Hash(x0, y0, period, seed);
            var b = Hash(x0 + 1, y0, period, seed);
            var c = Hash(x0, y0 + 1, period, seed);
            var d = Hash(x0 + 1, y0 + 1, period, seed);

            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Hash(int x, int y, int period, int seed)
        {
            var wrappedX = ((x % period) + period) % period;
            var wrappedY = ((y % period) + period) % period;

            unchecked
            {
                var hash = (uint)(wrappedX * 374761393) + (uint)(wrappedY * 668265263) + (uint)seed;
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0xFFFFFFu) / 16777215f;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = path.Substring(0, path.LastIndexOf('/'));
            var name = path.Substring(path.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
