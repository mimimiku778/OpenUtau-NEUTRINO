using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Core.Neutrino {
    /// <summary>
    /// 歌い方: NEUTRINO の歌手が予測した F0 (VOICEVOX では音量も) を、別の声で歌う句に使う (NEUTRINO 本体の -S サポートモデルと同じ働き)。
    /// 音符の選択肢 (abbr f0t) で句ごとに選ぶ。値は句の頭の音素の物を使う。空なら歌い方の指定なし (その声のまま)。
    /// </summary>
    public static class NeutrinoTeacher {
        public const string ABBR = "f0t";
        public const string NAME = "singing style (phrase)";

        // 選択肢型の正式な作り方 (min 0・max = 選択肢の数 - 1)。max が 0 だと選んだ値が 0 に切り詰められて効かない。
        // 選択肢の値は flag にしないと描画器へ届かない (UPhoneme.BuildResamplerFlags)。既定の空文字は flag としても何も足さない。
        public static UExpressionDescriptor Descriptor() => new UExpressionDescriptor(NAME, ABBR, true, Options());

        /// <summary>空 (歌い方の指定なし) と、読み込める NEUTRINO の歌手の Id。</summary>
        public static string[] Options() {
            var ids = new List<string> { string.Empty };
            if (SingerManager.Inst.SingerGroups.TryGetValue(USingerType.Neutrino, out var singers)) {
                ids.AddRange(singers.Select(s => s.Id).OrderBy(id => id, StringComparer.Ordinal));
            }
            return ids.ToArray();
        }

        /// <summary>句に指定された歌い方の歌手。指定なし・見つからない・v3 でない時は null。</summary>
        public static NeutrinoSinger Find(RenderPhrase phrase) {
            if (phrase.phones.Length == 0) {
                return null;
            }
            var flag = phrase.phones[0].flags.FirstOrDefault(f => f.Item3 == ABBR);
            if (flag == null || string.IsNullOrEmpty(flag.Item1)) {
                return null;
            }
            if (!SingerManager.Inst.Singers.TryGetValue(flag.Item1, out var found) || found is not NeutrinoSinger teacher) {
                Log.Warning($"Singing style {flag.Item1} is not an installed NEUTRINO singer; using none.");
                return null;
            }
            teacher.EnsureLoaded();
            if (NeutrinoUtils.DetectVersion(teacher.singerVersion) != NeutrinoVersion.V3) {
                Log.Warning($"Singing style {flag.Item1} is not NEUTRINO v3; using none.");
                return null;
            }
            return teacher;
        }
    }
}
