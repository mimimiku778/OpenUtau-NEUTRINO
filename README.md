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

- **v3 の声でも声色と明るさを変えられる(mel 編集)** [7b8ebd79](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7b8ebd790b6c5a7f7a91c14c339eb70f136b4d25): 元の PR では、ずんだもん等の NEUTRINO v3 の声に、ピアノロール下のカーブ `GENC`(声色)・`TENC`(明るさ・強さ)が効きません。この版では効きます。値は本家 OpenUtau と同じく −100〜100(0 で変化なし)、向きも本家の決まりどおりです(GENC 100 で声の山を 12 半音下げる)。効きが強いので少しずつ動かしてください。
  - **GENC**: 声の高さはそのままで、声の山(フォルマント)だけを動かします。+ で下げて太く低い声、− で上げて細く若い声。実測で +20 は ×0.86(−2.6 半音)、+40 は ×0.76(−4.7 半音)。目安は ±10〜20 から。+ に大きくすると、声の一番低い音より下(200 Hz 付近)にも音が出てきます(+40 で約 +15 dB、図の左端)。
  - **TENC**: + で大きく明るく、− で小さく暗く。全体の音量と、1 kHz を軸にした高い方の傾きが一緒に動きます。実測で +100 は音量 +7.3 dB・高い方 +3.2 dB/oct、+50 はその半分。目安は ±20〜50 から。
  - 1 枚目はカーブを描く所(下の `GENC` の欄)。2 枚目は、同じ句「ずんだもちたべたい」を値だけ変えて書き出し、句全体の平均スペクトルを重ねた実測です。

  <img src="docs/mel-genc.webp" alt="v3 の声でも声色と明るさを変えられる(mel 編集)">

- **direct でも自分で付けたビブラート・ピッチが効く** [f6f6ae32](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/f6f6ae32d2e7655638e7817a247ba217676dd7a1): direct をオンにすると NEUTRINO の自然な音程のまま歌いますが、元の PR ではそのとき OpenUtau で付けたビブラートや PITD(ピッチのカーブ)が無視されます。この版では NEUTRINO の自然な音程の上に足して鳴らすので、「歌い回しはそのまま、ここだけビブラートやしゃくりを足す」ができます。
- **聞こえる音程がそのまま画面に出る** [ca70f8be](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/ca70f8bedb679aba8f5994904ad2276b9ab7165a): 元の PR では、direct の部分でも画面の灰色の線は OpenUtau 側の音程のままで、聞こえる音程と食い違っていました。この版では実際に鳴る音程を線で描くので、線を見ながら PITD をペンで描き足せます。

  <img src="docs/pitch-line.webp" alt="聞こえる音程がそのまま画面に出る">

- **Mac で Shift+ホイールの横スクロール** [502e607c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/502e607cf137ce42fd6a7882581ba20e4a6fca9b): Mac では Shift を押しながらホイールを回しても横に動かず縦に動いていたのを直しました。ピアノロールとトラックの画面で横に動きます。
- **画面を開かずに書き出せる(CLI)** [8c0789a5](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/8c0789a5f65dbf43b82039629d9e4c294adaba0b) [759ea4cd](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/759ea4cd326bd5187b5e68420547b761c4b8c3a2) [d75ba160](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d75ba160d6f53d90bf38485603876ce50f3e42e2) [fdd4a328](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/fdd4a32895b64b1e9f3fd22cdb010757f1e4cd6a) [28c610b0](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/28c610b0e30503cc3a1529f2a35921877d276168) [d7eecf5b](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d7eecf5b40439b3cf7619f903d80e22e50a64963) [77a8ef4c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/77a8ef4c77937e5099871867a70f3b870b391a9d) [4e770fc1](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/4e770fc1caf9feb4bb3aa2f5fe26f0b840490b60) [d239c976](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d239c976455a011777536d781b86a34a999c0267): OpenUtau を起動せず、コマンド 1 行で ustx を wav に書き出せます(使い方は上)。たくさんの曲をまとめて書き出したり、ほかのツールから自動で呼んだりできます。本家に出ている未マージの PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162) を、NEUTRINO 対応版で動くように直して入れています。
- **句ごとに声と歌い方を選べる(VOICEVOX の声を NEUTRINO の歌い方で歌わせる)** [59140887](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/59140887d54937c14a8c97eec0546ad8a9bced65) [b7be2600](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/b7be2600524443e90996255d01209836a5d318b8): 1 つのトラックの中で、句(音符が隙間なくつながったまとまり)ごとに、歌う声と歌い方を別々に選べます。
  - **設定する所**: 句の音符を 1 つ選び、右の「音符のプロパティ」→「表情」で `voice (phrase)`(歌う声)と `singing style (phrase)`(歌い方)を選ぶ。1 つの音符で選べば、その句の音符全部に入ります。
  - **歌い方**は、音程の線(VOICEVOX では音量も)を別の NEUTRINO の歌手から取ります。たとえば VOICEVOX のずんだもんの声を、NEUTRINO の KIRITAN の歌い方で歌わせられます(NEUTRINO 本体のサポートモデル `-S` と同じ働き)。その上に自分のビブラート・PITD も足せます。
  - 図は、句を VOICEVOX のずんだもん+KIRITAN の歌い方にした画面。白い線が実際に鳴る音程、上の青い帯が句の範囲と選んだ声・歌い方です。

  <img src="docs/phrase-voice.webp" alt="句ごとに声と歌い方を選べる(VOICEVOX の声を NEUTRINO の歌い方で歌わせる)">

