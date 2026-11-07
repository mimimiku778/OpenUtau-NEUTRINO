using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using NAudio.Wave;
using OpenUtau.Core.Format;
using OpenUtau.Core.Neutrino;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using Serilog;
using ThirdParty;

/*
 * This source code is partially based on the VOICEVOX engine.
 * https://github.com/VOICEVOX/voicevox_engine/blob/master/LGPL_LICENSE
 */

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxRenderer : IRenderer {
        const string VOLC = VoicevoxUtils.VOLC;
        const string SMOC = VoicevoxUtils.SMOC;
        const string REPM = VoicevoxUtils.REPM;
        const string PITD = Format.Ustx.PITD;

        static readonly HashSet<string> supportedExp = new HashSet<string>(){
            Format.Ustx.DYN,
            PITD,
            Format.Ustx.CLR,
            Format.Ustx.VOL,
            Format.Ustx.DIR,
            Format.Ustx.SHFT,
            NeutrinoTeacher.ABBR,
            PhraseVoice.ABBR,
            VOLC,
            SMOC,
            REPM,
        };

        static readonly object lockObj = new object();

        public USingerType SingerType => USingerType.Voicevox;

        public bool SupportsRenderPitch => true;

        public bool SupportsPhonemeEnvelope => false;

        public bool SupportsExpression(UExpressionDescriptor descriptor) {
            return supportedExp.Contains(descriptor.abbr);
        }

        public RenderResult Layout(RenderPhrase phrase) {
            var span = VoicevoxUtils.GetSynthSpan(phrase);
            return VoicevoxUtils.ComputeLayout(span.startMs, span.endMs, phrase.leadingMs);
        }

        public Task<RenderResult> Render(RenderPhrase phrase, Progress progress, int trackNo, CancellationTokenSource cancellation, bool isPreRender, RenderPhraseEvents? renderEvents = null) {
            var task = Task.Run(() => {
                lock (lockObj) {
                    if (cancellation.IsCancellationRequested) {
                        return new RenderResult();
                    }
                    string progressInfo = $"Track {trackNo + 1}: {this} \"{string.Join(" ", phrase.phones.Select(p => p.phoneme))}\"";
                    progress.Complete(0, progressInfo);
                    ulong toneHash = HashPhraseGroups(phrase);
                    var wavPath = Path.Join(PathManager.Inst.CachePath, $"vv-{phrase.hash:x16}_{toneHash:x16}.wav");
                    phrase.AddCacheFile(wavPath);
                    var result = Layout(phrase);
                    if (!File.Exists(wavPath)) {
                        var singer = phrase.singer as VoicevoxSinger;
                        if (singer != null) {
                            if (VoicevoxUtils.dic == null) {
                                VoicevoxUtils.Loaddic(singer);
                            }
                            try {
                                VoicevoxSynthParams vsParams = PhraseToVoicevoxSynthParams(phrase, phrase.singer as VoicevoxSinger, false);

                                int vvTotalFrames = 0;
                                double frameMs = (1000d / VoicevoxUtils.fps);
                                vsParams.phonemes.ForEach(x => vvTotalFrames += x.frame_length);
                                if (!phrase.phones[0].direct) {
                                    vsParams.f0 = VoicevoxUtils.SampleCurve(phrase, phrase.pitches, 0, frameMs, vvTotalFrames, vsParams.phonemes[0].frame_length, vsParams.phonemes[^1].frame_length, 0, x => MusicMath.ToneToFreq(x * 0.01)).ToList();
                                } else {
                                    //vsParams.f0 = ToneShift(phrase, vsParams);
                                    vsParams.f0 = vsParams.f0.Select(f0 => f0 = f0 * Math.Pow(2, ((phrase.phones[0].toneShift * -1) / 12d))).ToList();
                                    if (TeacherOf(phrase) != null) {
                                        // NEUTRINO の direct と同じく、先生の F0 に音符のビブラートと PITD の分だけを足す
                                        var cents = VoicevoxUtils.SampleCurve(phrase, phrase.pitchDeviation, 0, frameMs, vvTotalFrames, vsParams.phonemes[0].frame_length, vsParams.phonemes[^1].frame_length, 0, x => x);
                                        vsParams.f0 = vsParams.f0.Select((f, i) => f > 0 ? f * Math.Pow(2, cents[i] / 1200.0) : f).ToList();
                                    }
                                }

                                //Volume parameter for synthesis. Scheduled to be revised
                                var volumeCurve = phrase.curves.FirstOrDefault(c => c.Item1 == VOLC);
                                if (volumeCurve != null) {
                                    var volumes = VoicevoxUtils.SampleCurve(phrase, volumeCurve.Item2, 0, frameMs, vvTotalFrames, vsParams.phonemes[0].frame_length, vsParams.phonemes[^1].frame_length, -10, x => x * 0.01);
                                    vsParams.volume = vsParams.volume.Select((vol, i) => vol = vol * volumes[i]).ToList();
                                } else {
                                    vsParams.volume = vsParams.volume.Select(vol => vol = vol * phrase.phones[0].volume).ToList();
                                }
                                for (int i = 0; i < vsParams.phonemes[0].frame_length; i++) {
                                    vsParams.volume[i] = 0;
                                }
                                for (int i = vsParams.volume.Count - vsParams.phonemes[vsParams.phonemes.Count - 1].frame_length; i < vsParams.volume.Count; i++) {
                                    vsParams.volume[i] = 0;
                                }

                                int speakerID = 0;
                                singer.voicevoxConfig.styles.ForEach(style => {
                                    if (style.name.Equals(phrase.singer.Subbanks[1].Suffix) && style.type.Equals("frame_decode")) {
                                        speakerID = style.id;
                                    }
                                    // Apply the voice color setting value
                                    if (style.name.Equals(phrase.phones[0].suffix) && style.type.Equals("frame_decode")) {
                                        speakerID = style.id;
                                    } else // Supports styles with the same name but different types
                                    if ((style.name + "_" + style.type).Equals(phrase.phones[0].suffix)) {
                                        speakerID = style.id;
                                    }
                                });
                                // 句ごとの声で選んだスタイル (別の歌手のトラックの句では音素の声色が使えないので、こちらが優先)
                                if (!string.IsNullOrEmpty(phrase.voiceStyle)) {
                                    var chosen = singer.voicevoxConfig.styles.FirstOrDefault(style => style.name == phrase.voiceStyle && style.type == "frame_decode");
                                    if (chosen != null) {
                                        speakerID = chosen.id;
                                    } else {
                                        Log.Warning($"VOICEVOX style {phrase.voiceStyle} is not in {singer.Id}; using the default style.");
                                    }
                                }
                                // OpenUtau mixes at 44.1 kHz. Ask the engine for 44.1 kHz instead of its native 24 kHz,
                                // so the audio is not resampled again on our side.
                                vsParams.outputSamplingRate = 44100;
                                VoicevoxUtils.InitializedSpeaker(speakerID.ToString(), false);
                                var dumpDir = Environment.GetEnvironmentVariable("OPENUTAU_DUMP_VV_QUERY");
                                if (!string.IsNullOrEmpty(dumpDir)) {
                                    Directory.CreateDirectory(dumpDir);
                                    int sf = VoicevoxUtils.ToFrame(VoicevoxUtils.GetSynthSpan(phrase).startMs);
                                    File.WriteAllText(Path.Join(dumpDir, $"{phrase.position:D8}.json"),
                                        $"{{\"positionMs\":{phrase.positionMs},\"startFrame\":{sf},\"layoutMs\":{result.positionMs},\"speaker\":{speakerID},\"teacher\":\"{TeacherOf(phrase)?.Id}\",\"query\":{Json.Serialize(vsParams)}}}");
                                }
                                var queryurl = new VoicevoxURL() { method = "POST", path = "/frame_synthesis", query = new Dictionary<string, string> { { "speaker", speakerID.ToString() } }, body = Json.Serialize(vsParams), accept = "audio/wav" };
                                var response = VoicevoxClient.Inst.SendRequest(queryurl);
                                byte[] bytes = null;
                                if (!response.Item2.Equals(null)) {
                                    bytes = response.Item2;
                                } else if (!string.IsNullOrEmpty(response.Item1)) {
                                    var jObj = JsonObject.Parse(response.Item1);
                                    if (jObj?.AsObject().ContainsKey("detail") == true) {
                                        Log.Error($"Voice synthesis failed with the VOICEVOX engine. : {jObj}");
                                    }
                                }
                                if (bytes != null) {
                                    File.WriteAllBytes(wavPath, bytes);
                                }
                            } catch (MessageCustomizableException) {
                                //BuildVNotes has already built a message for the user.
                                throw;
                            } catch (VoicevoxException e) {
                                throw new MessageCustomizableException("Failed to create the audio.", e.Message, e);
                            } catch (Exception e) {
                                Log.Error(e, "Failed to create the audio.");
                            }
                            if (cancellation.IsCancellationRequested) {
                                return new RenderResult();
                            }
                        }
                    }
                    progress.Complete(phrase.phones.Length, progressInfo);
                    if (File.Exists(wavPath)) {
                        using (var waveStream = new WaveFileReader(wavPath)) {

                            result.samples = Wave.GetSamples(waveStream.ToSampleProvider().ToMono(1, 0));
                        }
                        if (result.samples != null) {
                            Renderers.ApplyDynamics(phrase, result);
                        }
                    }
                    return result;
                }
            });
            return task;
        }

        //Match the phonemes in the synthesis parameters to the scores in the score to update F0 and volume  
        private VoicevoxSynthParams PhraseToVoicevoxSynthParams(RenderPhrase phrase, VoicevoxSinger singer, bool pitch_slur) {
            var teacher = TeacherOf(phrase);
            if (teacher != null) {
                return TeacherSynthParams(phrase, teacher);
            }

            //Prepare for future additions of Teacher Singer.
            string baseSingerID = VoicevoxUtils.getBaseSingerID(singer);
            VoicevoxUtils.InitializedSpeaker(baseSingerID, true);
            List<VoicevoxNote> vNotes = BuildVNotes(phrase);

            //Create parameters for the update source. 
            VoicevoxQueryMain vqMain = VoicevoxUtils.NoteGroupsToVQuery(vNotes.ToArray(), phrase.timeAxis);
            VoicevoxSynthParams vsParams;
            if (VoicevoxUtils.IsPhonemeNoteCountMatch(phrase)) {
                vsParams = VoicevoxUtils.VoicevoxVoiceBase(vqMain, baseSingerID);
            } else {
                //vsParamsServer is a parameter to hold phonemes generated from note lyrics
                VoicevoxSynthParams vsParamsServer = VoicevoxUtils.VoicevoxVoiceBase(vqMain, baseSingerID);

                //Create parameters for the update destination.
                vsParams = PhonemeToVoicevoxSynthParams(phrase);
                if (vsParams.phonemes.Count != vsParamsServer.phonemes.Count) {
                    // 句の音素が VOICEVOX の書き方でない (NEUTRINO 等のトラックで句ごとの声を VOICEVOX にした時) と、
                    // 歌詞から作る音素の並びと合わず、エンジンが「音素列が一致しません」と断る。
                    // その時は VOICEVOX が歌詞から作った置き方・高さ・音量のまま歌う。
                    Log.Warning($"VOICEVOX: the phrase's phonemes ({vsParams.phonemes.Count}) do not match the lyrics ({vsParamsServer.phonemes.Count}); singing with VOICEVOX's own timing.");
                    vsParams = vsParamsServer;
                    if (pitch_slur) {
                        VoicevoxUtils.AdjustF0ForSlur(VoicevoxUtils.NoteGroupsToVQuery(vNotes.ToArray(), phrase.timeAxis, true), vsParams.f0);
                    }
                    return vsParams;
                }
                //vsParamsUser is a parameter to hold phonemes changed by the user
                VoicevoxSynthParams vsParamsUser = vsParams.Clone();
                if (vsParams.phonemes.Count == vsParamsServer.phonemes.Count) {
                    for (int i = 0; i < vsParamsServer.phonemes.Count; i++) {
                        vsParams.phonemes[i].phoneme = vsParamsServer.phonemes[i].phoneme;
                    }
                }
                //Update F0 and volume
                vsParams.f0 = VoicevoxUtils.QueryToF0(vqMain, vsParams, baseSingerID);
                vsParams.volume = VoicevoxUtils.QueryToVolume(vqMain, vsParams, baseSingerID);
                //Update phoneme
                for (int i = 0; i < vsParamsUser.phonemes.Count; i++) {
                    vsParams.phonemes[i].phoneme = vsParamsUser.phonemes[i].phoneme;
                }
            }
            if (pitch_slur) {
                VoicevoxUtils.AdjustF0ForSlur(VoicevoxUtils.NoteGroupsToVQuery(vNotes.ToArray(), phrase.timeAxis, true), vsParams.f0);
            }
            return vsParams;
        }

        /// <summary>句の F0 の先生 (NEUTRINO)。音素で書かれた句だけ (かなの句は VOICEVOX の先生のまま)。</summary>
        static NeutrinoSinger TeacherOf(RenderPhrase phrase) {
            var teacher = NeutrinoTeacher.Find(phrase);
            if (teacher != null && VoicevoxUtils.IsPhonemeNoteCountMatch(phrase)) {
                Log.Warning("Singing style needs a phoneme-based phonemizer (VOICEVOX JA); this phrase is written in kana, so the VOICEVOX teacher is used.");
                return null;
            }
            return teacher;
        }

        /// 母音 (音量の基準を取る音素)。
        static readonly HashSet<string> TeacherVowels = new HashSet<string> { "a", "i", "u", "e", "o", "N", "A", "I", "U", "E", "O" };
        /// 母音の音量中央値の正規化先 (VOICEVOX の先生の典型値)、上限。
        const double TeacherVowelVolume = 0.058;
        const double TeacherVolumeMax = 0.25;

        /// <summary>
        /// NEUTRINO の先生で句を作る: 音素の置き方は画面 (句の音素の位置) のまま、F0 は先生の予測をそのまま、
        /// 音量は先生の描画の RMS (25 ms) を、母音の中央値が VOICEVOX の先生の典型値になるよう揃える。
        /// </summary>
        private VoicevoxSynthParams TeacherSynthParams(RenderPhrase phrase, NeutrinoSinger teacher) {
            var vsParams = PhonemeToVoicevoxSynthParams(phrase);
            var t = NeutrinoRenderer.RenderTeacher(phrase, teacher);
            int n = vsParams.f0.Count;
            int head = vsParams.phonemes[0].frame_length;
            int startFrame = VoicevoxUtils.ToFrame(VoicevoxUtils.GetSynthSpan(phrase).startMs);
            double frameMs = 1000.0 / VoicevoxUtils.fps;
            // 25 ms の RMS (中心窓) を累積和で引く
            int win = Math.Max(1, (int)(0.025 * t.sampleRate));
            var cum = new double[t.samples.Length + 1];
            for (int i = 0; i < t.samples.Length; i++) {
                cum[i + 1] = cum[i] + (double)t.samples[i] * t.samples[i];
            }
            double RmsAt(int k) {
                int a = Math.Clamp(k - win / 2, 0, t.samples.Length);
                int b = Math.Clamp(a + win, 0, t.samples.Length);
                return b > a ? Math.Sqrt((cum[b] - cum[a]) / win) : 0;
            }
            double F0At(double pos) {
                if (t.f0.Length == 0) return 0;
                pos = Math.Clamp(pos, 0, t.f0.Length - 1);
                int i0 = (int)Math.Floor(pos);
                int i1 = Math.Min(i0 + 1, t.f0.Length - 1);
                double w = pos - i0, a0 = t.f0[i0], a1 = t.f0[i1];
                if (a0 > 0 && a1 > 0) return Math.Exp((1 - w) * Math.Log(a0) + w * Math.Log(a1));
                double f = w < 0.5 ? a0 : a1;
                return f > 0 ? f : 0;
            }
            var rms = new double[n];
            for (int j = 0; j < n; j++) {
                double ms = (startFrame + j - head + 0.5) * frameMs;
                vsParams.f0[j] = F0At((ms - t.originMs) / NeutrinoRenderer.TeacherOutput.FrameMs);
                rms[j] = RmsAt((int)Math.Round((ms - t.originMs) / 1000.0 * t.sampleRate));
            }
            var vowelRms = new List<double>();
            int offset = 0;
            foreach (var ph in vsParams.phonemes) {
                if (TeacherVowels.Contains(ph.phoneme)) {
                    for (int j = offset; j < offset + ph.frame_length && j < n; j++) vowelRms.Add(rms[j]);
                }
                offset += ph.frame_length;
            }
            vowelRms.Sort();
            double median = vowelRms.Count > 0 ? vowelRms[vowelRms.Count / 2] : 0;
            if (vowelRms.Count > 0 && vowelRms.Count % 2 == 0) median = (vowelRms[vowelRms.Count / 2 - 1] + vowelRms[vowelRms.Count / 2]) / 2;
            for (int j = 0; j < n; j++) {
                vsParams.volume[j] = median > 0 ? Math.Clamp(rms[j] / median * TeacherVowelVolume, 0, TeacherVolumeMax) : 0;
            }
            return vsParams;
        }

        /// <summary>
        /// Start times (ms) of the morae of a note sung as several VOICEVOX notes. A mora starts where its
        /// vowel (or N / cl) starts in the phrase's phonemes, as VOICEVOX puts consonants before the note;
        /// without such phonemes the note is split evenly.
        /// </summary>
        internal static double[] MoraStarts(RenderPhrase phrase, int noteIndex, int count) {
            var note = phrase.notes[noteIndex];
            int partNoteIndex = phrase.noteIndexes[noteIndex];
            var nuclei = phrase.phones
                .Where(p => p.noteIndex == partNoteIndex && p.phoneme != "pau" && VoicevoxUtils.phoneme_List.vowels.Contains(p.phoneme))
                .Select(p => p.positionMs)
                .ToList();
            return MoraStarts(note.positionMs, note.endMs, nuclei, count);
        }

        internal static double[] MoraStarts(double startMs, double endMs, IList<double> nucleusStartsMs, int count) {
            var starts = new double[count];
            starts[0] = startMs;
            bool fromPhonemes = nucleusStartsMs.Count == count;
            for (int m = 1; m < count && fromPhonemes; m++) {
                starts[m] = nucleusStartsMs[m];
                fromPhonemes = starts[m] > starts[m - 1] && starts[m] < endMs;
            }
            if (!fromPhonemes) {
                for (int m = 1; m < count; m++) {
                    starts[m] = startMs + (endMs - startMs) * m / count;
                }
            }
            return starts;
        }

        private List<VoicevoxNote> BuildVNotes(RenderPhrase phrase) {
            List<VoicevoxNote> vNotes = new List<VoicevoxNote>();
            try {
                for (int i = 0; i < phrase.notes.Length; i++) {
                    var durationMs = phrase.notes[i].durationMs;
                    // The hint goes first: "あっ[a cl]" split by spaces would leave only "cl]".
                    var currentLyric = VoicevoxUtils.StripHint(phrase.notes[i].lyric.Normalize());
                    var lyricList = currentLyric.Split(" ");
                    var shiftTone = phrase.phones[0].toneShift;
                    if (!VoicevoxUtils.IsSyllableVowelExtensionNote(lyricList[^1])) {
                        if (VoicevoxUtils.IsPau(lyricList[^1])) {
                            currentLyric = string.Empty;
                        } else if (VoicevoxUtils.dic.IsDic(lyricList[^1])) {
                            currentLyric = VoicevoxUtils.dic.Lyrictodic(lyricList[^1]);
                        } else if (VoicevoxUtils.IsKana(lyricList[^1])) {
                            currentLyric = lyricList[^1];
                        } else if (VoicevoxUtils.SplitKana(lyricList[^1]) is string[] morae) {
                            // "あっ[a cl]" sings as "あ" + "っ": the engine takes one mora per note.
                            var starts = MoraStarts(phrase, i, morae.Length);
                            for (int m = 0; m < morae.Length; m++) {
                                double end = m + 1 < morae.Length ? starts[m + 1] : phrase.notes[i].endMs;
                                vNotes.Add(new VoicevoxNote() {
                                    lyric = morae[m],
                                    positionMs = starts[m],
                                    durationMs = end - starts[m],
                                    tone = (int)(phrase.notes[i].tone + shiftTone)
                                });
                            }
                            continue;
                        } else if (lyricList[^1] == "ー" && vNotes.Count > 0 && VoicevoxUtils.LongVowelKana(vNotes[^1].lyric) is string vowel) {
                            currentLyric = vowel;
                        } else {
                            currentLyric = string.Empty;
                        }
                    }
                    vNotes.Add(new VoicevoxNote() {
                        lyric = currentLyric,
                        positionMs = phrase.notes[i].positionMs,
                        durationMs = durationMs,
                        tone = (int)(phrase.notes[i].tone + shiftTone)
                    });
                }
            } catch (Exception e) {
                var phonemeText = string.Join(" ", phrase.phones.Select(p => p.phoneme));
                throw new MessageCustomizableException(
                    $"Failed to create a voice base. One or more phonemes may not be supported by the VOICEVOX engine.\n{phonemeText}",
                    $"An error occurred while creating a voice base from the current phrase. This may be caused by unsupported phonemes, invalid input, or a mismatch between phonemes and hiragana.\nPhonemes: {phonemeText}\nDetails: {e.Message}",
                    new VoicevoxException());
            }

            return vNotes;
        }

        /// <summary>NEUTRINO の音素化の句 (句ごとの声) を VOICEVOX で歌う時、VOICEVOX に無い音素 (息継ぎ・無音) を休符にする。</summary>
        static string ToVoicevoxPhoneme(string phoneme) {
            return phoneme == "br" || phoneme == "sil" ? "pau" : phoneme;
        }

        private VoicevoxSynthParams PhonemeToVoicevoxSynthParams(RenderPhrase phrase) {
            VoicevoxSynthParams vsParams = new VoicevoxSynthParams();
            int headFrames = (int)Math.Round((VoicevoxUtils.headS * VoicevoxUtils.fps), MidpointRounding.AwayFromZero);
            int tailFrames = (int)Math.Round((VoicevoxUtils.tailS * VoicevoxUtils.fps), MidpointRounding.AwayFromZero);
            try {
                vsParams.phonemes.Add(new Phonemes() {
                    phoneme = "pau",
                    frame_length = headFrames
                });
                //Holds the end frame of the previous phoneme so that phonemes stay contiguous.
                int cursor = phrase.phones.Length > 0
                    ? VoicevoxUtils.ToFrame(VoicevoxUtils.GetSynthSpan(phrase).startMs)
                    : 0;
                for (int i = 0; i < phrase.phones.Length; i++) {
                    double endMs = phrase.phones[i].positionMs + phrase.phones[i].durationMs;

                    int startFrame = cursor;
                    int endFrame = VoicevoxUtils.ToEndFrame(
                        startFrame,
                        endMs,
                        VoicevoxUtils.IsPlosive(phrase.phones[i].phoneme),
                        VoicevoxUtils.minFrames);

                    int length = endFrame - startFrame;
                    cursor = endFrame;

                    vsParams.phonemes.Add(new Phonemes() {
                        phoneme = ToVoicevoxPhoneme(phrase.phones[i].phoneme),
                        frame_length = length
                    });
                }
                vsParams.phonemes.Add(new Phonemes() {
                    phoneme = "pau",
                    frame_length = tailFrames
                });
            } catch (Exception e) {
                throw new VoicevoxException("Failed to create a voice base.", e);
            }

            int totalFrames = 0;
            vsParams.phonemes.ForEach(x => totalFrames += x.frame_length);

            vsParams.f0 = Enumerable.Repeat(0.0, totalFrames).ToList();
            vsParams.volume = Enumerable.Repeat(0.0, totalFrames).ToList();
            return vsParams;
        }

        public UExpressionDescriptor[] GetSuggestedExpressions(USinger singer, URenderSettings renderSettings) {
            //under development
            var result = new List<UExpressionDescriptor> {
                NeutrinoTeacher.Descriptor(),
                PhraseVoice.Descriptor(),
                //volumes
                new UExpressionDescriptor{
                    name="input volume (curve)",
                    abbr=VOLC,
                    type=UExpressionType.Curve,
                    min=0,
                    max=200,
                    defaultValue=100,
                    isFlag = false,
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
                //phoneme replace mode
                //new UExpressionDescriptor{
                //    name = "phoneme replace mode",
                //    abbr = REPM,
                //    type = UExpressionType.Options,
                //    options = new string[] { VoicevoxUtils.REPLACE, VoicevoxUtils.OVERWRITE},
                //    isFlag = false,
                //},
            };

            return result.ToArray();
        }

        public override string ToString() => Renderers.VOICEVOX;

        RenderPitchResult IRenderer.LoadRenderedPitch(RenderPhrase phrase) {
            try {
                var singer = phrase.singer as VoicevoxSinger;
                if (singer != null) {
                    // TODO: Support Teacher Singer in the future
                    // string baseSingerID = VoicevoxUtils.getBaseSingerID(singer);
                    VoicevoxSynthParams vsParams = PhraseToVoicevoxSynthParams(phrase, phrase.singer as VoicevoxSinger, true);
                    double frameMs = (1000d / VoicevoxUtils.fps);
                    int vvTotalFrames = 0;
                    vsParams.phonemes.ForEach(x => vvTotalFrames += x.frame_length);
                    //vsParams.f0 = ToneShift(phrase, vsParams);
                    vsParams.f0 = vsParams.f0.Select(f0 => f0 = f0 * Math.Pow(2, ((phrase.phones[0].toneShift * -1) / 12d))).ToList();
                    List<double> f0 = vsParams.f0;


                    var exprCurve = phrase.curves.FirstOrDefault(curve => curve.Item1.Equals(SMOC));
                    if (exprCurve != null) {
                        List<int> exprs = VoicevoxUtils.SampleCurve(phrase, exprCurve.Item2, 0, frameMs, vvTotalFrames, vsParams.phonemes[0].frame_length, vsParams.phonemes[^1].frame_length, -(VoicevoxUtils.headS + 10), x => x).Select(x => (int)x).ToList();
                        var f0S = new F0Smoother(f0);
                        f0S.SmoothenWidthList = exprs;
                        f0 = f0S.GetSmoothenedF0List(f0);
                    }

                    var result = new RenderPitchResult {
                        tones = f0.Select(value => (float)MusicMath.FreqToTone(value)).ToArray(),
                        ticks = new float[vvTotalFrames],
                        // The pau padding before and after the phrase is silence; without this mask its
                        // pitch would be written past the phrase end, over the next phrase.
                        voiced = PaddingVoicedMask(vsParams.phonemes.Select(p => p.frame_length).ToList()),
                    };
                    var layout = Layout(phrase);
                    var t = layout.positionMs - layout.leadingMs;
                    for (int i = 0; i < result.tones.Length; i++) {
                        t += (1000d / VoicevoxUtils.fps);
                        result.ticks[i] = phrase.timeAxis.MsPosToTickPos(t) - phrase.position;
                    }
                    return result;
                }
            } catch (Exception e) {
                throw new VoicevoxException("Failed to create pitch data.", e);
            }
            return null;
        }

        /// <summary>
        /// Per-frame voiced flags for a phoneme list that starts and ends with a pau padding:
        /// false for the frames of the first and the last phoneme, true for the rest.
        /// </summary>
        internal static bool[] PaddingVoicedMask(IReadOnlyList<int> phonemeFrameLengths) {
            var mask = new bool[phonemeFrameLengths.Sum()];
            int offset = 0;
            for (int i = 0; i < phonemeFrameLengths.Count; i++) {
                int length = phonemeFrameLengths[i];
                bool padding = i == 0 || i == phonemeFrameLengths.Count - 1;
                Array.Fill(mask, !padding, offset, length);
                offset += length;
            }
            return mask;
        }

        /// <summary>
        /// Mixed into the cache key. Bump it when the same phrase is sent to the engine differently,
        /// so renders made by the old code are not reused (2: lyrics such as "あっ[a cl]" and "づ" no longer become rests).
        /// </summary>
        const int CacheVersion = 2;

        ulong HashPhraseGroups(RenderPhrase phrase) {
            using (var stream = new MemoryStream()) {
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(CacheVersion);
                    writer.Write(phrase.preEffectHash);
                    writer.Write(phrase.phones[0].tone);
                    writer.Write(phrase.phones[0].direct);
                    //phrase.hash does not cover the SHFT expression, so it has to be mixed in here.
                    writer.Write(phrase.phones[0].toneShift);
                    foreach (var phone in phrase.phones) {
                        writer.Write(phone.tone);
                    }
                    writer.Write(phrase.phones[0].volume);
                    return XXH64.DigestOf(stream.ToArray());
                }
            }
        }

    }
}
