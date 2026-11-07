# OpenUtau NEUTRINO対応版(非公式)

本家 [OpenUtau](https://github.com/openutau/OpenUtau) に、rokujyushi さんの NEUTRINO 対応と、この版で足した機能を重ねた非公式ビルドです。

![OpenUtau でずんだもん (NEUTRINO) を編集している画面](docs/screenshot.webp)

## 中身

素の OpenUtau に、上から順に重ねています。

1. 本家 [OpenUtau](https://github.com/openutau/OpenUtau)。固定版は 0.1.570.3(2 の NEUTRINO 対応が取り込んだ版)、最新版は 0.1.577.9(本家 `master` の [f920e242](https://github.com/openutau/OpenUtau/commit/f920e2423113f9a936ec77eebf1f192e02a7ea6a)、2026-11-05)。VOICEVOX で歌う機能はここに元からあります。
2. NEUTRINO 対応。rokujyushi さんの PR [openutau/OpenUtau#2136](https://github.com/openutau/OpenUtau/pull/2136)。ソースは [rokujyushi/OpenUtau](https://github.com/rokujyushi/OpenUtau/tree/neutrino) の `neutrino` ブランチ([203c44f5](https://github.com/rokujyushi/OpenUtau/commit/203c44f5d1374697abfa69626a2b52a19627184b))。
3. 画面なしの書き出し(CLI)。christianazinn さんの PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162) を、NEUTRINO 対応版で動くように直した物。
4. この版で足した機能。下の「足した機能」。

## ダウンロード

| | Mac | Windows(動作未確認) |
|---|---|---|
| 固定版(OpenUtau 0.1.570.3) | [dmg](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-macos-arm64.dmg) | [zip](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-win-x64.zip) |
| 最新版(本家 [OpenUtau](https://github.com/openutau/OpenUtau) の最新) | [dmg](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-latest-macos-arm64.dmg) | [zip](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-latest-win-x64.zip) |

固定版は PR のまま、最新版は本家の最新を混ぜたものです(動作保証なし)。入れるのはどちらか一方です。

## 使い方

- 導入は [rokujyushi さんの wiki](https://github.com/rokujyushi/OpenUtau/wiki/OpenUtau%E2%80%90for%E2%80%90NEUTRINO) のとおり(アプリ名は `OpenUtau`)。同梱の登録スクリプトでも入れられます。
- VOICEVOX の声でも歌えます。OpenUtau を開く前に VOICEVOX のアプリを起動しておいてください(アプリの中で動く VOICEVOX エンジンにつないで歌わせるため、OpenUtau の使用中は開いたままにします)。歌手の入れ方は本家の [VOICEVOX support](https://github.com/openutau/OpenUtau/wiki/VOICEVOX-support) にあります。
- 画面なしで書き出し: `OpenUtau render --input x.ustx --output x.wav`。オプションと書き方は [CLI の説明](https://github.com/pika-02-03/OpenUtau-NEUTRINO/blob/release-kit/docs/cli.md) にあります。

## 足した機能

- **v3 の声でも声色と明るさを変えられる(mel 編集)**: 元の PR では、ずんだもん等の NEUTRINO v3 の声に「GENC(声色)」と「TENC(明るさ・強さ)」のカーブが効きません。この版では効くので、声を太く・細くしたり、語尾だけ暗く弱くしたりを OpenUtau の中でできます。
- **direct でも自分で付けたビブラート・ピッチが効く**: direct をオンにすると NEUTRINO の自然な音程のまま歌いますが、元の PR ではそのとき OpenUtau で付けたビブラートや PITD(ピッチのカーブ)が無視されます。この版では NEUTRINO の自然な音程の上に足して鳴らすので、「歌い回しはそのまま、ここだけビブラートやしゃくりを足す」ができます。
- **聞こえる音程がそのまま画面に出る**: 元の PR では、direct の部分でも画面の灰色の線は OpenUtau 側の音程のままで、聞こえる音程と食い違っていました。この版では実際に鳴る音程を線で描くので、線を見ながら PITD をペンで描き足せます。
- **Mac で Shift+ホイールの横スクロール**: Mac では Shift を押しながらホイールを回しても横に動かず縦に動いていたのを直しました。ピアノロールとトラックの画面で横に動きます。
- **画面を開かずに書き出せる(CLI)**: OpenUtau を起動せず、コマンド 1 行で ustx を wav に書き出せます(使い方は上)。たくさんの曲をまとめて書き出したり、ほかのツールから自動で呼んだりできます。本家に出ている未マージの PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162) を、NEUTRINO 対応版で動くように直して入れています。
- **句ごとに声を選び、VOICEVOX の句の音程は NEUTRINO に作らせる(句ごとの声・F0 の先生)**: 1 つのトラックの中で、句(音符が隙間なくつながったまとまり)ごとに NEUTRINO・VOICEVOX の歌手を選べます(音符の表情 `voice (phrase)`)。VOICEVOX が歌う句は、音程を NEUTRINO の歌手に作らせられる(`F0 teacher (NEUTRINO)`)ので、VOICEVOX の声を NEUTRINO の歌い回しで歌わせ、その上に自分のビブラート・PITD を足せます。
- **どの句をどの声と先生で歌っているかが見える**: 声・先生を付けた句には、ピアノロールの上端に句の頭から終わりまでの帯を描き、「声 …・先生 …」と書きます。1 つの音符で選ぶと、その句の音符全部に同じ値が入ります。
- **VOICEVOX の音を 44.1 kHz で受ける**: 24 kHz で受けて OpenUtau 側で上げていたため、8 kHz より上が 10〜18 dB 削れていました。
- **44.1 kHz でない音の読み込みを sinc で変換する**: 48 kHz の伴奏や NEUTRINO の出力などを、線形補間でなく sinc で変換します(48 kHz からだと 16 kHz で −11 dB、18 kHz で −21 dB 削れ、1〜2 標本遅れていたのが直ります)。NAudio が次の版で既定にする設定と同じです。
- **VOICEVOX でカタカナの歌詞も歌う**: かなの表にひらがなしかなく、カタカナの音符は黙って休符になっていました。
- **VOICEVOX で `あっ[a cl]`・`づ`・`ー` も歌う**: 発音ヒント付きの歌詞(`あっ[a cl]` など)・`づ`・`ー`(さー)が黙って休符になり、無音でした。`あっ` は「あ」「っ」の 2 音符に分けて渡し、`ー` は前の音の母音で歌います。

## ソース

ビルドしたソースは、この repo のブランチにそのまま並べてあります(CI が組み立てる物と中身が同じ)。

| | ブランチ | 中身 |
|---|---|---|
| 固定版 | [`neutrino-cli-dist`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/neutrino-cli-dist) | 2 の `neutrino` に、下の表の「固定版」のコミットを足した物 |
| 最新版 | [`neutrino-latest-dist`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/neutrino-latest-dist) | 2 の `neutrino` に本家の `master`([f920e242](https://github.com/openutau/OpenUtau/commit/f920e2423113f9a936ec77eebf1f192e02a7ea6a))をマージし、下の表の「最新版」のコミットを足した物 |

```bash
git clone -b neutrino-cli-dist https://github.com/pika-02-03/OpenUtau-NEUTRINO.git
```

自分で組み立てる時は、rokujyushi さんの `neutrino`(最新版はそこへ本家の `master` をマージした物)に、下の表のコミットを cherry-pick します。当てる順番は [packaging/extra-commits.txt](https://github.com/pika-02-03/OpenUtau-NEUTRINO/blob/release-kit/packaging/extra-commits.txt)(固定版)・[extra-commits-latest.txt](https://github.com/pika-02-03/OpenUtau-NEUTRINO/blob/release-kit/packaging/extra-commits-latest.txt)(最新版)のとおりで、CI も同じ一覧を [apply-extra-commits.sh](https://github.com/pika-02-03/OpenUtau-NEUTRINO/blob/release-kit/packaging/apply-extra-commits.sh) で当てています。

### 足したコミット

| 中身 | 誰の物か | 固定版 | 最新版 | 本家 `master` 向け |
|---|---|---|---|---|
| v3 の声でも声色と明るさを変えられる(mel 編集) | rokujyushi さん([`Editid-Mel-spectrogram`](https://github.com/rokujyushi/OpenUtau/tree/Editid-Mel-spectrogram) ブランチ) | [c5cb0f47](https://github.com/rokujyushi/OpenUtau/commit/c5cb0f47243a012d20c79d73b58d354b9149d7af) | [7b8ebd79](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7b8ebd790b6c5a7f7a91c14c339eb70f136b4d25) |  |
| direct でも自分で付けたビブラート・ピッチが効く | この repo | [7a637f20](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7a637f201e12ad5840cef3371d6fe8fb8fb187e0) | [f6f6ae32](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/f6f6ae32d2e7655638e7817a247ba217676dd7a1) |  |
| 聞こえる音程がそのまま画面に出る | この repo | [7ee7cbc8](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7ee7cbc8e951d3338f1fa45c88394b8cf8b32498) | [ca70f8be](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/ca70f8bedb679aba8f5994904ad2276b9ab7165a) |  |
| Mac で Shift+ホイールの横スクロール | この repo | [4e6c5f7c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/4e6c5f7c3e890da4d003b5d8f39653b74ef7789a) | [502e607c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/502e607cf137ce42fd6a7882581ba20e4a6fca9b) | [`openutau-fix/mac-shift-wheel`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/mac-shift-wheel) [8ae718f2](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/8ae718f25761df64901baa24d631c58a61ee9937) |
| 画面を開かずに書き出せる(CLI) | christianazinn さんの PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162)(コミットはこの repo に写してある) | [003f50ed](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/003f50ede862081ee40e86dbc43c53930eb74ef4) [fd532cd6](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/fd532cd6091d1c21762523d6fcb8adec6fdab69d) [c5a30d1d](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/c5a30d1dc371766cb67a54ec9573ef6085171e01) [75a3d946](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/75a3d9461f090064f0950fafa65a29bee9f41aa7) [3f019bce](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/3f019bce1117f1926fe607cbc2a81cf2c7eb2724) [fb18986b](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/fb18986b35c4a06a6727756dc68d7c41aaaf7358) [a9b35dd1](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/a9b35dd1070470c26565073001fc0ba85c6d6410) [59b1ec0c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/59b1ec0c9a6fbe6d34c2431de682374c4adf6a0a) [e4814b96](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/e4814b9629181e60d6f2478c9180119cdc6c3cf3) | [8c0789a5](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/8c0789a5f65dbf43b82039629d9e4c294adaba0b) [759ea4cd](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/759ea4cd326bd5187b5e68420547b761c4b8c3a2) [d75ba160](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d75ba160d6f53d90bf38485603876ce50f3e42e2) [fdd4a328](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/fdd4a32895b64b1e9f3fd22cdb010757f1e4cd6a) [28c610b0](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/28c610b0e30503cc3a1529f2a35921877d276168) [d7eecf5b](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d7eecf5b40439b3cf7619f903d80e22e50a64963) [77a8ef4c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/77a8ef4c77937e5099871867a70f3b870b391a9d) [4e770fc1](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/4e770fc1caf9feb4bb3aa2f5fe26f0b840490b60) [d239c976](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d239c976455a011777536d781b86a34a999c0267) |  |
| CLI を NEUTRINO 対応版で動くようにする直し | この repo | [d8c3da40](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d8c3da40b535c7bc35108dfcb90e4abea883d345) [adf90893](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/adf90893b133dac65c41846a0effec792f3e4480) | [db870028](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/db870028e686eed23bd7acb09ba476de923e1eb6) [a82b6282](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/a82b6282954f88f7ce62f08b8b748234de04e0a3) |  |
| 句ごとに声を選び、VOICEVOX の句の音程は NEUTRINO に作らせる(句ごとの声・F0 の先生) | この repo | [2f4a4a05](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/2f4a4a0593dab2f8dcad40e79334eb09097c33a2) [c480d923](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/c480d92345a52f8d875a3233e4c35e8745cb4d0d) [22245c92](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/22245c92d4ed60db4f80579e753b007ceeda741b) [093fbb65](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/093fbb65da67f19a98cc864814c05102698efb35) [860f6136](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/860f6136038c73e879ac754dfcc30c6ce1fe15a7) [415833dc](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/415833dc9191032ff9a83d47a75b22bfcd49999d) | [9b8602ec](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/9b8602ec3bdd39e2ea52ee3e896c82f98f6dc91b) [35ea7a76](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/35ea7a7649aa81df7bd3acefd15a5af6646251a0) [74d31ad8](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/74d31ad84cf2b7959a9693a87328fc20ac85e352) [ab84f276](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/ab84f276e236170e4277df634bfd306ed70ad814) [d2c9b23d](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d2c9b23dbc0397f02285cd13d2391cae2d366b79) [53d00d52](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/53d00d52fb370b0a82602e2b96240eef6ae73cb9) |  |
| どの句をどの声と先生で歌っているかが見える | この repo | [c43d181e](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/c43d181e902ed648f308181480b93128fd81b6d7) | [fc1ea2a6](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/fc1ea2a61e76584f6e9004ca7d4ade1cd340f287) |  |
| NEUTRINO v3 の声のトラックでは、2.x でだけ効く表情(engine type・engine mode)を出さない | この repo | [b5d238b2](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/b5d238b2cc9649c8c83fb082e815886ebd05131f) [528f17a2](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/528f17a22f61b500a4f4fc70cd6c94f1838a519d) | [1fe506b8](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/1fe506b8ef62ae48846cbddf360465d411733199) [543453bd](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/543453bd111dd49c40a2b8a00504cf78d82078d7) |  |
| VOICEVOX の音を 44.1 kHz で受ける | この repo | [6692c06c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/6692c06c980f2d9bd2207ef23ba33e115b1cef4c) | [850b263b](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/850b263bad8a9e02674c813b35f33131d0f7b06f) | [`openutau-fix/voicevox-44100`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/voicevox-44100) [76f51a7f](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/76f51a7faed1642bce3b648ec270ee2638a0f35b) |
| 44.1 kHz でない音の読み込みを sinc で変換する | この repo | [620ebbaf](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/620ebbafe0fcc7fe6787fd23d24c0063292d2a70) [f49406d4](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/f49406d4f134f02f7ee30ed7cc885ea117263b8d) [b2893a55](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/b2893a559b437a82d458742dcdd8654f71c319c5) | [2bc39830](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/2bc39830a657d3e082393c81b540afe9832ecefe) [72208832](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/722088326497cda8eed09bc4b8b46932cf755a7b) [9a6c25e1](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/9a6c25e135a22ba95afc4f5b8781a51cd4a430a2) | [`openutau-fix/sinc-resampling`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/sinc-resampling) [c94ffe8a](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/c94ffe8ad1daca46048d878bf49bb5bf00c11ff7) |
| VOICEVOX でカタカナの歌詞も歌う | この repo | [e3acc8aa](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/e3acc8aaa97551fc35e23f804d0473ab886b4782) | [0c03c291](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/0c03c291ab73f07e4b9e12f9cb615edbda4d0208) | [`openutau-fix/voicevox-katakana-lyrics`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/voicevox-katakana-lyrics) [e522d9f0](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/e522d9f09f476796efb00d0d582552dbcb1da546) |
| VOICEVOX で `あっ[a cl]`・`づ`・`ー` も歌う | この repo | [05dcceec](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/05dcceec7be8d13048408f1b16d314442c08d859) | [6d97e184](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/6d97e184b77aa33c81bc381fea2219199f1d0761) |  |

右の列は、「この repo」の直しのうち本家の OpenUtau にもそのまま使える物を、本家 `master`([f920e242](https://github.com/openutau/OpenUtau/commit/f920e2423113f9a936ec77eebf1f192e02a7ea6a))に 1 コミットずつ載せたブランチです(テスト付き)。本家の OpenUtau に cherry-pick すれば、そのまま使えます。

```bash
git fetch https://github.com/pika-02-03/OpenUtau-NEUTRINO.git openutau-fix/sinc-resampling
git cherry-pick FETCH_HEAD
```

## 注意

非公式・無保証です。

この repo で足したコード(README の[ソース](https://github.com/pika-02-03/OpenUtau-NEUTRINO#ソース)で「この repo」と書いた物)は、連絡なしにどう使っても構いません。フォーク・改変・再配布・取り込み、何でも自由で、こちらは関知しません。

本家 OpenUtau や、PR を出している方々のコードは、それぞれの条件に従ってください。OpenUtau は [MIT ライセンス](https://github.com/openutau/OpenUtau/blob/master/LICENSE.txt)、NEUTRINO は[公式の規約](https://studio-neutrino.com/downloads/manual/EULA_NEUTRINO.pdf)です。
