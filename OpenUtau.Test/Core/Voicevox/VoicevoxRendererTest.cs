using Xunit;

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxRendererTest {
        [Fact]
        public void MoraStarts_SplitsWhereTheSecondMoraStarts() {
            // "あっ[a cl]": a at 1000 ms, cl at 1180 ms, note ends at 1250 ms.
            var starts = VoicevoxRenderer.MoraStarts(1000, 1250, new double[] { 1000, 1180 }, 2);

            Assert.Equal(new double[] { 1000, 1180 }, starts);
        }

        [Fact]
        public void MoraStarts_WithoutPhonemesSplitsEvenly() {
            var starts = VoicevoxRenderer.MoraStarts(1000, 1250, new double[] { 1000 }, 2);

            Assert.Equal(new double[] { 1000, 1125 }, starts);
        }

        [Fact]
        public void MoraStarts_PhonemeOutsideTheNoteSplitsEvenly() {
            var starts = VoicevoxRenderer.MoraStarts(1000, 1250, new double[] { 1000, 1300 }, 2);

            Assert.Equal(new double[] { 1000, 1125 }, starts);
        }
    }
}
