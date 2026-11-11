#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Slice audio clips produced from a deterministic internal synthesizer (T-7.11).
    /// </summary>
    public sealed class SliceAudioClipSet
    {
        public AudioClip EngineLoop;
        public AudioClip TyreScreechLoop;
        public AudioClip Impact;
        public AudioClip CountdownBeep;
        public AudioClip CountdownGo;
        public AudioClip PauseBeep;
        public AudioClip ResumeBeep;
        public AudioClip FinishSting;
    }

    /// <summary>
    /// Generates the vertical slice's 16-bit mono WAV assets with no external audio library or
    /// licensed sample pack (T-7.11).
    /// </summary>
    /// <remarks>
    /// Loops are built from integer-cycle tones and a short crossfade at the noise boundary, so
    /// they repeat without a click. Every clip is generated from a fixed seed and normalized to
    /// the same peak, which keeps the mix predictable across rebuilds.
    /// </remarks>
    public static class SliceAudioKit
    {
        public const string AudioFolder = "Assets/Audio/Generated";

        private const int SampleRate = 44100;
        private const float TargetPeak = 0.72f;

        /// <summary>Creates or refreshes every slice clip and returns the set.</summary>
        public static SliceAudioClipSet EnsureClips()
        {
            EnsureFolder("Assets/Audio");
            EnsureFolder(AudioFolder);

            var set = new SliceAudioClipSet
            {
                EngineLoop = WriteClip("SliceEngineLoop", CreateEngineLoop(2f)),
                TyreScreechLoop = WriteClip("SliceTyreScreechLoop", CreateTyreScreechLoop(1.5f)),
                Impact = WriteClip("SliceImpact", CreateImpact()),
                CountdownBeep = WriteClip("SliceCountdownBeep", CreateBeep(880f, 0.14f, 7f)),
                CountdownGo = WriteClip("SliceCountdownGo", CreateGo()),
                PauseBeep = WriteClip("SlicePauseBeep", CreateSweep(560f, 420f, 0.13f)),
                ResumeBeep = WriteClip("SliceResumeBeep", CreateSweep(440f, 640f, 0.13f)),
                FinishSting = WriteClip("SliceFinishSting", CreateFinishSting())
            };

            AssetDatabase.SaveAssets();
            return set;
        }

        /// <summary>Loads the clip set from committed asset paths, or null when incomplete.</summary>
        public static SliceAudioClipSet LoadClips()
        {
            var set = new SliceAudioClipSet
            {
                EngineLoop = Load("SliceEngineLoop"),
                TyreScreechLoop = Load("SliceTyreScreechLoop"),
                Impact = Load("SliceImpact"),
                CountdownBeep = Load("SliceCountdownBeep"),
                CountdownGo = Load("SliceCountdownGo"),
                PauseBeep = Load("SlicePauseBeep"),
                ResumeBeep = Load("SliceResumeBeep"),
                FinishSting = Load("SliceFinishSting")
            };

            return set.EngineLoop != null && set.Impact != null ? set : null;
        }

        /// <summary>Project-relative path of a generated clip.</summary>
        public static string ClipPath(string name)
        {
            return AudioFolder + "/" + name + ".wav";
        }

        private static AudioClip Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(name));
        }

        private static AudioClip WriteClip(string name, float[] samples)
        {
            Normalize(samples, TargetPeak);

            var path = ClipPath(name);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                var dataLength = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataLength);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataLength);

                for (var i = 0; i < samples.Length; i++)
                {
                    var value = Mathf.Clamp(samples[i], -1f, 1f);
                    writer.Write((short)Mathf.RoundToInt(value * 32767f));
                }
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Audio import failed: " + path);
            }

            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static float[] CreateEngineLoop(float durationSeconds)
        {
            var count = Mathf.RoundToInt(SampleRate * durationSeconds);
            var samples = new float[count];
            var random = new System.Random(71101);

            // Base frequencies are integer multiples of 1/duration, so the tonal bed is
            // loop-perfect without a crossfade.
            const float baseHz = 55f;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                var fundamental = Mathf.Sin(2f * Mathf.PI * baseHz * t);
                var second = Mathf.Sin(2f * Mathf.PI * baseHz * 2f * t);
                var third = Mathf.Sin(2f * Mathf.PI * baseHz * 3f * t);
                var fourth = Mathf.Sin(2f * Mathf.PI * baseHz * 4f * t);

                var modulation = 0.76f + (0.24f * Mathf.Sin(2f * Mathf.PI * 8f * t));
                var growl = (float)random.NextDouble() - 0.5f;

                samples[i] =
                    ((fundamental * 0.42f) + (second * 0.26f) + (third * 0.13f) + (fourth * 0.07f)) * modulation +
                    (growl * 0.03f);
            }

            CrossfadeLoopBoundary(samples, Mathf.RoundToInt(SampleRate * 0.02f));
            return samples;
        }

        private static float[] CreateTyreScreechLoop(float durationSeconds)
        {
            var count = Mathf.RoundToInt(SampleRate * durationSeconds);
            var samples = new float[count];
            var random = new System.Random(71102);
            var previous = 0f;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;

                // One-pole smoothing of white noise gives a soft "shhh"; the two tones make the
                // scrub audible over the engine.
                var noise = ((float)random.NextDouble() * 2f) - 1f;
                previous += (noise - previous) * 0.35f;

                var tremolo = 0.62f + (0.38f * Mathf.Sin(2f * Mathf.PI * 7f * t));
                var toneA = Mathf.Sin(2f * Mathf.PI * 1050f * t) * 0.16f;
                var toneB = Mathf.Sin(2f * Mathf.PI * 1680f * t) * 0.09f;

                samples[i] = ((previous * 0.9f) + toneA + toneB) * tremolo;
            }

            CrossfadeLoopBoundary(samples, Mathf.RoundToInt(SampleRate * 0.04f));
            return samples;
        }

        private static float[] CreateImpact()
        {
            var duration = 0.36f;
            var count = Mathf.RoundToInt(SampleRate * duration);
            var samples = new float[count];
            var random = new System.Random(71103);
            var previous = 0f;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var noise = ((float)random.NextDouble() * 2f) - 1f;
                previous += (noise - previous) * 0.55f;

                var thumpHz = Mathf.Lerp(110f, 46f, Mathf.Clamp01(t / duration));
                var thump = Mathf.Sin(2f * Mathf.PI * thumpHz * t) * Mathf.Exp(-t * 16f);
                var crack = previous * Mathf.Exp(-t * 26f);

                samples[i] = (thump * 0.85f) + (crack * 0.9f);
            }

            return samples;
        }

        private static float[] CreateBeep(float frequency, float durationSeconds, float decay)
        {
            var count = Mathf.RoundToInt(SampleRate * durationSeconds);
            var samples = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var envelope = Mathf.Min(1f, t * 400f) * Mathf.Exp(-t * decay);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
            }

            return samples;
        }

        private static float[] CreateSweep(float startHz, float endHz, float durationSeconds)
        {
            var count = Mathf.RoundToInt(SampleRate * durationSeconds);
            var samples = new float[count];
            var phase = 0f;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var frequency = Mathf.Lerp(startHz, endHz, Mathf.Clamp01(t / durationSeconds));
                phase += 2f * Mathf.PI * frequency / SampleRate;
                var envelope = Mathf.Min(1f, t * 300f) * Mathf.Exp(-t * 12f);
                samples[i] = Mathf.Sin(phase) * envelope;
            }

            return samples;
        }

        private static float[] CreateGo()
        {
            var duration = 0.4f;
            var count = Mathf.RoundToInt(SampleRate * duration);
            var samples = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var envelope = Mathf.Min(1f, t * 300f) * Mathf.Exp(-t * 6f);
                samples[i] =
                    (Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.6f) +
                    (Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.4f);
                samples[i] *= envelope;
            }

            return samples;
        }

        private static float[] CreateFinishSting()
        {
            var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.5f };
            var noteSeconds = 0.2f;
            var duration = noteSeconds * notes.Length;
            var count = Mathf.RoundToInt(SampleRate * duration);
            var samples = new float[count];

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var value = 0f;

                for (var n = 0; n < notes.Length; n++)
                {
                    var start = n * noteSeconds;
                    if (t < start)
                    {
                        continue;
                    }

                    var local = t - start;
                    var envelope = Mathf.Min(1f, local * 250f) * Mathf.Exp(-local * 7f);
                    value += Mathf.Sin(2f * Mathf.PI * notes[n] * local) * envelope * 0.32f;
                }

                samples[i] = value;
            }

            return samples;
        }

        private static void CrossfadeLoopBoundary(float[] samples, int fadeSamples)
        {
            var fade = Mathf.Clamp(fadeSamples, 1, samples.Length / 4);
            for (var i = 0; i < fade; i++)
            {
                var t = i / (float)fade;
                var tailIndex = samples.Length - fade + i;
                samples[tailIndex] = (samples[tailIndex] * (1f - t)) + (samples[i] * t);
            }
        }

        private static void Normalize(float[] samples, float peak)
        {
            var max = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var magnitude = Mathf.Abs(samples[i]);
                if (magnitude > max)
                {
                    max = magnitude;
                }
            }

            if (max <= 0.00001f)
            {
                return;
            }

            var scale = peak / max;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
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
