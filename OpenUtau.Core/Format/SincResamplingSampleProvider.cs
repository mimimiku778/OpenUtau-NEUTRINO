using NAudio.Dsp;
using NAudio.Wave;

namespace OpenUtau.Core.Format {
    /// <summary>
    /// Resamples with WdlResampler in 64-tap windowed sinc mode.
    /// </summary>
    /// <remarks>
    /// NAudio 2.2.1's WdlResamplingSampleProvider is hard-coded to linear interpolation with IIR
    /// filtering, which rolls off the top of the audio band (48 kHz to 44.1 kHz: -11 dB at 16 kHz,
    /// -21 dB at 18 kHz). This is the same provider with the settings NAudio has since made its
    /// default (WdlResamplerQuality.Sinc64, naudio/NAudio#1491): flat to 20 kHz and no added delay.
    /// Once NAudio is updated past that change, WdlResamplingSampleProvider can be used instead.
    /// </remarks>
    public class SincResamplingSampleProvider : ISampleProvider {
        private readonly WdlResampler resampler;
        private readonly ISampleProvider source;
        private readonly int channels;

        public SincResamplingSampleProvider(ISampleProvider source, int newSampleRate) {
            this.source = source;
            channels = source.WaveFormat.Channels;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(newSampleRate, channels);
            resampler = new WdlResampler();
            resampler.SetMode(false, 0, true, 64, 32);
            resampler.SetFeedMode(false); // output driven
            resampler.SetRates(source.WaveFormat.SampleRate, newSampleRate);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count) {
            int framesRequested = count / channels;
            int inNeeded = resampler.ResamplePrepare(framesRequested, channels, out float[] inBuffer, out int inBufferOffset);
            int inAvailable = source.Read(inBuffer, inBufferOffset, inNeeded * channels) / channels;
            int outAvailable = resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, channels);
            return outAvailable * channels;
        }
    }
}
