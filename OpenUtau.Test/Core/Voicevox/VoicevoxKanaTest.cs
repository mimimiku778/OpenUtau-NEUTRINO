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
        [InlineData("づ", "う")]
        [InlineData("ヅ", "う")]
        [InlineData("ぢ", "い")]
        public void DzuAndDziAreKana(string lyric, string vowel) {
            Assert.True(VoicevoxUtils.IsKana(lyric));
            Assert.Equal(vowel, VoicevoxUtils.phoneme_List.kanas[lyric]);
        }

        [Theory]
        [InlineData("あっ[a cl]", "あ っ")]
        [InlineData("がっ[g a cl]", "が っ")]
        [InlineData("レッ[r e cl]", "レ ッ")]
        [InlineData("きゃっ", "きゃ っ")]
        [InlineData("あ[a]", "あ")]
        public void SplitKanaGivesOneMoraPerNote(string lyric, string morae) {
            Assert.Equal(morae.Split(' '), VoicevoxUtils.SplitKana(lyric));
        }

        [Theory]
        [InlineData("あっ[a cl]", "あっ")]
        [InlineData("か[k a]", "か")]
        [InlineData("か", "か")]
        [InlineData("[a]", "[a]")]
        public void StripHintDropsTheHintWithItsSpaces(string lyric, string stripped) {
            Assert.Equal(stripped, VoicevoxUtils.StripHint(lyric));
        }

        [Theory]
        [InlineData("さ", "あ")]
        [InlineData("ディ", "い")]
        [InlineData("ン", "ん")]
        public void LongVowelMarkSingsThePreviousVowel(string previous, string kana) {
            Assert.Equal(kana, VoicevoxUtils.LongVowelKana(previous));
        }

        [Theory]
        [InlineData("")]
        [InlineData("ka")]
        public void LongVowelMarkAfterOtherTextStaysARest(string previous) {
            Assert.Null(VoicevoxUtils.LongVowelKana(previous));
        }

        [Theory]
        [InlineData("ka")]
        [InlineData("あka")]
        [InlineData("[a cl]")]
        [InlineData("")]
        public void SplitKanaRejectsOtherText(string lyric) {
            Assert.Null(VoicevoxUtils.SplitKana(lyric));
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
