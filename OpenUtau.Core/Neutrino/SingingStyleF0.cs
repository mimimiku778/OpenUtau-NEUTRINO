using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Neutrino {
    /// <summary>
    /// 歌い方の F0 の整え方。direct の句で鳴らす F0 (NEUTRINO が出した F0 + 音符のビブラート + PITD) を、句ごとの数値の表情 3 つで
    /// 足す → 平滑 → 語尾 の順で整える。
    ///   f0a 調声の掛け具合 (%)   : ビブラート + PITD をこの割合で足す。100 = 今と同じ。
    ///   f0s 平滑 (Hz)            : 有声のつながり (13 コマ以上) ごとに、高さ (cent) のこの Hz より速い成分を取る
    ///                              (2 次 Butterworth の前後往復)。足した調声ごと均す。0 = 切。
    ///   f0l 語尾の落ちの上限 (cent): 母音の後ろ 40% の落ち込みを、母音の中央値比この cent へ tanh で漸近させる
    ///                              (浅い落ちはほぼそのまま)。0 = 切。
    /// 値は句の頭の音素の物が句全体に効く。3 つとも既定なら呼び出し側は今までの式をそのまま使う (音も線も 1 ビットも変わらない)。
    /// 効くのは direct の句: NEUTRINO の句と、歌い方 (NEUTRINO) を選んだ VOICEVOX の句。
    /// </summary>
    public static class SingingStyleF0 {
        public const string TUNING = "f0a";
        public const string SMOOTH = "f0s";
        public const string TAIL_LIMIT = "f0l";

        public static readonly string[] Abbrs = { TUNING, SMOOTH, TAIL_LIMIT };

        /// <summary>語尾の上限を掛ける母音。</summary>
        public static readonly HashSet<string> Vowels = new HashSet<string> { "a", "i", "u", "e", "o", "N" };

        /// <summary>平滑を掛ける有声のつながりの最短 (この数より長い所だけ。前後往復の端の折り返し 9 コマより長くする)。</summary>
        const int MinSmoothRun = 12;

        public static UExpressionDescriptor[] Descriptors() => new[] {
            new UExpressionDescriptor("tuning % (phrase)", TUNING, 0, 200, 100),
            new UExpressionDescriptor("smooth Hz (phrase)", SMOOTH, 0, 40, 0),
            new UExpressionDescriptor("tail limit c (phrase)", TAIL_LIMIT, 0, 1200, 0),
        };

        public static bool IsStyleAbbr(string abbr) => abbr == TUNING || abbr == SMOOTH || abbr == TAIL_LIMIT;

        /// <summary>句の F0 の整え方 (句の頭の音素の値)。</summary>
        public sealed class Settings {
            public static readonly Settings Default = new Settings(100, 0, 0);

            /// <summary>調声 (ビブラート + PITD) の掛け具合 (%)。</summary>
            public readonly float tuningPercent;
            /// <summary>平滑の Hz。0 以下は切。</summary>
            public readonly float smoothHz;
            /// <summary>語尾の落ちの上限 (cent)。0 以下は切。</summary>
            public readonly float tailLimitCents;

            public Settings(float tuningPercent, float smoothHz, float tailLimitCents) {
                this.tuningPercent = tuningPercent;
                this.smoothHz = smoothHz;
                this.tailLimitCents = tailLimitCents;
            }

            public bool IsDefault => tuningPercent == 100 && !(smoothHz > 0) && !(tailLimitCents > 0);

            /// <summary>調声に掛ける倍率 (12.5% → 0.125)。</summary>
            public double Scale => tuningPercent / 100.0;

            /// <summary>cache の鍵へ混ぜる。既定の時は呼ばない (今までの鍵のまま)。</summary>
            public void Write(BinaryWriter writer) {
                writer.Write("singing-style-f0");
                writer.Write(tuningPercent);
                writer.Write(smoothHz);
                writer.Write(tailLimitCents);
            }

            /// <summary>帯に書く中身 (既定の物は書かない)。</summary>
            public string Describe() {
                var parts = new List<string>();
                if (tuningPercent != 100) {
                    parts.Add($"調声 {tuningPercent.ToString("0.###", CultureInfo.InvariantCulture)}%");
                }
                if (smoothHz > 0) {
                    parts.Add($"平滑 {smoothHz.ToString("0.###", CultureInfo.InvariantCulture)} Hz");
                }
                if (tailLimitCents > 0) {
                    parts.Add($"語尾 {tailLimitCents.ToString("0.###", CultureInfo.InvariantCulture)}c");
                }
                return string.Join(" ・ ", parts);
            }

            public override string ToString() => $"{tuningPercent.ToString(CultureInfo.InvariantCulture)}%/{smoothHz.ToString(CultureInfo.InvariantCulture)}Hz/{tailLimitCents.ToString(CultureInfo.InvariantCulture)}c";
        }

        /// <summary>音素の 3 つの表情の値。プロジェクトに表情が無ければ既定。</summary>
        public static Settings Read(UPhoneme phoneme, UProject project, UTrack track) {
            float Get(string abbr, float neutral) =>
                track.TryGetExpDescriptor(project, abbr, out _) ? phoneme.GetExpression(project, track, abbr).Item1 : neutral;
            var settings = new Settings(Get(TUNING, 100), Get(SMOOTH, 0), Get(TAIL_LIMIT, 0));
            return settings.IsDefault ? Settings.Default : settings;
        }

        /// <summary>
        /// f0 (Hz、0 以下 = 無声) をその場で整える。cents = 音符のビブラート + PITD (コマごと)、frameHz = コマの速さ、
        /// vowels = 母音のコマの範囲 [start, end)、smoothBreaks = 平滑のつながりを切るコマ (null なら有声かどうかだけで決める)。
        /// </summary>
        public static void Shape(double[] f0, IReadOnlyList<double> cents, Settings settings, double frameHz,
                IEnumerable<(int start, int end)> vowels, bool[] smoothBreaks = null) {
            int n = f0.Length;
            // 1. 調声を掛け具合だけ足す (有声のコマだけ)
            double scale = settings.Scale;
            for (int i = 0; i < n; i++) {
                if (f0[i] > 0) {
                    f0[i] = f0[i] * Math.Pow(2, scale * cents[i] / 1200.0);
                }
            }
            // 2. 有声のつながりごとに、高さ (cent) の速い成分を取る
            if (settings.smoothHz > 0 && settings.smoothHz < frameHz / 2) {
                var sos = Butter2Lowpass(settings.smoothHz / (frameHz / 2));
                bool Smoothable(int i) => f0[i] > 0 && (smoothBreaks == null || i >= smoothBreaks.Length || !smoothBreaks[i]);
                int k = 0;
                while (k < n) {
                    if (!Smoothable(k)) {
                        k++;
                        continue;
                    }
                    int r0 = k;
                    while (k < n && Smoothable(k)) {
                        k++;
                    }
                    if (k - r0 > MinSmoothRun) {
                        var c = new double[k - r0];
                        for (int j = 0; j < c.Length; j++) {
                            c[j] = 1200 * Math.Log2(f0[r0 + j]);
                        }
                        var y = FiltFilt(sos, c);
                        for (int j = 0; j < c.Length; j++) {
                            f0[r0 + j] = Math.Pow(2, y[j] / 1200);
                        }
                    }
                }
            }
            // 3. 母音の後ろ 40% の深い落ちを、中央値比 C cent へ tanh で漸近させる
            if (settings.tailLimitCents > 0 && vowels != null) {
                double limit = settings.tailLimitCents;
                foreach (var (start, end) in vowels) {
                    int s0 = Math.Clamp(start, 0, n), e0 = Math.Clamp(end, 0, n);
                    if (e0 - s0 <= 4) {
                        continue;
                    }
                    var voiced = new List<double>();
                    for (int i = s0; i < e0; i++) {
                        if (f0[i] > 0) {
                            voiced.Add(f0[i]);
                        }
                    }
                    if (voiced.Count <= 3) {
                        continue;
                    }
                    double med = Median(voiced);
                    int ts0 = s0 + (int)(0.6 * (e0 - s0));
                    var c = new double[e0 - ts0];
                    double min = double.PositiveInfinity;
                    for (int j = 0; j < c.Length; j++) {
                        double v = f0[ts0 + j];
                        c[j] = v > 0 ? 1200 * Math.Log2(Math.Max(v, 1e-6) / med) : 0.0;
                        min = Math.Min(min, c[j]);
                    }
                    if (min < -0.5 * limit) {
                        for (int j = 0; j < c.Length; j++) {
                            if (f0[ts0 + j] > 0) {
                                double cl = c[j] < 0 ? -limit * Math.Tanh(-c[j] / limit) : c[j];
                                f0[ts0 + j] = med * Math.Pow(2, cl / 1200);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>音素の並び (名前とコマ数) から母音のコマの範囲。</summary>
        public static List<(int start, int end)> VowelSpans(IEnumerable<(string phoneme, int frames)> phonemes) {
            var spans = new List<(int, int)>();
            int offset = 0;
            foreach (var (phoneme, frames) in phonemes) {
                if (Vowels.Contains(phoneme)) {
                    spans.Add((offset, offset + frames));
                }
                offset += frames;
            }
            return spans;
        }

        static double Median(List<double> values) {
            values.Sort();
            int m = values.Count / 2;
            return values.Count % 2 == 1 ? values[m] : (values[m - 1] + values[m]) / 2;
        }

        /// <summary>
        /// scipy.signal.butter(2, wn, output="sos") の 1 段 [b0, b1, b2, 1, a1, a2] (wn はナイキスト比)。
        /// scipy と同じ道筋: アナログの極 → 周波数の前ゆがみ → 双一次変換。
        /// </summary>
        internal static double[] Butter2Lowpass(double wn) {
            const double fs = 2.0;
            double warped = 2 * fs * Math.Tan(Math.PI * wn / fs);
            // buttap(2): p = -exp(j π m / 4), m = -1, 1
            var poles = new[] { -Complex.Exp(new Complex(0, Math.PI * -1 / 4)), -Complex.Exp(new Complex(0, Math.PI * 1 / 4)) };
            for (int i = 0; i < poles.Length; i++) {
                poles[i] = warped * poles[i];
            }
            double k = warped * warped;
            const double fs2 = 2.0 * fs;
            var z = new Complex[poles.Length];
            Complex prod = Complex.One;
            for (int i = 0; i < poles.Length; i++) {
                z[i] = (fs2 + poles[i]) / (fs2 - poles[i]);
                prod *= fs2 - poles[i];
            }
            double kz = k * (Complex.One / prod).Real;
            // 共役の組は虚部が正の方を代表にする (scipy の _cplxreal)
            var p1 = z[0].Imaginary > 0 ? z[0] : z[1];
            var p2 = Complex.Conjugate(p1);
            double a1 = (-p2 - p1).Real;
            double a2 = (p1 * p2).Real;
            return new[] { kz, 2 * kz, kz, 1.0, a1, a2 };
        }

        /// <summary>scipy.signal.sosfiltfilt (1 段、padtype="odd"、padlen = 9) と同じ前後往復。x は 9 コマより長いこと。</summary>
        internal static double[] FiltFilt(double[] sos, double[] x) {
            const int edge = 9;
            int n = x.Length;
            if (n <= edge) {
                throw new ArgumentException($"FiltFilt needs more than {edge} samples, got {n}.");
            }
            double b0 = sos[0], b1 = sos[1], b2 = sos[2], a1 = sos[4], a2 = sos[5];
            // sosfilt_zi (lfilter_zi): 定常の内部状態
            double B0 = b1 - a1 * b0, B1 = b2 - a2 * b0;
            double zi0 = (B0 + B1) / (1 + a1 + a2);
            double zi1 = B1 - a2 * zi0;
            var ext = new double[n + 2 * edge];
            for (int i = 0; i < edge; i++) {
                ext[i] = 2 * x[0] - x[edge - i];
                ext[edge + n + i] = 2 * x[n - 1] - x[n - 2 - i];
            }
            Array.Copy(x, 0, ext, edge, n);
            var y = Filter(ext, b0, b1, b2, a1, a2, zi0 * ext[0], zi1 * ext[0]);
            Array.Reverse(y);
            y = Filter(y, b0, b1, b2, a1, a2, zi0 * y[0], zi1 * y[0]);
            Array.Reverse(y);
            var result = new double[n];
            Array.Copy(y, edge, result, 0, n);
            return result;
        }

        /// <summary>sosfilt の 1 段 (直接形 II 転置)。</summary>
        static double[] Filter(double[] x, double b0, double b1, double b2, double a1, double a2, double z0, double z1) {
            var y = new double[x.Length];
            for (int i = 0; i < x.Length; i++) {
                double xi = x[i];
                double yi = b0 * xi + z0;
                z0 = b1 * xi - a1 * yi + z1;
                z1 = b2 * xi - a2 * yi;
                y[i] = yi;
            }
            return y;
        }
    }
}
