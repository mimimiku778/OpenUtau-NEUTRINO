using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.Format;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Headless {
    public static class HeadlessRenderer {
        private static readonly string[] KnownRenderers = new[] {
            Renderers.CLASSIC,
            Renderers.WORLDLINE_R,
            Renderers.WORLDLINE_R2,
            Renderers.ENUNU,
            Renderers.VOGEN,
            Renderers.DIFFSINGER,
            Renderers.VOICEVOX,
        };

        public static async Task RenderOneAsync(
            RenderJob job,
            HeadlessOpenUtauHost host,
            CancellationToken cancellationToken = default) {
            if (job == null) {
                throw new ArgumentNullException(nameof(job));
            }
            var inputPath = RequireInputPath(job.InputPath);
            var outputPath = RequireOutputPath(job.OutputPath);
            EnsureOutputWritable(outputPath);
            // 再生管理を先に作り終えておく。音素化の裏スレッドが初めて作ると、購読の登録だけ済んだ
            // 作りかけの所へ下の LoadProjectNotification が届き、空のメトロノームを止めに行って落ちる。
            _ = PlaybackManager.Inst;

            var project = Formats.ReadProject(new[] { inputPath });
            if (project == null) {
                throw new HeadlessRenderException($"Failed to load project: {inputPath}");
            }
            DocManager.Inst.ExecuteCmd(new LoadProjectNotification(project));
            host.ClearErrors();

            ApplyOverrides(project, job);
            // 画面なしでは裏の組み立て役を止め、フレーズをその場で組み立てさせる
            // (組み立て役が残ると、検査の時点で renderPhrases がまだ空のことがある)
            Pipeline.PhraseSourceBuilder.Current?.Dispose();
            project.ValidateFull();
            await DocManager.Inst.PhonemizerRunner.WaitForIdleAsync();
            cancellationToken.ThrowIfCancellationRequested();
            EnsureProjectReady(project);

            host.ClearErrors();
            await PlaybackManager.Inst.RenderMixdown(project, outputPath);
            var renderErrors = host.TakeErrors();
            if (renderErrors.Length > 0) {
                throw new HeadlessRenderException(string.Join(Environment.NewLine, renderErrors));
            }
            if (!File.Exists(outputPath)) {
                throw new HeadlessRenderException($"Render failed; output was not written: {outputPath}");
            }
            if (new FileInfo(outputPath).Length <= 46) {
                throw new HeadlessRenderException($"Render failed; output contains no audio data: {outputPath}");
            }
            DumpDirectPitch(project);
        }

        /// <summary>
        /// OPENUTAU_DUMP_DIRECT_PITCH=<path> の時だけ、NEUTRINO direct の句ごとに画面へ描く線(GetDirectPitch)を JSON Lines で書く。
        /// 画面を開かずに、描く線が実際に鳴った F0 と同じかを確かめるため。
        /// </summary>
        static void DumpDirectPitch(UProject project) {
            var path = Environment.GetEnvironmentVariable("OPENUTAU_DUMP_DIRECT_PITCH");
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }
            using var w = new StreamWriter(path);
            foreach (var part in project.parts.OfType<UVoicePart>()) {
                foreach (var phrase in part.renderPhrases) {
                    var c = OpenUtau.Core.Neutrino.NeutrinoRenderer.GetDirectPitch(phrase);
                    if (c == null) {
                        continue;
                    }
                    w.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {
                        part = part.name, position = phrase.position, preEffectHash = phrase.preEffectHash.ToString("x16"),
                        source = c.source, ticks = c.ticks,
                        heard = c.heard.Select(v => float.IsNaN(v) ? (float?)null : v),
                        under = c.under,
                    }));
                }
            }
        }

        private static void ApplyOverrides(UProject project, RenderJob job) {
            var singer = string.IsNullOrWhiteSpace(job.Singer)
                ? null
                : ResolveSinger(job.Singer);
            var phonemizerFactory = string.IsNullOrWhiteSpace(job.Phonemizer)
                ? null
                : ResolvePhonemizer(job.Phonemizer);
            var renderer = string.IsNullOrWhiteSpace(job.Renderer)
                ? null
                : ResolveRenderer(job.Renderer);
            var resampler = string.IsNullOrWhiteSpace(job.Resampler)
                ? null
                : ResolveResampler(job.Resampler);
            var wavtool = string.IsNullOrWhiteSpace(job.Wavtool)
                ? null
                : ResolveWavtool(job.Wavtool);

            foreach (var track in project.tracks) {
                if (track.RendererSettings == null) {
                    track.RendererSettings = new URenderSettings();
                }
                if (singer != null) {
                    track.Singer = singer;
                }
                if (phonemizerFactory != null) {
                    track.Phonemizer = phonemizerFactory.Create();
                }
                if (renderer != null) {
                    track.RendererSettings.renderer = renderer;
                    track.RendererSettings.Renderer = null;
                }
                if (resampler != null) {
                    track.RendererSettings.resampler = resampler;
                    track.RendererSettings.Resampler = null;
                }
                if (wavtool != null) {
                    track.RendererSettings.wavtool = wavtool;
                    track.RendererSettings.Wavtool = null;
                }
            }
        }

        private static string RequireInputPath(string inputPath) {
            if (string.IsNullOrWhiteSpace(inputPath)) {
                throw new HeadlessRenderException("Missing required input path.");
            }
            var fullPath = Path.GetFullPath(inputPath);
            if (!File.Exists(fullPath)) {
                throw new HeadlessRenderException($"Input project not found: {fullPath}");
            }
            return fullPath;
        }

        private static string RequireOutputPath(string outputPath) {
            if (string.IsNullOrWhiteSpace(outputPath)) {
                throw new HeadlessRenderException("Missing required output path.");
            }
            return Path.GetFullPath(outputPath);
        }

        private static void EnsureOutputWritable(string outputPath) {
            try {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir)) {
                    Directory.CreateDirectory(dir);
                }
                var exists = File.Exists(outputPath);
                using (File.Open(
                    outputPath,
                    exists ? FileMode.Open : FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite)) {
                }
                if (!exists) {
                    File.Delete(outputPath);
                }
            } catch (Exception e) {
                throw new HeadlessRenderException($"Output is not writable: {outputPath}", e);
            }
        }

        internal static void EnsureProjectReady(UProject project) {
            var voiceTrackNos = project.parts
                .OfType<UVoicePart>()
                .Select(part => part.trackNo)
                .Distinct()
                .ToHashSet();
            foreach (var trackNo in voiceTrackNos) {
                var track = project.tracks[trackNo];
                if (track.Singer == null || !track.Singer.Found) {
                    throw new HeadlessRenderException(
                        $"Singer not found for track {trackNo + 1}: {track.singer ?? track.Singer?.Name ?? "(none)"}");
                }
                if (track.RendererSettings.Renderer == null) {
                    throw new HeadlessRenderException(
                        $"Renderer not found for track {trackNo + 1}: {track.RendererSettings.renderer ?? "(none)"}");
                }
                if (track.RendererSettings.Renderer.SingerType != track.Singer.SingerType) {
                    throw new HeadlessRenderException(
                        $"Renderer {track.RendererSettings.Renderer} is not supported for singer {track.Singer.Name}.");
                }
            }
            var staleParts = project.parts
                .OfType<UVoicePart>()
                .Where(part => !part.PhonemesUpToDate)
                .ToArray();
            if (staleParts.Length > 0) {
                throw new HeadlessRenderException("Phonemization did not complete for all voice parts.");
            }
            var silentParts = project.parts
                .OfType<UVoicePart>()
                .Where(part => HasRenderableNotes(part) && part.renderPhrases.Count == 0)
                .ToArray();
            if (silentParts.Length > 0) {
                var names = string.Join(", ", silentParts.Select(part => part.DisplayName));
                throw new HeadlessRenderException(
                    $"No render phrases were generated for voice part(s): {names}. Check singer, phonemizer, and aliases.");
            }
        }

        private static bool HasRenderableNotes(UVoicePart part) {
            return part.notes.Any(note =>
                !string.IsNullOrWhiteSpace(note.lyric) &&
                !string.Equals(note.lyric, "R", StringComparison.OrdinalIgnoreCase));
        }

        private static USinger ResolveSinger(string value) {
            var singerValue = value.Replace("%VOICE%", "");
            var singer = SingerManager.Inst.GetSinger(singerValue);
            if (singer != null) {
                return singer;
            }

            var candidates = SingerManager.Inst.Singers.Values.Distinct().ToArray();
            singer = candidates.FirstOrDefault(s => MatchesSinger(s, singerValue));
            if (singer != null) {
                return singer;
            }

            if (Directory.Exists(singerValue)) {
                var fullPath = Path.GetFullPath(singerValue).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                singer = candidates.FirstOrDefault(s =>
                    SamePath(s.Location, fullPath) ||
                    SamePath(s.BasePath, fullPath));
                if (singer != null) {
                    return singer;
                }
            }
            throw new HeadlessRenderException($"Singer not found: {value}");
        }

        private static bool MatchesSinger(USinger singer, string value) {
            return EqualsIgnoreCase(singer.Id, value) ||
                EqualsIgnoreCase(singer.Name, value) ||
                EqualsIgnoreCase(singer.LocalizedName, value) ||
                (singer.LocalizedNames?.Values.Any(name => EqualsIgnoreCase(name, value)) ?? false);
        }

        internal static PhonemizerFactory ResolvePhonemizer(string value) {
            var factory = PhonemizerFactory.Get(value);
            if (factory != null) {
                return factory;
            }
            factory = PhonemizerFactory.GetAll().FirstOrDefault(f =>
                EqualsIgnoreCase(f.name, value) ||
                EqualsIgnoreCase(f.tag, value) ||
                EqualsIgnoreCase(f.type.Name, value) ||
                EqualsIgnoreCase(f.type.FullName, value));
            if (factory == null) {
                throw new HeadlessRenderException($"Phonemizer not found: {value}");
            }
            return factory;
        }

        internal static string ResolveRenderer(string value) {
            var renderer = KnownRenderers.FirstOrDefault(r => EqualsIgnoreCase(r, value));
            renderer ??= value;
            if (Renderers.CreateRenderer(renderer) == null) {
                throw new HeadlessRenderException($"Renderer not found: {value}");
            }
            return renderer;
        }

        private static string ResolveResampler(string value) {
            var resampler = ToolsManager.Inst.Resamplers.FirstOrDefault(r => EqualsIgnoreCase(r.ToString(), value));
            if (resampler == null) {
                throw new HeadlessRenderException($"Resampler not found: {value}");
            }
            return resampler.ToString();
        }

        private static string ResolveWavtool(string value) {
            var wavtool = ToolsManager.Inst.Wavtools.FirstOrDefault(w => EqualsIgnoreCase(w.ToString(), value));
            if (wavtool == null) {
                throw new HeadlessRenderException($"Wavtool not found: {value}");
            }
            return wavtool.ToString();
        }

        private static bool EqualsIgnoreCase(string? left, string? right) {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static bool SamePath(string? left, string right) {
            if (string.IsNullOrWhiteSpace(left)) {
                return false;
            }
            var fullPath = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return EqualsIgnoreCase(fullPath, right);
        }
    }
}
