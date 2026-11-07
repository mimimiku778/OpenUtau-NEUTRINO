# OpenUtau NEUTRINO対応版(非公式)

本家 [OpenUtau](https://github.com/openutau/OpenUtau) に、rokujyushi さんの NEUTRINO 対応と、この版で足した機能を重ねた非公式ビルドです。

![OpenUtau でずんだもん (NEUTRINO) を編集している画面](docs/screenshot.webp)

## 中身

素の OpenUtau に、上から順に重ねています。

| | 固定版 | 最新版 |
|---|---|---|
| 1. 本家 OpenUtau | 0.1.570.3(下の NEUTRINO 対応が取り込んだ版) | 0.1.577.9(本家 `master` の [f920e242](https://github.com/openutau/OpenUtau/commit/f920e2423113f9a936ec77eebf1f192e02a7ea6a)、2026-11-05) |
| 2. NEUTRINO 対応 | rokujyushi さんの PR [openutau/OpenUtau#2136](https://github.com/openutau/OpenUtau/pull/2136)。ソースは [rokujyushi/OpenUtau](https://github.com/rokujyushi/OpenUtau/tree/neutrino) の `neutrino` ブランチ([203c44f5](https://github.com/rokujyushi/OpenUtau/commit/203c44f5d1374697abfa69626a2b52a19627184b)) | 同じ |
| 3. 画面なしの書き出し(CLI) | christianazinn さんの PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162) と、NEUTRINO 対応版で動くようにした直し | 同じ(本家の最新に当たる形) |
| 4. この版で足した機能 | 下の「足した機能」 | 同じ(本家の最新に当たる形) |

VOICEVOX で歌う機能は、1 の本家 OpenUtau に元からある物です。どのコミットを誰のどのブランチから入れたかは、下の「ソース」にあります。

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
- **画面を開かずに書き出せる(CLI)**: OpenUtau を起動せず、コマンド1行で ustx を wav に書き出せます(使い方は上)。たくさんの曲をまとめて書き出したり、ほかのツールから自動で呼んだりできます。本家に出ている未マージの PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162) を、NEUTRINO 対応版で動くように直して入れています。

## ソース

どちらの版も、ビルドしたソースをこの repo の 1 本のブランチにそのまま並べてあります(CI が組み立てる物と中身が同じ)。

| | ブランチ | 中身 |
|---|---|---|
| 固定版 | [`neutrino-cli-dist`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/neutrino-cli-dist) | 2 の `neutrino` に、下の表の「固定版」のコミットを上から順に足した物 |
| 最新版 | [`neutrino-latest-dist`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/neutrino-latest-dist) | 2 の `neutrino` に本家の `master`([f920e242](https://github.com/openutau/OpenUtau/commit/f920e2423113f9a936ec77eebf1f192e02a7ea6a))をマージし、下の表の「最新版」のコミットを上から順に足した物 |

```bash
git clone -b neutrino-cli-dist https://github.com/pika-02-03/OpenUtau-NEUTRINO.git
```

自分で組み立てる時は、rokujyushi さんの `neutrino`(最新版はそこへ本家の `master` をマージした物)に、下の表のコミットを上から順に cherry-pick します。CI と同じ手順は [packaging/apply-extra-commits.sh](https://github.com/pika-02-03/OpenUtau-NEUTRINO/blob/release-kit/packaging/apply-extra-commits.sh) です。

### 足したコミット

| 中身 | 誰の物か | 固定版 | 最新版 |
|---|---|---|---|
| v3 の声で mel 編集(GENC・TENC) | rokujyushi さん([`Editid-Mel-spectrogram`](https://github.com/rokujyushi/OpenUtau/tree/Editid-Mel-spectrogram) ブランチ) | [c5cb0f47](https://github.com/rokujyushi/OpenUtau/commit/c5cb0f47243a012d20c79d73b58d354b9149d7af) | [7b8ebd79](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7b8ebd790b6c5a7f7a91c14c339eb70f136b4d25) |
| direct でもビブラート・PITD が効く | この repo | [7a637f20](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7a637f201e12ad5840cef3371d6fe8fb8fb187e0) | [f6f6ae32](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/f6f6ae32d2e7655638e7817a247ba217676dd7a1)(本家の `pitchesBeforeVibrato` と名前がぶつかるので `stepPitches` にした) |
| 聞こえる音程を画面に描く | この repo | [7ee7cbc8](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7ee7cbc8e951d3338f1fa45c88394b8cf8b32498) | [ca70f8be](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/ca70f8bedb679aba8f5994904ad2276b9ab7165a) |
| Mac の Shift+ホイールで横スクロール | この repo | [4e6c5f7c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/4e6c5f7c3e890da4d003b5d8f39653b74ef7789a) | [502e607c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/502e607cf137ce42fd6a7882581ba20e4a6fca9b) |
| 画面なしの書き出し(CLI)、9 コミット | christianazinn さんの PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162)(head 06990cf5)。PR が force-push されても消えないよう、この repo に写した物 | 下の一覧 | 下の一覧 |
| CLI を NEUTRINO 対応版で動くようにする直し | この repo | [d8c3da40](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d8c3da40b535c7bc35108dfcb90e4abea883d345) | [db870028](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/db870028e686eed23bd7acb09ba476de923e1eb6) |

固定版の mel 編集は、ブランチ上では cherry-pick した 1eaba9c1 として入っています。

CLI の 9 コミット(PR のまま。最新版の 2 つだけ、本家が `OpenUtau.sln` を `OpenUtau.slnx` に替えたのに合わせた):

| PR のコミット | 固定版 | 最新版 |
|---|---|---|
| Add phonemizer idle wait | [003f50ed](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/003f50ede862081ee40e86dbc43c53930eb74ef4) | [8c0789a5](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/8c0789a5f65dbf43b82039629d9e4c294adaba0b) |
| Add headless render host | [fd532cd6](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/fd532cd6091d1c21762523d6fcb8adec6fdab69d) | [759ea4cd](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/759ea4cd326bd5187b5e68420547b761c4b8c3a2) |
| Add CLI render command | [c5a30d1d](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/c5a30d1dc371766cb67a54ec9573ef6085171e01) | [d75ba160](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d75ba160d6f53d90bf38485603876ce50f3e42e2)(`OpenUtau.Cli` を slnx に足した) |
| Fix headless CLI runtime behavior | [75a3d946](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/75a3d9461f090064f0950fafa65a29bee9f41aa7) | [fdd4a328](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/fdd4a32895b64b1e9f3fd22cdb010757f1e4cd6a) |
| Avoid stale output probes in CLI render | [3f019bce](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/3f019bce1117f1926fe607cbc2a81cf2c7eb2724) | [28c610b0](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/28c610b0e30503cc3a1529f2a35921877d276168) |
| Run render command from OpenUtau executable | [fb18986b](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/fb18986b35c4a06a6727756dc68d7c41aaaf7358) | [d7eecf5b](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d7eecf5b40439b3cf7619f903d80e22e50a64963)(`OpenUtau.Cli` を slnx から外した) |
| Expose headless ONNX and DiffSinger options | [a9b35dd1](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/a9b35dd1070470c26565073001fc0ba85c6d6410) | [77a8ef4c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/77a8ef4c77937e5099871867a70f3b870b391a9d) |
| Add headless batch render mode | [59b1ec0c](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/59b1ec0c9a6fbe6d34c2431de682374c4adf6a0a) | [4e770fc1](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/4e770fc1caf9feb4bb3aa2f5fe26f0b840490b60) |
| Add headless render command tests | [e4814b96](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/e4814b9629181e60d6f2478c9180119cdc6c3cf3) | [d239c976](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d239c976455a011777536d781b86a34a999c0267) |

CLI を NEUTRINO 対応版で動くようにした直し(d8c3da40・db870028)の中身:

- 画面なしでは、裏でフレーズを組み立てる役を止めて、その場で組み立てさせる(残っていると、書き出す時点で句がまだ空のことがある)。
- `OPENUTAU_DUMP_DIRECT_PITCH=<path>` を指定した時だけ、direct の句の画面に描く音程の線を JSON Lines で書き出す(画面を開かずに、描く線と鳴った音程が同じかを確かめるため)。
- 書き出しのスレッドの MTA 指定を Windows の時だけにする。
- CLI のテストの `Render` の引数を、NEUTRINO 対応版の今の形(`RenderPhraseEvents`)に合わせる。

### 配布版に入っていないブランチ

**本家 OpenUtau に足して使える修正**: 本家 `openutau/OpenUtau` の `master`([f920e242](https://github.com/openutau/OpenUtau/commit/f920e2423113f9a936ec77eebf1f192e02a7ea6a))に 1 コミットずつ載せたブランチです(テスト付き、どれも この repo)。本家の OpenUtau に cherry-pick すれば、そのまま使えます。

| 修正 | ブランチ | コミット |
|---|---|---|
| Mac で Shift+ホイールが横スクロールになる(配布版の 4e6c5f7c と同じ変更) | [`openutau-fix/mac-shift-wheel`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/mac-shift-wheel) | [8ae718f2](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/8ae718f25761df64901baa24d631c58a61ee9937) |
| VOICEVOX の音をエンジンから 44.1 kHz で受ける(24 kHz で受けて OpenUtau 側で上げると、8 kHz より上が 10〜18 dB 削れていた) | [`openutau-fix/voicevox-44100`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/voicevox-44100) | [76f51a7f](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/76f51a7faed1642bce3b648ec270ee2638a0f35b) |
| VOICEVOX でカタカナの歌詞も歌う(かなの表にひらがなしかなく、カタカナの音符は黙って休符になっていた) | [`openutau-fix/voicevox-katakana-lyrics`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/voicevox-katakana-lyrics) | [e522d9f0](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/e522d9f09f476796efb00d0d582552dbcb1da546) |
| 44.1 kHz でない音(48 kHz の伴奏や NEUTRINO の出力など)の読み込みを、線形補間でなく sinc で変換する(今は 48 kHz から 16 kHz で −11 dB、18 kHz で −21 dB 削れ、1〜2 標本遅れる)。NAudio が次の版で既定にする設定と同じ | [`openutau-fix/sinc-resampling`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/openutau-fix/sinc-resampling) | [c94ffe8a](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/c94ffe8ad1daca46048d878bf49bb5bf00c11ff7) |

```bash
git fetch https://github.com/pika-02-03/OpenUtau-NEUTRINO.git openutau-fix/sinc-resampling
git cherry-pick FETCH_HEAD
```

**試作中**: [`neutrino-f0-teacher`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/tree/neutrino-f0-teacher)(この repo、最新版の上に積んだ物)。句ごとに歌う声(NEUTRINO・VOICEVOX)と、音程を作る先生(NEUTRINO の歌手)を選べる機能と、どの句をどの声と先生で歌っているかを画面の上に帯で出す表示です。

## 注意

非公式・無保証です。

この repo で足したコード(README の[ソース](https://github.com/pika-02-03/OpenUtau-NEUTRINO#ソース)で「この repo」と書いた物)は、連絡なしにどう使っても構いません。フォーク・改変・再配布・取り込み、何でも自由で、こちらは関知しません。

本家 OpenUtau や、PR を出している方々のコードは、それぞれの条件に従ってください。OpenUtau は [MIT ライセンス](https://github.com/openutau/OpenUtau/blob/master/LICENSE.txt)、NEUTRINO は[公式の規約](https://studio-neutrino.com/downloads/manual/EULA_NEUTRINO.pdf)です。
