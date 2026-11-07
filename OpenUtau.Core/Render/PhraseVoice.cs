using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Voicevox;
using Serilog;

namespace OpenUtau.Core.Render {
    /// <summary>
    /// 句ごとの声: 音符がつながっていない句は別々に描くので、その句だけ別の歌手 (NEUTRINO・VOICEVOX) で歌わせる。
    /// 音符の選択肢 (abbr pvo) で選び、値は句の頭の音素の物を使う。空ならトラックの歌手。
    /// 値は歌手の Id、VOICEVOX はスタイルを "Id:スタイル" で添えられる (無ければその歌手の最初のスタイル)。
    /// </summary>
    public static class PhraseVoice {
        public const string ABBR = "pvo";
        const char StyleSeparator = ':';

        // 選択肢型の正式な作り方 (min 0・max = 選択肢の数 - 1)。max が 0 だと選んだ値が 0 に切り詰められて効かない。
        // 選択肢の値は flag にしないと描画器へ届かない (UPhoneme.BuildResamplerFlags)。既定の空文字は何も足さない。
        public static UExpressionDescriptor Descriptor() => new UExpressionDescriptor("voice (phrase)", ABBR, true, Options());

        /// <summary>空 (トラックの歌手) と、NEUTRINO の歌手、VOICEVOX の歌手とそのスタイル。</summary>
        public static string[] Options() {
            var options = new List<string> { string.Empty };
            if (SingerManager.Inst.SingerGroups.TryGetValue(USingerType.Neutrino, out var neutrino)) {
                options.AddRange(neutrino.Select(s => s.Id).OrderBy(id => id, StringComparer.Ordinal));
            }
            if (SingerManager.Inst.SingerGroups.TryGetValue(USingerType.Voicevox, out var voicevox)) {
                bool engineUp = VoicevoxEngineReachable();
                foreach (var singer in voicevox.OrderBy(s => s.Id, StringComparer.Ordinal)) {
                    options.Add(singer.Id);
                    if (singer is not VoicevoxSinger vs) {
                        continue;
                    }
                    if (!vs.Loaded && engineUp) {
                        try {
                            vs.EnsureLoaded();
                        } catch (Exception e) {
                            Log.Warning(e, $"Could not load VOICEVOX singer {vs.Id} to list its styles.");
                        }
                    }
                    var styles = vs.voicevoxConfig?.styles;
                    if (vs.Loaded && styles != null) {
                        options.AddRange(styles.Where(st => st.type == "frame_decode").Select(st => $"{vs.Id}{StyleSeparator}{st.name}"));
                    }
                }
            }
            return options.ToArray();
        }

        /// <summary>
        /// NEUTRINO か VOICEVOX のトラックがあるプロジェクトへ、句ごとの声 (pvo) と歌い方 (f0t) を自動で足す
        /// (表情の窓の「まとめて追加」で使わない物まで入れなくて済むように)。
        /// 既にある時は、選択肢は値の番号で保存されるので並びを変えず、増えた歌手だけ後ろへ足す (refresh の時だけ)。
        /// </summary>
        public static void EnsureExpressions(UProject project, bool refresh) {
            if (project?.tracks == null) {
                return;
            }
            if (!refresh && project.expressions.ContainsKey(ABBR) && project.expressions.ContainsKey(Neutrino.NeutrinoTeacher.ABBR)) {
                return;
            }
            bool uses = project.tracks.Any(t => {
                string r = t.RendererSettings?.renderer;
                var type = t.Singer?.SingerType;
                return r == Renderers.NEUTRINO || r == Renderers.VOICEVOX
                    || type == USingerType.Neutrino || type == USingerType.Voicevox;
            });
            if (!uses) {
                return;
            }
            foreach (var descriptor in new[] { Descriptor(), Neutrino.NeutrinoTeacher.Descriptor() }) {
                if (!project.expressions.TryGetValue(descriptor.abbr, out var existing)) {
                    project.expressions[descriptor.abbr] = descriptor;
                    continue;
                }
                // 表示名を変えた時 (F0 teacher → singing style) も、保存済みのプロジェクトを開けば今の名前になる
                existing.name = descriptor.name;
                if (existing.type != UExpressionType.Options || existing.options == null) {
                    continue;
                }
                if (refresh) {
                    var added = descriptor.options.Where(o => !existing.options.Contains(o)).ToArray();
                    if (added.Length > 0) {
                        existing.options = existing.options.Concat(added).ToArray();
                    }
                }
                // 以前の版で max = 0 のまま保存された物も、選択肢の数に合わせて直す (値が 0 に切り詰められないように)
                existing.min = 0;
                existing.max = existing.options.Length - 1;
            }
        }

