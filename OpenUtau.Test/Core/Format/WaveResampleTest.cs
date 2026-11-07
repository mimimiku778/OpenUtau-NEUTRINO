using System;
using System.Linq;
using NAudio.Wave;
using Xunit;

namespace OpenUtau.Core.Format {
    public class WaveResampleTest {
        static WaveStream Sine(int rate, double freq, double seconds, int channels = 1, int silentChannel = -1) {
            int frames = (int)(rate * seconds);
            var x = new float[frames * channels];
            for (int i = 0; i < frames; i++) {
                for (int c = 0; c < channels; c++) {
                    x[i * channels + c] = c == silentChannel ? 0 : (float)(0.5 * Math.Sin(2 * Math.PI * freq * i / rate));
                }
            }
            var bytes = new byte[x.Length * sizeof(float)];
            Buffer.BlockCopy(x, 0, bytes, 0, bytes.Length);
            return new RawSourceWaveStream(bytes, 0, bytes.Length, WaveFormat.CreateIeeeFloatWaveFormat(rate, channels));
        }

        // RMS of one channel relative to the 0.5 amplitude input, away from the edges.
        static double LevelDb(float[] x, int channels = 1, int channel = 0) {
            int frames = x.Length / channels;
            var mid = Enumerable.Range(frames / 4, frames / 2).Select(i => (double)x[i * channels + channel]);
            return 10 * Math.Log10(mid.Average(v => v * v) / 0.125 + 1e-20);
        }

        [Theory]
        [InlineData(48000, 1000)]
        [InlineData(48000, 10000)]
        [InlineData(48000, 18000)]
        [InlineData(24000, 10000)]
        [InlineData(22050, 9000)]
        public void KeepsTheLevelInThePassBand(int rate, double freq) {
            using var stream = Sine(rate, freq, 1.0);
            var y = Wave.GetSamples(stream.ToSampleProvider());
            Assert.InRange(y.Length, 44099, 44101);
            Assert.InRange(LevelDb(y), -0.5, 0.5);
        }

        [Fact]
        public void KeepsTheTiming() {
            using var stream = Sine(48000, 1000, 1.0);
            var y = Wave.GetSamples(stream.ToSampleProvider());
            // Fit the 1 kHz sine on the 44.1 kHz timeline; its phase gives the delay.
            double s = 0, c = 0;
            for (int i = 4410; i < 39690; i++) {
                s += y[i] * Math.Sin(2 * Math.PI * 1000 * i / 44100);
                c += y[i] * Math.Cos(2 * Math.PI * 1000 * i / 44100);
            }
            double delaySamples = -Math.Atan2(c, s) / (2 * Math.PI * 1000) * 44100;
            Assert.InRange(delaySamples, -0.1, 0.1);
        }

        [Fact]
        public void KeepsStereoChannelsApart() {
            using var stream = Sine(48000, 10000, 1.0, channels: 2, silentChannel: 1);
            var y = Wave.GetStereoSamples(stream);
            Assert.InRange(LevelDb(y, 2, 0), -0.5, 0.5);
            Assert.True(LevelDb(y, 2, 1) < -150);
        }

        [Fact]
        public void LeavesTheSameRateUntouched() {
            using var stream = Sine(44100, 1000, 0.1);
            var y = Wave.GetSamples(stream.ToSampleProvider());
            Assert.Equal(4410, y.Length);
            for (int i = 0; i < y.Length; i++) {
                Assert.Equal((float)(0.5 * Math.Sin(2 * Math.PI * 1000 * i / 44100)), y[i]);
            }
        }
    }
}