- **どの句をどの声と歌い方で歌っているかが見える** [c09a9115](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/c09a9115566e52c7cf53ea77d0bc4f486b07eaa8): 声・歌い方を付けた句には、ピアノロールの上端に句の頭から終わりまでの帯を描き、「声 …・歌い方 …」と書きます。1 つの音符で選ぶと、その句の音符全部に同じ値が入ります。
- **歌い方の効き具合を句ごとに整えられる(調声の量・線の均し・語尾の落ち)** [a4fad1eb](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/a4fad1eb7401a3580e7447421ae9ccd4a885ea97): NEUTRINO の direct の句と、歌い方を付けた VOICEVOX の句で、実際に鳴る音程の線を句ごとに整えられます。既定のままなら今までと同じ音です。
  - **設定する所**: 句の音符を 1 つ選び、右の「音符のプロパティ」→「表情」の 3 つに数字を入れる(その句の音符全部に入ります)。上の帯にも「調声 50% ・ 平滑 12 Hz ・ 語尾 200c」のように出ます。
  - `tuning % (phrase)`: 自分で付けたビブラート・PITD を何割足すか。100 でそのまま、50 で半分、0 で足さない。測ると「い」の揺れ幅が 100 % で 74 c、50 % で 38 c、0 % で 18 c(0 % で残るのは NEUTRINO がもともと出す揺れ)。
  - `smooth Hz (phrase)`: 1 秒にこの回数より速い揺れを均す。0 で切。ビブラートは 1 秒に 5〜6 回なので、12〜24 Hz ならビブラートは残り、6 Hz まで下げるとビブラートも小さくなります。測ると「い」の揺れ幅が 24・12 Hz では 74 c のまま(NEUTRINO の線はもともと滑らか)、6 Hz で 44 c。
  - `tail limit c (phrase)`: 母音の後ろ 4 割で音程が深く落ちるのを、何セントで止めるか。0 で切。浅い落ちはほぼそのまま、深い落ちほどこの値へなめらかに寄せます。測ると「い」の語尾の落ち 198 c が、200 c で 149 c、100 c で 105 c。
  - 1 枚目は画面で、同じ句を既定のまま(上)と 50 % ・ 12 Hz ・ 200 c(下)で比べた物。白い線が実際に鳴る音程で、下では語尾の落ち(赤枠)が止まり、句の途中の深い落ち込みとビブラートの幅が小さくなっています。
  - 2〜4 枚目は、同じ句「ずんだもちたべたい」(NEUTRINO v3 ずんだもん)を値だけ変えて書き出し、出来上がりの音から測った音程(左が句全体、右が赤枠の拡大、青い帯は譜面)。

  <img src="docs/singing-style-shape.webp" alt="歌い方の効き具合を句ごとに整えられる(調声の量・線の均し・語尾の落ち)">

  <img src="docs/style-tuning.webp" alt="歌い方の効き具合を句ごとに整えられる(調声の量・線の均し・語尾の落ち)">

  <img src="docs/style-smooth.webp" alt="歌い方の効き具合を句ごとに整えられる(調声の量・線の均し・語尾の落ち)">

  <img src="docs/style-tail.webp" alt="歌い方の効き具合を句ごとに整えられる(調声の量・線の均し・語尾の落ち)">

