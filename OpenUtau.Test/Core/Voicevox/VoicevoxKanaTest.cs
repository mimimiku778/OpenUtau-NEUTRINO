using Xunit;

namespace OpenUtau.Core.Voicevox {
    public class VoicevoxKanaTest {
        [Theory]
        [InlineData("メ", "え")]
        [InlineData("ロ", "お")]
        [InlineData("ディ", "い")]
        [InlineData("ツ", "う")]
        [InlineData("ゴ", "お")]
        [InlineData("ン", "ん")]
        [InlineData("ッ", "っ")]
        [InlineData("ヴァ", "あ")]
        [InlineData("ヮ", "あ")]
        public void KatakanaLyricsAreKana(string lyric, string vowel) {
            Assert.True(VoicevoxUtils.IsKana(lyric));
            Assert.Equal(vowel, VoicevoxUtils.phoneme_List.kanas[lyric]);
        }

        [Theory]
        [InlineData("め", "え")]
        [InlineData("でぃ", "い")]
        public void HiraganaLyricsAreUnchanged(string lyric, string vowel) {
            Assert.True(VoicevoxUtils.IsKana(lyric));
            Assert.Equal(vowel, VoicevoxUtils.phoneme_List.kanas[lyric]);
        }

        [Theory]
        [InlineData("ka")]
        [InlineData("R")]
        [InlineData("ー")]
        public void OtherTextIsNotKana(string lyric) {
            Assert.False(VoicevoxUtils.IsKana(lyric));
        }
    }
}
