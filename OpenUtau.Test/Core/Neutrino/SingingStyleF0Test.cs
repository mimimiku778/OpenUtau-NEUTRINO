using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace OpenUtau.Core.Neutrino {
    /// <summary>
    /// 歌い方の F0 の整え方 (調声の掛け具合・平滑・語尾の上限) が、元にした Python の実装と
    /// 同じ入力で同じ F0 を出すか。Files/SingingStyleF0/python_golden.json は、その script を
    /// 合成の入力 (深い落ち・浅い落ち・ガタつき・短い有声・息継ぎの有声) で実際に回し、frame_synthesis へ送った query の f0 を書いた物。
    /// cents = 調声の差分 (SHAPED ÷ NATIVE) をコマごとに引いた値。
    /// </summary>
    public class SingingStyleF0Test {
        sealed class Golden {
            public double frameHz;
            public List<(string phoneme, int frames)> phonemes = new();
            public double[] f0 = Array.Empty<double>();
            public double[] cents = Array.Empty<double>();
            public List<(string name, float tuning, float smooth, float limit, double[] f0)> cases = new();
            public List<(double hz, double frameHz, double[] sos)> butter = new();
        }

        static readonly Lazy<Golden> golden = new(Load);

        static Golden Load() {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Join(dir, "Files", "SingingStyleF0", "python_golden.json")));
            var root = doc.RootElement;
            double[] Array(JsonElement e) => e.EnumerateArray().Select(x => x.GetDouble()).ToArray();
            var g = new Golden {
                frameHz = root.GetProperty("frameHz").GetDouble(),
                f0 = Array(root.GetProperty("f0")),
                cents = Array(root.GetProperty("cents")),
            };
            foreach (var p in root.GetProperty("phonemes").EnumerateArray()) {
                g.phonemes.Add((p.GetProperty("phoneme").GetString()!, p.GetProperty("frames").GetInt32()));
            }
            foreach (var c in root.GetProperty("cases").EnumerateArray()) {
                g.cases.Add((c.GetProperty("name").GetString()!, (float)c.GetProperty("tuningPercent").GetDouble(),
                    (float)c.GetProperty("smoothHz").GetDouble(), (float)c.GetProperty("tailFallLimitCents").GetDouble(), Array(c.GetProperty("f0"))));
            }
            foreach (var b in root.GetProperty("butter").EnumerateArray()) {
                g.butter.Add((b.GetProperty("hz").GetDouble(), b.GetProperty("frameHz").GetDouble(), Array(b.GetProperty("sos"))));
            }
            return g;
        }

        public static IEnumerable<object[]> CaseNames() => golden.Value.cases.Select(c => new object[] { c.name });

        [Theory]
        [MemberData(nameof(CaseNames))]
        public void Shape_MatchesPython(string name) {
            var g = golden.Value;
            var c = g.cases.Single(x => x.name == name);
            var f0 = g.f0.ToArray();
            SingingStyleF0.Shape(f0, g.cents, new SingingStyleF0.Settings(c.tuning, c.smooth, c.limit), g.frameHz, SingingStyleF0.VowelSpans(g.phonemes));

            Assert.Equal(c.f0.Length, f0.Length);
            double worst = 0;
            for (int i = 0; i < f0.Length; i++) {
                Assert.Equal(c.f0[i] > 0, f0[i] > 0);
                if (f0[i] > 0) {
                    worst = Math.Max(worst, Math.Abs(1200 * Math.Log2(f0[i] / c.f0[i])));
                }
            }
            Assert.True(worst < 1e-6, $"{name}: the largest difference from Python is {worst} cent");
        }

        [Fact]
        public void Shape_CasesChangeTheLine() {
            // 正解の側も入力から十分に動いていること (動かない正解で一致しても意味がない)
            var g = golden.Value;
            foreach (var c in g.cases) {
                double moved = 0;
                for (int i = 0; i < g.f0.Length; i++) {
                    if (g.f0[i] > 0 && c.f0[i] > 0) {
                        moved = Math.Max(moved, Math.Abs(1200 * Math.Log2(c.f0[i] / g.f0[i])));
                    }
                }
                Assert.True(moved > 5, $"{c.name}: Python changed the line by only {moved} cent");
            }
        }

        [Fact]
        public void Butter2Lowpass_MatchesScipy() {
            foreach (var (hz, frameHz, sos) in golden.Value.butter) {
                var mine = SingingStyleF0.Butter2Lowpass(hz / (frameHz / 2));
                for (int i = 0; i < 6; i++) {
                    Assert.True(Math.Abs(mine[i] - sos[i]) <= 1e-12 * Math.Max(1, Math.Abs(sos[i])),
                        $"{hz} Hz at {frameHz} fps, coefficient {i}: {mine[i]} vs scipy {sos[i]}");
                }
            }
        }

        [Fact]
        public void FiltFilt_KeepsAConstantAndARamp() {
            var sos = SingingStyleF0.Butter2Lowpass(12 / (93.75 / 2));
            var flat = Enumerable.Repeat(6900.0, 30).ToArray();
            var ramp = Enumerable.Range(0, 30).Select(i => 6000.0 + 10 * i).ToArray();
            var a = SingingStyleF0.FiltFilt(sos, flat);
            var b = SingingStyleF0.FiltFilt(sos, ramp);
            for (int i = 0; i < 30; i++) {
                Assert.True(Math.Abs(flat[i] - a[i]) < 1e-9, $"flat {i}: {a[i]}");
            }
            // 直線は端の折り返しの過渡が消えた中ほどでそのまま残る (遅れ・ずれが無い)
            for (int i = 10; i < 20; i++) {
                Assert.True(Math.Abs(ramp[i] - b[i]) < 1e-3, $"ramp {i}: {b[i]} vs {ramp[i]}");
            }
        }

        [Fact]
        public void Shape_FullTuningAddsTheDeviationExactlyAsBefore() {
            // 100% (平滑・語尾は切) は、今までの式 f × 2^(cent / 1200) と 1 ビットも違わない
            var g = golden.Value;
            var f0 = g.f0.ToArray();
            SingingStyleF0.Shape(f0, g.cents, new SingingStyleF0.Settings(100, 0, 0), g.frameHz, SingingStyleF0.VowelSpans(g.phonemes));
            for (int i = 0; i < f0.Length; i++) {
                double before = g.f0[i] > 0 ? g.f0[i] * Math.Pow(2, g.cents[i] / 1200.0) : g.f0[i];
                Assert.Equal(BitConverter.DoubleToInt64Bits(before), BitConverter.DoubleToInt64Bits(f0[i]));
            }
        }

        [Fact]
        public void Shape_SmoothingStopsAtBreaks() {
            // 息継ぎのコマで切ると、その前後は別々に均す (息継ぎの低い F0 が歌へ混ざらない)
            int n = 60;
            var f0 = Enumerable.Range(0, n).Select(i => i >= 25 && i < 35 ? 90.0 : 440.0 * (1 + 0.01 * (i % 2))).ToArray();
            var breaks = Enumerable.Range(0, n).Select(i => i >= 25 && i < 35).ToArray();
            var cut = f0.ToArray();
            SingingStyleF0.Shape(cut, new double[n], new SingingStyleF0.Settings(100, 12, 0), 100, null, breaks);
            for (int i = 25; i < 35; i++) {
                Assert.Equal(90.0, cut[i]);
            }
            Assert.InRange(cut[24], 430, 450);
            Assert.InRange(cut[35], 430, 450);
            var joined = f0.ToArray();
            SingingStyleF0.Shape(joined, new double[n], new SingingStyleF0.Settings(100, 12, 0), 100, null);
            Assert.True(joined[24] < 400, "without the break the breath pulls the sung frames down");
        }

        [Fact]
        public void Shape_TailLimitOnlyTouchesDeepFalls() {
            // 母音 20 コマ: 前 12 コマは 440 Hz、後ろ 8 コマで -50c (浅い) / -900c (深い) へ落ちる
            double[] Vowel(double fallCents) => Enumerable.Range(0, 20)
                .Select(i => i < 12 ? 440.0 : 440.0 * Math.Pow(2, fallCents * (i - 11) / 8.0 / 1200)).ToArray();
            var spans = new List<(int, int)> { (0, 20) };
            var shallow = Vowel(-50);
            var before = shallow.ToArray();
            SingingStyleF0.Shape(shallow, new double[20], new SingingStyleF0.Settings(100, 0, 200), 100, spans);
            Assert.Equal(before, shallow);
            var deep = Vowel(-900);
            SingingStyleF0.Shape(deep, new double[20], new SingingStyleF0.Settings(100, 0, 200), 100, spans);
            double last = 1200 * Math.Log2(deep[^1] / 440.0);
            Assert.InRange(last, -200, -190);
        }

        [Fact]
        public void Settings_DefaultAndBandText() {
            Assert.True(SingingStyleF0.Settings.Default.IsDefault);
            Assert.True(new SingingStyleF0.Settings(100, 0, 0).IsDefault);
            Assert.False(new SingingStyleF0.Settings(12.5f, 0, 0).IsDefault);
            Assert.False(new SingingStyleF0.Settings(100, 12, 0).IsDefault);
            Assert.False(new SingingStyleF0.Settings(100, 0, 200).IsDefault);
            Assert.Equal(0.125, new SingingStyleF0.Settings(12.5f, 0, 0).Scale);
            Assert.Equal(0.8, new SingingStyleF0.Settings(80, 0, 0).Scale);
            Assert.Equal("調声 12.5% ・ 平滑 12 Hz ・ 語尾 200c", new SingingStyleF0.Settings(12.5f, 12, 200).Describe());
            Assert.Equal("平滑 12 Hz", new SingingStyleF0.Settings(100, 12, 0).Describe());
        }

        [Fact]
        public void Descriptors_FitTheExpressionColumn() {
            var descriptors = SingingStyleF0.Descriptors();
            Assert.Equal(SingingStyleF0.Abbrs, descriptors.Select(d => d.abbr).ToArray());
            Assert.All(descriptors, d => Assert.True(d.name.Length <= 22, d.name));
            // 小数の値 (12.5%) を落とさないよう、resampler の flag (整数) にはしない
            Assert.All(descriptors, d => Assert.True(string.IsNullOrEmpty(d.flag), d.abbr));
            Assert.All(descriptors, d => Assert.True(Render.PhraseVoice.IsPhraseWide(d.abbr), d.abbr));
            Assert.Equal(new[] { 100f, 0f, 0f }, descriptors.Select(d => d.defaultValue).ToArray());
        }
    }
}