- **VOICEVOX の音を 44.1 kHz で受ける** [988cbb17](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/988cbb17902f02aa101db80b89f0238b5fa34ac5): 24 kHz で受けて OpenUtau 側で上げていたため、8 kHz より上が 10〜18 dB 削れていました。
- **44.1 kHz でない音の読み込みを sinc で変換する** [68f4a04c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/68f4a04c24bb1b2a88ed31604aff812aaca2ba33): 48 kHz の伴奏や NEUTRINO の出力などを、線形補間でなく sinc で変換します(48 kHz からだと 16 kHz で −11 dB、18 kHz で −21 dB 削れ、1〜2 標本遅れていたのが直ります)。NAudio が次の版で既定にする設定と同じです。
- **VOICEVOX でカタカナの歌詞も歌う** [2d2e6a9d](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/2d2e6a9d14a29ddbe6d0938318db8d3a69e93310): かなの表にひらがなしかなく、カタカナの音符は黙って休符になっていました。
- **VOICEVOX で `あっ[a cl]`・`づ`・`ー` も歌う** [a4d059d4](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/a4d059d453b7d9c226dfe433d46d8bbee30e54f2): 発音ヒント付きの歌詞(`あっ[a cl]` など)・`づ`・`ー`(さー)が黙って休符になり、無音でした。`あっ` は「あ」「っ」の 2 音符に分けて渡し、`ー` は前の音の母音で歌います。

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
| CLI を NEUTRINO 対応版で動くようにする直し | この repo | [bc287690](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/bc287690e45642c69ceaccc64dba139bdef9b9fa) | [72d4e4c5](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/72d4e4c59b260962c244e7dfff493e70737cfdaa) |  |
| 句ごとに声と歌い方を選べる(VOICEVOX の声を NEUTRINO の歌い方で歌わせる) | この repo | [7eea7f8b](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7eea7f8b366ae440969d9224ab787ab1ed26fde4) [9db74e93](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/9db74e93343dc5b6844033d634240c2d424d796d) | [59140887](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/59140887d54937c14a8c97eec0546ad8a9bced65) [b7be2600](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/b7be2600524443e90996255d01209836a5d318b8) |  |
| どの句をどの声と歌い方で歌っているかが見える | この repo | [a0fa3c3f](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/a0fa3c3f9003ba20266797e5b9dce49f5ea74b2e) | [c09a9115](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/c09a9115566e52c7cf53ea77d0bc4f486b07eaa8) |  |
| 歌い方の効き具合を句ごとに整えられる(調声の量・線の均し・語尾の落ち) | この repo | [d3e3d5ac](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d3e3d5acdc8950b2ea5adc5d84dd2e5389738e03) | [a4fad1eb](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/a4fad1eb7401a3580e7447421ae9ccd4a885ea97) |  |
| NEUTRINO v3 の声のトラックでは、2.x でだけ効く表情(engine type・engine mode・tone shift)を出さない | この repo | [140d82a4](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/140d82a44dce0022422d8c3fe9b9aefe2cd4ec81) | [45c8e3a7](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/45c8e3a78b47b948bea9b3b1fbff7e68e7f174fc) |  |
| VOICEVOX の音を 44.1 kHz で受ける | この repo | [a8caffe3](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/a8caffe37771ff04254b663d43a1ce950c7b1980) | [988cbb17](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/988cbb17902f02aa101db80b89f0238b5fa34ac5) | [`openutau-fix/voicevox-44100`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/voicevox-44100) [76f51a7f](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/76f51a7faed1642bce3b648ec270ee2638a0f35b) |
| 44.1 kHz でない音の読み込みを sinc で変換する | この repo | [1dcba0c5](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/1dcba0c580497100abc76944384ff25256bad9ff) | [68f4a04c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/68f4a04c24bb1b2a88ed31604aff812aaca2ba33) | [`openutau-fix/sinc-resampling`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/sinc-resampling) [c94ffe8a](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/c94ffe8ad1daca46048d878bf49bb5bf00c11ff7) |
| VOICEVOX でカタカナの歌詞も歌う | この repo | [9ee0b068](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/9ee0b068236018bed4f9e568271b8efb7d4c7167) | [2d2e6a9d](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/2d2e6a9d14a29ddbe6d0938318db8d3a69e93310) | [`openutau-fix/voicevox-katakana-lyrics`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/voicevox-katakana-lyrics) [e522d9f0](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/e522d9f09f476796efb00d0d582552dbcb1da546) |
| VOICEVOX で `あっ[a cl]`・`づ`・`ー` も歌う | この repo | [53b8bdce](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/53b8bdce2c7cd2fbf362b7950d8211da43487361) | [a4d059d4](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/a4d059d453b7d9c226dfe433d46d8bbee30e54f2) |  |

右の列は、「この repo」の直しのうち本家の OpenUtau にもそのまま使える物を、本家 `master`([f920e242](https://github.com/openutau/OpenUtau/commit/f920e2423113f9a936ec77eebf1f192e02a7ea6a))に 1 コミットずつ載せたブランチです(テスト付き)。本家の OpenUtau に cherry-pick すれば、そのまま使えます。

```bash
git fetch https://github.com/pika-02-03/OpenUtau-NEUTRINO.git openutau-fix/sinc-resampling
git cherry-pick FETCH_HEAD
```

## 注意

非公式・無保証です。

この repo で足したコード(README の[ソース](https://github.com/pika-02-03/OpenUtau-NEUTRINO#ソース)で「この repo」と書いた物)は、連絡なしにどう使っても構いません。フォーク・改変・再配布・取り込み、何でも自由で、こちらは関知しません。

本家 OpenUtau や、PR を出している方々のコードは、それぞれの条件に従ってください。OpenUtau は [MIT ライセンス](https://github.com/openutau/OpenUtau/blob/master/LICENSE.txt)、NEUTRINO は[公式の規約](https://studio-neutrino.com/downloads/manual/EULA_NEUTRINO.pdf)です。