        /// <summary>句全体に効く表情 (句ごとの声・歌い方)。値は句の頭の音符の物が効く。</summary>
        public static bool IsPhraseWide(string abbr) => abbr == ABBR || abbr == Neutrino.NeutrinoTeacher.ABBR;

        /// <summary>
        /// 渡した音符と、それらが入っている句 (音符が隙間なくつながったまとまり) の音符全部。
        /// 句全体に効く表情は句の全音符に同じ値を入れ、どの音符を選んでも効いている値が見えるようにする。
        /// </summary>
        public static HashSet<UNote> NotesInSamePhrases(UVoicePart part, IEnumerable<UNote> notes) {
            var result = new HashSet<UNote>(notes);
            if (part == null) {
                return result;
            }
            lock (part) {
                var starts = new HashSet<int>(result.Select(n => part.position + n.position));
                foreach (var phrase in part.renderPhrases) {
                    var inPhrase = new HashSet<int>(phrase.notes.Select(rn => phrase.position + rn.position));
                    if (!inPhrase.Overlaps(starts)) {
                        continue;
                    }
                    foreach (var note in part.notes) {
                        if (inPhrase.Contains(part.position + note.position)) {
                            result.Add(note);
                        }
                    }
                }
            }
            return result;
        }

        static bool VoicevoxEngineReachable() {
            try {
                using var client = new TcpClient();
                return client.ConnectAsync("127.0.0.1", 50021).Wait(200) && client.Connected;
            } catch {
                return false;
            }
        }

        /// <summary>句の頭の音素の flag から、その句を歌う歌手・描画器・(VOICEVOX の) スタイル。指定なし・使えない時は false。</summary>
        public static bool TryResolve(Tuple<string, int?, string>[] flags, out USinger singer, out IRenderer renderer, out string style) {
            singer = null;
            renderer = null;
            style = null;
            var flag = flags?.FirstOrDefault(f => f.Item3 == ABBR);
            if (flag == null || string.IsNullOrEmpty(flag.Item1)) {
                return false;
            }
            string id = flag.Item1;
            if (!SingerManager.Inst.Singers.TryGetValue(id, out singer)) {
                int sep = id.LastIndexOf(StyleSeparator);
                if (sep > 0 && SingerManager.Inst.Singers.TryGetValue(id.Substring(0, sep), out singer)) {
                    style = id.Substring(sep + 1);
                }
            }
            if (singer == null) {
                Log.Warning($"Phrase voice {id} is not an installed singer; using the track's singer.");
                return false;
            }
            string rendererName = singer.SingerType switch {
                USingerType.Neutrino => Renderers.NEUTRINO,
                USingerType.Voicevox => Renderers.VOICEVOX,
                _ => null,
            };
            if (rendererName == null) {
                Log.Warning($"Phrase voice {id} is neither NEUTRINO nor VOICEVOX; using the track's singer.");
                singer = null;
                return false;
            }
            try {
                singer.EnsureLoaded();
            } catch (Exception e) {
                Log.Warning(e, $"Could not load phrase voice {id}; using the track's singer.");
            }
            if (!singer.Loaded) {
                singer = null;
                style = null;
                return false;
            }
            renderer = Renderers.GetOrCreate(rendererName);
            return true;
        }
    }
}
