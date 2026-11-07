using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using NAudio.Wave;
using NWaves.Operations;
using NWaves.Signals;
using OpenUtau.Core.Format;
using OpenUtau.Core.Hts;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;
using ThirdParty;

namespace OpenUtau.Core.Neutrino {
    public class NeutrinoRenderer : HTSLabelRenderer {
        const string NTYP = "ntyp";
        const string NMOD = "nmod";
        const string NMEL = "nmel";
        const string SMOC = "smoc";

        enum NeutrinoRenderType {
            WORLD,
            NSF,
        }

        enum NeutrinoRenderMode {
            Elements = 2,
            Standard = 3,
            Advanced = 4,
        }

        enum NsfModel {
            va,
            vs,
            ve,
        }

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            Format.Ustx.PITD,
            Format.Ustx.SHFT,
            Format.Ustx.GENC,
            Format.Ustx.TENC,
            Format.Ustx.BREC,
            Format.Ustx.VOIC,
            Format.Ustx.DIR,
            NTYP,
            NMOD,
            SMOC,
            NeutrinoTeacher.ABBR,
            PhraseVoice.ABBR,
        };

        static readonly object lockObj = new object();

        public override USingerType SingerType => USingerType.Neutrino;

        public override bool SupportsRenderPitch => true;

        public override bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
        }

        protected NeutrinoSinger singer;
        string NeutrinoExe = string.Empty;
        string NeutrinoClientExe = string.Empty;
        string NeutrinoServerExe = string.Empty;
        string NsfExe = string.Empty;
        string WorldExe = string.Empty;
        string VocoderClientExe = string.Empty;
        string VocoderServerExe = string.Empty;
        bool existNeutrinoClient = false;
        int sampleRate = 48000;
        // NEUTRINO version the executable paths and dictionary were resolved for.
        NeutrinoVersion setUpVersion = NeutrinoVersion.Unsupported;

        NeutrinoVersion Version => NeutrinoUtils.DetectVersion(this.singer.singerVersion);

        public override void SetUp() {
            lang = "JPN";//TODO: use singer.language
            tablePath = NeutrinoUtils.TableFile;
            if (!NeutrinoUtils.TryResolveBasePath(this.singer.singerVersion, out string basePath, out string error)) {
                Log.Error(error);
                return;
            }
            //Load Dictionary
            try {
                phoneDict.Clear();
                LoadDict(NeutrinoUtils.DicPath(basePath, NeutrinoUtils.ConfFile), this.singer.TextFileEncoding);
                LoadDict(NeutrinoUtils.DicPath(basePath, NeutrinoUtils.TableFile), this.singer.TextFileEncoding);
                // Lyrics often handled in OpenUtau
                phoneDict.Add("R", new string[] { "pau" });
                phoneDict.Add("-", new string[] { "pau" });
                phoneDict.Add("SP", new string[] { "pau" });
                phoneDict.Add("AP", new string[] { "br" });
                g2p = this.LoadG2p();
            } catch (Exception e) {
                Log.Error(e, $"failed to load dictionary from {tablePath}");
                return;
            }
            Log.Debug("Loaded dictionary successfully.");
            var paths = NeutrinoPaths.Create(basePath);
            NeutrinoExe = paths.Neutrino;
            NeutrinoClientExe = paths.NeutrinoClient;
            NeutrinoServerExe = paths.NeutrinoServer;
            NsfExe = paths.Nsf;
            WorldExe = paths.World;
            VocoderClientExe = paths.VocoderClient;
            VocoderServerExe = paths.VocoderServer;
            existNeutrinoClient = paths.HasClient;
            Log.Debug($"NeutrinoExe: {NeutrinoExe}");
            Log.Debug(existNeutrinoClient ? $"No NeutrinoClientExe" : $"NeutrinoClientExe: {NeutrinoClientExe}");
            Log.Debug(String.IsNullOrEmpty(WorldExe) ? $"No WorldExe" : $"WorldExe: {WorldExe}");
            NeutrinoServerLauncher.EnsureStarted(NeutrinoServerExe);
            NeutrinoServerLauncher.EnsureStarted(VocoderServerExe, 23456);
            setUpVersion = Version;
        }

        protected override HTSPhoneme[] CustomHTSPhonemeContext(HTSPhoneme[] htsPhonemes, RenderNote notes) {
            var fixs = GetPrefixAndSuffix(notes);
            foreach (var htsPhoneme in htsPhonemes) {
                htsPhoneme.flag1 = "00"; // NEUTRINO Default.
            }
            return htsPhonemes;
        }

        protected override List<monoLabel> CustomMonoLabel(List<monoLabel> monoLabels) {
            if (Version == NeutrinoVersion.V27) {
                for (int i = 0; i < monoLabels.Count; i++) {
                    var label = monoLabels[i];
                    label.startMs = Math.Round(label.startMs / 10.0) * 10.0;
                    label.endMs = Math.Round(label.endMs / 10.0) * 10.0;
                    monoLabels[i] = label;
                }
            }
            return monoLabels;
        }

        public double[] LoadFile(string filePath) {
            if (File.Exists(filePath)) {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                    using (BinaryReader reader = new BinaryReader(fs)) {
                        long fileSize = fs.Length;
                        int Count = (int)(fileSize / sizeof(float));
                        double[] data = new double[Count];
                        for (int i = 0; i < Count; i++) {
                            data[i] = reader.ReadSingle();
                        }
                        return data;
                    }
                }
            }
            return new double[0];
        }

        public void SaveFile(string filePath, double[] doubles) {
            try {
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
                    using (BinaryWriter writer = new BinaryWriter(fs)) {
                        foreach (double pitch in doubles) {
                            writer.Write((float)pitch);
                        }
                    }
                }
            } catch (Exception ex) {
                Log.Error($"Error: {ex.Message}");
            }
        }

        public override Task<RenderResult> Render(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender, RenderPhraseEvents? renderEvents) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    string progressInfo = $"Track {trackNo + 1}: {this} \"{string.Join(" ", phrase.phones.Select(p => p.phoneme))}\"";
                    progress.Complete(0, progressInfo);
                    this.singer = phrase.singer as NeutrinoSinger;
                    // The renderer instance is reused for the whole track, so switching the
                    // singer to another NEUTRINO version must re-resolve the paths and dictionary.
                    if (g2p == null || string.IsNullOrEmpty(NeutrinoExe) || setUpVersion != Version) {
                        SetUp();
                    }
                    var result = Layout(phrase);
                    var hash = HashPhraseGroups(phrase);
                    string tmpPath = Path.Join(PathManager.Inst.CachePath, $"ne-{phrase.preEffectHash:x16}_temp");
                    if (!Directory.Exists(tmpPath)) {
                        Directory.CreateDirectory(tmpPath);
                    }
                    int toneShift = phrase.phones[0] != null ? phrase.phones[0].toneShift : 0;
                    ulong acousticHash = HashPhraseAcoustic(phrase);
                    ulong renderHash = HashPhraseRender(phrase);
                    string wavPath = Path.Join(tmpPath, $"ne-{renderHash}.wav");
                    string f0Path = Path.Join(tmpPath, $"ne-{acousticHash}.f0");
                    string editorf0Path = Path.Join(tmpPath, $"ne-{acousticHash}-edit.f0");
                    string melspecPath = Path.Join(tmpPath, $"ne-{acousticHash}.melspec");
                    string editorMelspecPath = Path.Join(tmpPath, $"ne-{renderHash}-edit.melspec");
                    string mgcPath = Path.Join(tmpPath, $"ne-{acousticHash}.mgc");
                    string bapPath = Path.Join(tmpPath, $"ne-{acousticHash}.bap");
                    fullScorePath = Path.Join(tmpPath, $"ne-{hash}_full_score.lab");
                    monoTimingPath = Path.Join(tmpPath, $"ne-{hash}_mono_timing.lab");
                    string modelDir = NeutrinoUtils.ModelDir(this.singer);
                    int numThreads = Preferences.Default.NumRenderThreads;
                    if (!File.Exists(fullScorePath) && !File.Exists(monoTimingPath)) {
                        ProcessPart(phrase);
                    }
                    var flag1 = phrase.phones[0].flags.FirstOrDefault(f => f.Item3.Equals(NTYP));
                    string eng = string.Empty;
                    if (flag1 != null) {
                        eng = flag1.Item1;
                    }
                    string ArgParam = string.Empty;
                    if (Version == NeutrinoVersion.V27) {
                        if (eng.Equals(NeutrinoRenderType.NSF.ToString())) {
                            var flag2 = phrase.phones[0].flags.FirstOrDefault(f => f.Item3.Equals(NMOD));
                            string nsf = "vs";
                            if (flag2 != null) {
                                if (flag2.Item2 == 4) {
                                    nsf = NsfModel.va.ToString();
                                    sampleRate = 48000;
                                } else if (flag2.Item2 == 3) {
                                    nsf = NsfModel.vs.ToString();
                                    sampleRate = 48000;
                                } else if (flag2.Item2 == 2) {
                                    nsf = NsfModel.ve.ToString();
                                    sampleRate = 24000;
                                }
                            }
                            if (!File.Exists(f0Path) || !File.Exists(melspecPath)) {
                                ArgParam = $"\"{fullScorePath}\" \"{monoTimingPath}\" \"{f0Path}\" \"{melspecPath}\" \"{modelDir}\" -s -n 1 -o {numThreads} -k {toneShift} -m -t";
                                if (existNeutrinoClient) {
                                    ProcessRunner.Run(NeutrinoClientExe, ArgParam, Log.Logger);
                                } else {
                                    ProcessRunner.Run(NeutrinoExe, ArgParam, Log.Logger);
                                }
                            }
                            if (cancellation.IsCancellationRequested) {
                                return new RenderResult();
                            }
                            if (!File.Exists(wavPath) && File.Exists(f0Path) && File.Exists(melspecPath)) {
                                if (phrase.phones[0].direct) {
                                    ArgParam = $"\"{f0Path}\" \"{melspecPath}\" \"{modelDir}{nsf}.bin\" \"{wavPath}\" -l \"{monoTimingPath}\" -n 1 -p {numThreads} -s{(int)sampleRate / 1000} -m -t";
                                } else {
                                    double[] f0 = LoadFile(f0Path);
                                    double[] melspec = LoadFile(melspecPath);
                                    int totalFrames = f0.Length;
                                    int headFrames = (int)Math.Round(headMs / framePeriod);
                                    int tailFrames = (int)Math.Round(tailMs / framePeriod);
                                    double[] editorF0 = SampleCurve(phrase, phrase.pitches, 0, framePeriod, totalFrames, headFrames, tailFrames, x => MusicMath.ToneToFreq(x * 0.01));
                                    KeepUnvoiced(f0, editorF0);
                                    KeepPredictedInBreaths(f0, editorF0, monoTimingPath, framePeriod);
                                SaveFile(editorf0Path, editorF0);
                                    ArgParam = $"\"{editorf0Path}\" \"{melspecPath}\" \"{modelDir}{nsf}.bin\" \"{wavPath}\" -l \"{monoTimingPath}\" -n 1 -p {numThreads} -s{(int)sampleRate / 1000} -m -t";
                                }
                                if (File.Exists(VocoderClientExe)) {
                                    ProcessRunner.Run(VocoderClientExe, ArgParam, Log.Logger);
                                } else {
                                    ProcessRunner.Run(NsfExe, ArgParam, Log.Logger);
                                }
                                if (!File.Exists(wavPath)) {
                                    Log.Error($"NEUTRINO produced no wav at {wavPath}. args: {ArgParam}");
                                    result.samples = new float[0];
                                    return result;
                                }
                            }
                        } else {
                            if (!File.Exists(f0Path) || !File.Exists(mgcPath) || !File.Exists(bapPath)) {
                                ArgParam = $"\"{fullScorePath}\" \"{monoTimingPath}\" \"{f0Path}\" \"{melspecPath}\" \"{modelDir}\" -w \"{mgcPath}\" \"{bapPath}\" -s -n 1 -o {numThreads} -k {toneShift} -m -t";
                                if (existNeutrinoClient) {
                                    ProcessRunner.Run(NeutrinoClientExe, ArgParam, Log.Logger);
                                } else {
                                    ProcessRunner.Run(NeutrinoExe, ArgParam, Log.Logger);
                                }
                            }
                            if (cancellation.IsCancellationRequested) {
                                return new RenderResult();
                            }
                            if (!File.Exists(wavPath) && File.Exists(f0Path) && File.Exists(mgcPath) && File.Exists(bapPath)) {
                                if (phrase.phones[0].direct) {
                                    float gender = 1f + (phrase.phones[0].flags.FirstOrDefault(f => f.Item3.Equals(Format.Ustx.GEN)).Item2 / 100f) ?? 1f;
                                    float breathiness = phrase.phones[0].flags.FirstOrDefault(f => f.Item3.Equals(Format.Ustx.BRE)).Item2 ?? 0f;
                                    ArgParam = $"\"{f0Path}\" \"{mgcPath}\" \"{bapPath}\" \"{wavPath}\" -n 1 -m {gender} -b {breathiness} -t";
                                    // vocoder_server only speaks NSF. Handing it the WORLD
                                    // arguments kills the server, so always run WORLD directly.
                                    ProcessRunner.Run(WorldExe, ArgParam, Log.Logger);
                                    if (!File.Exists(wavPath)) {
                                        Log.Error($"NEUTRINO produced no wav at {wavPath}. args: {ArgParam}");
                                        result.samples = new float[0];
                                        return result;
                                    }
                                } else {
                                    double[] f0 = LoadFile(f0Path);
                                    double[] mgc = LoadFile(mgcPath);
                                    double[] bap = LoadFile(bapPath);
                                    int totalFrames = f0.Length;
                                    int headFrames = (int)Math.Round(headMs / framePeriod);
                                    int tailFrames = (int)Math.Round(tailMs / framePeriod);

                                    var editorF0 = SampleCurve(phrase, phrase.pitches, 0, framePeriod, totalFrames, headFrames, tailFrames, x => MusicMath.ToneToFreq(x * 0.01));
                                    KeepUnvoiced(f0, editorF0);
                                    var gender = SampleCurve(phrase, phrase.gender, 0.5, framePeriod, totalFrames, headFrames, tailFrames, x => 0.5 + 0.005 * x);
                                    var tension = SampleCurve(phrase, phrase.tension, 0.5, framePeriod, totalFrames, headFrames, tailFrames, x => 0.5 + 0.005 * x);
                                    var breathiness = SampleCurve(phrase, phrase.breathiness, 0.5, framePeriod, totalFrames, headFrames, tailFrames, x => 0.5 + 0.005 * x);
                                    var voicing = SampleCurve(phrase, phrase.voicing, 1.0, framePeriod, totalFrames, headFrames, tailFrames, x => 0.01 * x);

                                    for (int i = 0; i < f0.Length; i++) {
                                        if (f0[i] < 50) {
                                            editorF0[i] = 0;
                                        }
                                    }

                                    var samples = Worldline.WorldSynthesis(
                                        editorF0,
                                        mgc, true, 60,
                                        bap, true, 2048,
                                        framePeriod, sampleRate,
                                        gender, tension, breathiness, voicing);
                                    result.samples = samples.Select(d => (float)d).ToArray();
                                    Wave.CorrectSampleScale(result.samples);
                                    var signal = new DiscreteSignal(sampleRate, result.samples);
                                    signal = Operation.Resample(signal, 44100);
                                    result.samples = signal.Samples;
                                    Wave.WriteMono16Wav(wavPath, result.samples);
                                }
                            }
                        }
                    } else if (Version == NeutrinoVersion.V3) {
                        // F0ファイル生成 (F0 の先生がいればその歌手のモデルで予測する。声 (mel・波形) は自分のモデル)
                        if (!File.Exists(f0Path)) {
                            var teacher = NeutrinoTeacher.Find(phrase);
                            string f0ModelDir = teacher != null ? NeutrinoUtils.ModelDir(teacher) : modelDir;
                            ArgParam = $"\"{fullScorePath}\" \"{monoTimingPath}\" \"{f0Path}\" \"{melspecPath}\" \"{wavPath}\" \"{f0ModelDir}\" --skip-timing --skip-melspec --skip-wav -k {toneShift} -m -t";
                            if (existNeutrinoClient) {
                                ProcessRunner.Run(NeutrinoClientExe, ArgParam, Log.Logger);
                            } else {
                                ProcessRunner.Run(NeutrinoExe, ArgParam, Log.Logger);
                            }
                        }
                        if (cancellation.IsCancellationRequested) {
                            return new RenderResult();
                        }
                        //メルスペクトグラムファイル生成
                        if (File.Exists(f0Path) && !File.Exists(melspecPath)) {
                            if (phrase.phones[0].direct) {
                                string directF0 = DirectF0(phrase, f0Path, editorf0Path, monoTimingPath, headMs, tailMs);
                                ArgParam = $"\"{fullScorePath}\" \"{monoTimingPath}\" \"{directF0}\" \"{melspecPath}\" \"{wavPath}\" \"{modelDir}\" --skip-timing --skip-f0 --skip-wav -k {toneShift} -m -t";
                            } else {
                                double[] f0 = LoadFile(f0Path);
                                int totalFrames = f0.Length;
                                int headFrames = (int)Math.Ceiling(headMs / 1000.0 * 99.84);
                                int tailFrames = (int)Math.Floor(tailMs / 1000.0 * 99.84);
                                var editorF0 = SampleCurve(phrase, phrase.pitches, 0, 9.984, totalFrames, headFrames, tailFrames, x => MusicMath.ToneToFreq(x * 0.01));
                                KeepUnvoiced(f0, editorF0);
                                KeepPredictedInBreaths(f0, editorF0, monoTimingPath, 9.984);
                                SaveFile(editorf0Path, editorF0);
                                // F0の編集とメルスペクトグラムの生成はセット
                                ArgParam = $"\"{fullScorePath}\" \"{monoTimingPath}\" \"{editorf0Path}\" \"{melspecPath}\" \"{wavPath}\" \"{modelDir}\" --skip-timing --skip-f0 --skip-wav -k {toneShift} -m -t";
                            }
                            if (existNeutrinoClient) {
                                ProcessRunner.Run(NeutrinoClientExe, ArgParam, Log.Logger);
                            } else {
                                ProcessRunner.Run(NeutrinoExe, ArgParam, Log.Logger);
                            }
                        }
                        if (cancellation.IsCancellationRequested) {
                            return new RenderResult();
                        }
                        //音声ファイル生成
                        if (!File.Exists(wavPath) && File.Exists(f0Path) && File.Exists(melspecPath)) {
                            //メルスペクトグラムの編集
                            string synthMelspecPath = EditMelspec(phrase, melspecPath, editorMelspecPath);
                            if (phrase.phones[0].direct) {
                                string directF0 = DirectF0(phrase, f0Path, editorf0Path, monoTimingPath, headMs, tailMs);
                                ArgParam = $"\"{fullScorePath}\" \"{monoTimingPath}\" \"{directF0}\" \"{synthMelspecPath}\" \"{wavPath}\" \"{modelDir}\" --skip-timing --skip-f0 --skip-melspec -k {toneShift} -m -t";
                            } else {
                                ArgParam = $"\"{fullScorePath}\" \"{monoTimingPath}\" \"{editorf0Path}\" \"{synthMelspecPath}\" \"{wavPath}\" \"{modelDir}\" --skip-timing --skip-f0 --skip-melspec -k {toneShift} -m -t";
                            }
                            if (existNeutrinoClient) {
                                ProcessRunner.Run(NeutrinoClientExe, ArgParam, Log.Logger);
                            } else {
                                ProcessRunner.Run(NeutrinoExe, ArgParam, Log.Logger);
                            }
                            if (!File.Exists(wavPath)) {
                                Log.Error($"NEUTRINO produced no wav at {wavPath}. args: {ArgParam}");
                                result.samples = new float[0];
                                return result;
                            }
                        }
                    } else {
                        Log.Error($"Unsupported NEUTRINO version: {this.singer.singerVersion}");
                        result.samples = new float[0];
                        return result;
                    }
                    progress.Complete(phrase.phones.Length, progressInfo);
                    try {
                        if (File.Exists(wavPath)) {
                            using (var waveStream = new WaveFileReader(wavPath)) {

                                result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                            }
                            if (result.samples != null) {
                                Renderers.ApplyDynamics(phrase, result);
                            }
                        }
                    } catch (Exception e) {
                        Log.Error(e.Message);
                        result.samples = new float[0];
                    }
                    return result;
                }
            });
            return task;
        }

        double[] SampleCurve(RenderPhrase phrase, float[] curve, double defaultValue, double frameMs, int length, int headFrames, int tailFrames, Func<double, double> convert) {
            const int interval = 5;
            var result = new double[length];
            if (curve == null) {
                Array.Fill(result, defaultValue);
                return result;
            }
            for (int i = 0; i < length - headFrames - tailFrames; i++) {
                double posMs = phrase.positionMs - phrase.leadingMs + i * frameMs;
                int ticks = phrase.timeAxis.MsPosToTickPos(posMs) - (phrase.position - phrase.leading);
                int index = Math.Max(0, (int)((double)ticks / interval));
                if (index < curve.Length) {
                    result[i + headFrames] = convert(curve[index]);
                }
            }
            Array.Fill(result, defaultValue, 0, headFrames);
            Array.Fill(result, defaultValue, length - tailFrames, tailFrames);
            return result;
        }


        /// <summary>
        /// Applies GENC (formant warp) and TENC (spectral tilt) to the melspec NEUTRINO
        /// predicted, and returns the path to feed into the waveform step. When both curves
        /// are flat the original file is used as is, so nothing is written or re-read.
        /// </summary>
        string EditMelspec(RenderPhrase phrase, string melspecPath, string editorMelspecPath) {
            try {
                if (File.Exists(editorMelspecPath)) {
                    return editorMelspecPath;
                }
                double[] melspec = LoadFile(melspecPath);
                int totalFrames = melspec.Length / NeutrinoMelspec.Bins;
                if (totalFrames == 0 || melspec.Length % NeutrinoMelspec.Bins != 0) {
                    Log.Warning($"Unexpected melspec size {melspec.Length} in {melspecPath}, skipping edit.");
                    return melspecPath;
                }
                int headFrames = (int)Math.Ceiling(headMs / 1000.0 * 99.84);
                int tailFrames = (int)Math.Floor(tailMs / 1000.0 * 99.84);
                var gender = SampleCurve(phrase, phrase.gender, 0, 9.984, totalFrames, headFrames, tailFrames, x => x);
                var tension = SampleCurve(phrase, phrase.tension, 0, 9.984, totalFrames, headFrames, tailFrames, x => x);
                if (NeutrinoMelspec.IsNoOp(gender, tension)) {
                    return melspecPath;
                }
                SaveFile(editorMelspecPath, NeutrinoMelspec.Edit(melspec, gender, tension));
                return editorMelspecPath;
            } catch (Exception e) {
                Log.Error(e, "Failed to edit melspec, falling back to the unedited one.");
                return melspecPath;
            }
        }

        public override UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            var result = new List<UExpressionDescriptor> {
                ////engine
                new UExpressionDescriptor {
                    name = "NEUTRINO engine type (~2.x)",
                    abbr = NTYP,
                    type = UExpressionType.Options,
                    options = Enum.GetNames<NeutrinoRenderType>(),
                    isFlag = false
                },
                ////engine mode
                new UExpressionDescriptor {
                    name = "NEUTRINO engine mode (~2.x)",
                    abbr = NMOD,
                    type = UExpressionType.Options,
                    options = Enum.GetNames<NeutrinoRenderMode>(),
                    isFlag = false
                },
                //expressiveness
                new UExpressionDescriptor {
                    name = "pitch smoothened (curve)",
                    abbr = SMOC,
                    type = UExpressionType.Curve,
                    min = 0,
                    max = 10,
                    defaultValue = 0,
                    isFlag = false
                },
                NeutrinoTeacher.Descriptor(),
                PhraseVoice.Descriptor(),
            };
            return result.ToArray();
        }

                /// <summary>
        /// 息継ぎ (br)・休符 (pau/sil) の音素区間は、編集した音程で上書きせず NEUTRINO の予測値を使う。
        /// NEUTRINO は息継ぎで低い F0 (約 90Hz) を予測し、それが息として鳴らす合図になっている。
        /// 画面の音程 (前の音の高さ) で上書きすると、息継ぎが高い声のうなりになる。
        /// </summary>
        static void KeepPredictedInBreaths(double[] predicted, double[] edited, string monoTimingPath, double frameMs) {
            if (!File.Exists(monoTimingPath)) return;
            int n = Math.Min(predicted.Length, edited.Length);
            foreach (var line in File.ReadAllLines(monoTimingPath)) {
                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 3 || !(p[2] == "br" || p[2] == "pau" || p[2] == "sil")) continue;
                int a = (int)Math.Floor(long.Parse(p[0]) / 1e4 / frameMs);
                int b = (int)Math.Ceiling(long.Parse(p[1]) / 1e4 / frameMs);
                for (int i = Math.Max(0, a); i < Math.Min(n, b); i++) edited[i] = predicted[i];
            }
        }

        /// <summary>
        /// NEUTRINO が無声 (F0=0) と予測したフレーム (息継ぎ・無声子音・休符) は、編集した音程で
        /// 上書きせず 0 のまま残す。上書きすると NSF が息や子音を有声で鳴らし、うなりが混ざる。
        /// </summary>
        /// <summary>
        /// direct: NEUTRINO が予測した F0 をそのまま使い、音符のビブラートと PITD の分 (phrase.pitchDeviation) だけを
        /// 有声のコマへ足す。足す物が無ければ予測の F0 のファイルをそのまま返す (従来の direct と同じ音)。
        /// 息継ぎ・休符は予測のまま。
        /// </summary>
        string DirectF0(RenderPhrase phrase, string f0Path, string editorf0Path, string monoTimingPath, double headMs, double tailMs) {
            if (phrase.pitchDeviation == null || phrase.pitchDeviation.All(x => x == 0)) {
                return f0Path;
            }
            double[] f0 = LoadFile(f0Path);
            int totalFrames = f0.Length;
            int headFrames = (int)Math.Ceiling(headMs / 1000.0 * 99.84);
            int tailFrames = (int)Math.Floor(tailMs / 1000.0 * 99.84);
            double[] cents = SampleCurve(phrase, phrase.pitchDeviation, 0, 9.984, totalFrames, headFrames, tailFrames, x => x);
            double[] edited = new double[totalFrames];
            for (int i = 0; i < totalFrames; i++) {
                edited[i] = f0[i] > 0 ? f0[i] * Math.Pow(2, cents[i] / 1200.0) : f0[i];
            }
            KeepPredictedInBreaths(f0, edited, monoTimingPath, 9.984);
            SaveFile(editorf0Path, edited);
            return editorf0Path;
        }

        static void KeepUnvoiced(double[] predicted, double[] edited) {
            int n = Math.Min(predicted.Length, edited.Length);
            for (int i = 0; i < n; i++) {
                if (predicted[i] <= 0) edited[i] = 0;
            }
        }

        public override string ToString() => Renderers.NEUTRINO;

        /// <summary>
        /// VOICEVOX の音素表記 (無声化の大文字の母音) を NEUTRINO の表記へ写す。NEUTRINO は大文字の母音を使わないので、
        /// NEUTRINO の音素化の句には何もしない。VOICEVOX のトラックの句を NEUTRINO で歌う時 (句ごとの声・F0 の先生) に効く。
        /// </summary>
        protected override string MapSymbol(string symbol) {
            switch (symbol) {
                case "A": return "a";
                case "I": return "i";
                case "U": return "u";
                case "E": return "e";
                case "O": return "o";
                default: return symbol;
            }
        }

        /// <summary>先生の NEUTRINO が歌った句。f0 は FrameMs 刻み (0 = 無声)、コマ 0 とサンプル 0 は originMs (絶対 ms)。</summary>
        public sealed class TeacherOutput {
            public const double FrameMs = 10.0;
            public double[] f0;
            public float[] samples;
            public int sampleRate;
            public double originMs;
        }

        /// <summary>
        /// 句を先生の NEUTRINO で歌わせる (音素の置き方は句の音素の位置のまま、F0 と波形は先生のモデル)。
        /// 別の描画器 (VOICEVOX) が先生の F0・音量を使うための物。cache は nt-* に置く。
        /// </summary>
        public static TeacherOutput RenderTeacher(RenderPhrase phrase, NeutrinoSinger teacher) {
            var r = new NeutrinoRenderer { singer = teacher };
            lock (lockObj) {
                r.SetUp();
                if (r.setUpVersion != NeutrinoVersion.V3 || string.IsNullOrEmpty(r.NeutrinoExe)) {
                    throw new Exception($"NEUTRINO teacher {teacher.Id} could not be set up.");
                }
                r.Layout(phrase);
                int toneShift = phrase.phones[0] != null ? phrase.phones[0].toneShift : 0;
                string dir = TeacherCacheDir(phrase, teacher);
                Directory.CreateDirectory(dir);
                r.fullScorePath = Path.Join(dir, "full_score.lab");
                r.monoTimingPath = Path.Join(dir, "mono_timing.lab");
                string f0Path = Path.Join(dir, "teacher.f0");
                string melspecPath = Path.Join(dir, "teacher.melspec");
                string wavPath = Path.Join(dir, "teacher.wav");
                string modelDir = NeutrinoUtils.ModelDir(teacher);
                if (!File.Exists(r.fullScorePath) || !File.Exists(r.monoTimingPath)) {
                    r.ProcessPart(phrase);
                }
                string Args(string skip) => $"\"{r.fullScorePath}\" \"{r.monoTimingPath}\" \"{f0Path}\" \"{melspecPath}\" \"{wavPath}\" \"{modelDir}\" --skip-timing {skip} -k {toneShift} -m -t";
                void Run(string args) {
                    ProcessRunner.Run(r.existNeutrinoClient ? r.NeutrinoClientExe : r.NeutrinoExe, args, Log.Logger);
                }
                if (!File.Exists(f0Path)) {
                    Run(Args("--skip-melspec --skip-wav"));
                }
                if (File.Exists(f0Path) && !File.Exists(melspecPath)) {
                    Run(Args("--skip-f0 --skip-wav"));
                }
                if (File.Exists(melspecPath) && !File.Exists(wavPath)) {
                    Run(Args("--skip-f0 --skip-melspec"));
                }
                if (!File.Exists(f0Path) || !File.Exists(wavPath)) {
                    throw new Exception($"NEUTRINO teacher {teacher.Id} produced no f0/wav in {dir}.");
                }
                var output = new TeacherOutput { f0 = r.LoadFile(f0Path), originMs = phrase.positionMs - r.headMs };
                using (var waveStream = new WaveFileReader(wavPath)) {
                    // Wave.GetSamples は 44100 Hz へ変換して返す (先生の WAV は 48000 Hz)。時刻→サンプルの換算は変換後の速さで
                    output.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                    output.sampleRate = 44100;
                }
                return output;
            }
        }

        /// <summary>
        /// direct の句の、画面に描く音程。heard = 描画済みの素の F0 + 音符のビブラート + PITD(実際に鳴る線、無声は NaN)、
        /// under = heard − PITD(ペンで描いた高さから PITD を出す基準、無声は前後でつなぐ)。単位は MIDI×100。
        /// 素の F0 はこの句の cache(今の編集の分が未描画なら同じ句の最後に描いた素)から読む。
        /// </summary>
        public class DirectPitchCurve {
            public int[] ticks;
            public float[] heard;
            public float[] under;
            public string source;
        }

        static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, DirectPitchCurve> directPitchCache = new();

        public static DirectPitchCurve GetDirectPitch(RenderPhrase phrase) {
            if (phrase == null || phrase.phones.Length == 0 || !phrase.phones[0].direct
                || phrase.pitchDeviation == null || phrase.pitches.Length == 0) {
                return null;
            }
            if (phrase.renderer is NeutrinoRenderer nr) {
                return nr.LoadDirectPitch(phrase);
            }
            // VOICEVOX の句に NEUTRINO の F0 の先生が付いている時は、先生の F0 (+ ビブラート + PITD) が鳴る線
            if (phrase.renderer is Voicevox.VoicevoxRenderer) {
                var teacher = NeutrinoTeacher.Find(phrase);
                if (teacher != null) {
                    return LoadTeacherPitch(phrase, teacher);
                }
            }
            return null;
        }

        static string TeacherCacheDir(RenderPhrase phrase, NeutrinoSinger teacher) {
            int toneShift = phrase.phones[0] != null ? phrase.phones[0].toneShift : 0;
            return Path.Join(PathManager.Inst.CachePath, $"nt-{phrase.preEffectHash:x16}_{teacher.voicebankNameHash:x16}_{toneShift}");
        }

        /// <summary>先生の描画済み F0 (RenderTeacher の cache) から、VOICEVOX が鳴らす線を作る。未描画なら null。</summary>
        static DirectPitchCurve LoadTeacherPitch(RenderPhrase phrase, NeutrinoSinger teacher) {
            try {
                string f0File = Path.Join(TeacherCacheDir(phrase, teacher), "teacher.f0");
                if (!File.Exists(f0File)) {
                    return null;
                }
                if (directPitchCache.TryGetValue(phrase.hash, out var cached) && cached.source == f0File) {
                    return cached;
                }
                var reader = new NeutrinoRenderer { singer = teacher };
                double[] f0 = reader.LoadFile(f0File);
                var axis = phrase.timeAxis;
                // RenderTeacher と同じ原点 (句の頭から 1 小節前、HTSLabelRenderer.Layout の headMs)
                double headMsLocal = Math.Round(60000.0 / axis.GetBpmAtTick(phrase.position) * axis.TimeSignatureAtTick(phrase.position).beatPerBar);
                double originMs = phrase.positionMs - headMsLocal;
                int n = f0.Length;
                int pitchStart = phrase.position - phrase.leading;
                int pitchEnd = pitchStart + phrase.pitches.Length * 5;
                double shift = Math.Pow(2, -phrase.phones[0].toneShift / 12.0);
                var curve = new DirectPitchCurve { ticks = new int[n], heard = new float[n], under = new float[n], source = f0File };
                for (int k = 0; k < n; k++) {
                    // VoicevoxRenderer.TeacherSynthParams と同じ時刻の当て方 (先生の f0 の k 番目 = originMs + k × 10 ms)
                    double ms = originMs + k * TeacherOutput.FrameMs;
                    int tick = axis.MsPosToTickPos(ms);
                    curve.ticks[k] = tick;
                    bool body = tick >= pitchStart && tick < pitchEnd;
                    int idx = Math.Clamp((tick - pitchStart) / 5, 0, phrase.pitches.Length - 1);
                    float dev = body ? phrase.pitchDeviation[idx] : 0;
                    float pitd = body ? phrase.pitches[idx] - phrase.pitchesBeforeDeviation[idx] : 0;
                    if (body && f0[k] > 0) {
                        float c = (float)(MusicMath.FreqToTone(f0[k] * shift) * 100);
                        curve.heard[k] = c + dev;
                        curve.under[k] = c + dev - pitd;
                    } else {
                        curve.heard[k] = float.NaN;
                        curve.under[k] = float.NaN;
                    }
                }
                FillGaps(curve.under);
                if (directPitchCache.Count > 4000) {
                    directPitchCache.Clear();
                }
                directPitchCache[phrase.hash] = curve;
                return curve;
            } catch (Exception e) {
                Log.Error(e, "Failed to load NEUTRINO teacher pitch.");
                return null;
            }
        }

        DirectPitchCurve LoadDirectPitch(RenderPhrase phrase) {
            try {
                if (phrase.singer is not NeutrinoSinger s || NeutrinoUtils.DetectVersion(s.singerVersion) != NeutrinoVersion.V3) {
                    return null;
                }
                string tmpPath = Path.Join(PathManager.Inst.CachePath, $"ne-{phrase.preEffectHash:x16}_temp");
                if (!Directory.Exists(tmpPath)) {
                    return null;
                }
                string exact = Path.Join(tmpPath, $"ne-{HashPhraseAcoustic(phrase)}.f0");
                string f0File = File.Exists(exact) ? exact : Directory.GetFiles(tmpPath, "ne-*.f0")
                    .Where(f => !f.EndsWith("-edit.f0")).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
                if (f0File == null) {
                    return null;
                }
                if (directPitchCache.TryGetValue(phrase.hash, out var cached) && cached.source == f0File) {
                    return cached;
                }
                double[] f0 = LoadFile(f0File);
                var axis = phrase.timeAxis;
                double headMsLocal = Math.Round(60000.0 / axis.GetBpmAtTick(phrase.position) * axis.TimeSignatureAtTick(phrase.position).beatPerBar);
                double tailMsLocal = Math.Round(60000.0 / axis.GetBpmAtTick(phrase.end) * axis.TimeSignatureAtTick(phrase.end).beatPerBar);
                int n = f0.Length;
                int headFrames = (int)Math.Ceiling(headMsLocal / 1000.0 * 99.84);
                int tailFrames = (int)Math.Floor(tailMsLocal / 1000.0 * 99.84);
                int pitchStart = phrase.position - phrase.leading;
                double shift = Math.Pow(2, -phrase.phones[0].toneShift / 12.0);
                var curve = new DirectPitchCurve { ticks = new int[n], heard = new float[n], under = new float[n], source = f0File };
                for (int k = 0; k < n; k++) {
                    // DirectF0 (SampleCurve) と同じ時刻の当て方
                    double ms = phrase.positionMs - phrase.leadingMs + (k - headFrames) * 9.984;
                    int tick = axis.MsPosToTickPos(ms);
                    curve.ticks[k] = tick;
                    int idx = Math.Clamp((int)((double)(tick - pitchStart) / 5), 0, phrase.pitches.Length - 1);
                    bool body = k >= headFrames && k < n - tailFrames;
                    float dev = body ? phrase.pitchDeviation[idx] : 0;
                    float pitd = body ? phrase.pitches[idx] - phrase.pitchesBeforeDeviation[idx] : 0;
                    if (f0[k] > 0) {
                        float c = (float)(MusicMath.FreqToTone(f0[k] * shift) * 100);
                        curve.heard[k] = c + dev;
                        curve.under[k] = c + dev - pitd;
                    } else {
                        curve.heard[k] = float.NaN;
                        curve.under[k] = float.NaN;
                    }
                }
                FillGaps(curve.under);
                if (directPitchCache.Count > 4000) {
                    directPitchCache.Clear();
                }
                directPitchCache[phrase.hash] = curve;
                return curve;
            } catch (Exception e) {
                Log.Error(e, "Failed to load NEUTRINO direct pitch.");
                return null;
            }
        }

        static void FillGaps(float[] v) {
            int last = -1;
            for (int i = 0; i < v.Length; i++) {
                if (float.IsNaN(v[i])) {
                    continue;
                }
                if (last < 0) {
                    for (int j = 0; j < i; j++) v[j] = v[i];
                } else if (i - last > 1) {
                    for (int j = last + 1; j < i; j++) v[j] = v[last] + (v[i] - v[last]) * (j - last) / (i - last);
                }
                last = i;
            }
            if (last >= 0) {
                for (int j = last + 1; j < v.Length; j++) v[j] = v[last];
            }
        }

        public override RenderPitchResult LoadRenderedPitch(RenderPhrase phrase) {
            if (this.singer == null) {
                // Nothing rendered yet, so Version cannot be resolved.
                return null;
            }
            var result = new RenderPitchResult();
            try {
                string tmpPath = Path.Join(PathManager.Inst.CachePath, $"ne-{phrase.preEffectHash:x16}_temp");
                string f0Path = Path.Join(tmpPath, $"ne-{HashPhraseAcoustic(phrase)}.f0");
                if (!File.Exists(f0Path)) {
                    return null;
                }
                double[] f0 = LoadFile(f0Path);

                int totalFrames = f0.Length;
                int headFrames = 0;
                int tailFrames = 0;
                if (Version == NeutrinoVersion.V3) {
                    headFrames = (int)Math.Round(headMs / 1000.0 * 99.84);
                    tailFrames = (int)Math.Round(tailMs / 1000.0 * 99.84);
                } else {
                    headFrames = (int)Math.Round(headMs / framePeriod);
                    tailFrames = (int)Math.Round(tailMs / framePeriod);
                }
                var exprCurve = phrase.curves.FirstOrDefault(curve => curve.Item1.Equals(SMOC));
                if (exprCurve != null) {

                    List<int> exprs = SampleCurve(phrase, exprCurve.Item2, 0, framePeriod, totalFrames, headFrames, tailFrames, x => x).Select(x => (int)x).ToList();
                    var f0S = new F0Smoother(f0.ToList());
                    f0S.SmoothenWidthList = exprs;
                    f0 = f0S.GetSmoothenedF0List(f0.ToList()).ToArray();
                }

                int toneShift = phrase.phones[0] != null ? phrase.phones[0].toneShift : 0;
                result = new RenderPitchResult() {
                    tones = f0.Select(f => (float)MusicMath.FreqToTone(f * Math.Pow(2, ((toneShift * -1) / 12d)))).ToArray(),
                };
                result.ticks = new float[result.tones.Length];
                var layout = Layout(phrase);
                var t = layout.positionMs - layout.leadingMs;
                for (int i = 0; i < result.tones.Length; i++) {
                    if (Version == NeutrinoVersion.V3) {
                        t += 10;
                    } else {
                        t += framePeriod;
                    }
                    result.ticks[i] = phrase.timeAxis.MsPosToTickPos(t) - phrase.position;
                }
            } catch (Exception e) {
                Log.Error(e, "Failed to load rendered pitch.");
                return null;
            }
            return result;
        }


        // Cache keys are layered so that an edit only invalidates the steps that actually
        // depend on it. phrase.hash is deliberately not used: it mixes in every curve, so a
        // gender tweak would re-run the acoustic model, and it omits toneShift entirely
        // because RenderPhone.Hash() does not write it.
        //
        //   group    : notes + StyleShift              -> label files
        //   acoustic : group + pitch curve             -> f0, edited f0, melspec
        //   render   : acoustic + vocoder side curves  -> edited melspec, wav
        //
        // DYN is in none of them on purpose. It is applied to the samples after the wav is
        // read, so changing it never needs a re-render.
        ulong HashPhrase(RenderPhrase phrase, bool withPitch, bool withVocoderCurves) {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(phrase.preEffectHash);
                    writer.Write(phrase.phones[0] != null ? phrase.phones[0].toneShift : 0);
                    if (withPitch) {
                        WriteCurve(writer, phrase.pitches);
                    }
                    if (withVocoderCurves) {
                        WriteCurve(writer, phrase.gender);
                        WriteCurve(writer, phrase.tension);
                        WriteCurve(writer, phrase.breathiness);
                        WriteCurve(writer, phrase.voicing);
                    }
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }

        static void WriteCurve(BinaryWriter writer, float[] curve) {
            if (curve == null) {
                writer.Write("null");
                return;
            }
            foreach (var value in curve) {
                writer.Write(value);
            }
        }

        ulong HashPhraseGroups(RenderPhrase phrase) => HashPhrase(phrase, false, false);

        ulong HashPhraseAcoustic(RenderPhrase phrase) => HashPhrase(phrase, true, false);

        ulong HashPhraseRender(RenderPhrase phrase) => HashPhrase(phrase, true, true);
    }
}
