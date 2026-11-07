# OpenUtau NEUTRINO対応版(非公式)

rokujyushi さんの NEUTRINO 対応 PR [openutau/OpenUtau#2136](https://github.com/openutau/OpenUtau/pull/2136) を、最新のソースで自動ビルドしている非公式版です。本人の配布版「OpenUtau-for-NEUTRINO」との違いは、下の「本人の配布版・PR との違い」にまとめています。

![OpenUtau でずんだもん (NEUTRINO) を編集している画面](docs/screenshot.webp)

## ダウンロード

| | Mac(Apple Silicon) | Windows(64bit、動作未確認) |
|---|---|---|
| **固定版** OpenUtau v0.1.570.3 | [dmg](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-macos-arm64.dmg) | [zip](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-win-x64.zip) |
| **最新版** OpenUtau v0.1.577.9 | [dmg](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-latest-macos-arm64.dmg) | [zip](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-latest-win-x64.zip) |

固定版と最新版の違いは、下の「本人の配布版・PR との違い」にあります。同じ名前のアプリなので、入れるのはどちらか一方です。

- 過去の版: [Releases](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases)

## 使い方

導入のしかた、NEUTRINO の置き方、歌手の入れ方は、rokujyushi さんの wiki を見てください: [OpenUtau-for-NEUTRINO インストール方法](https://github.com/rokujyushi/OpenUtau/wiki/OpenUtau%E2%80%90for%E2%80%90NEUTRINO)

wiki を読むときは、次のように読み替えてください。

- アプリの名前は `OpenUtau.app`(Mac)・`OpenUtau.exe`(Windows)です。wiki の `OpenUtau-for-NEUTRINO` を読み替えます。
- `Dependencies`・`Singers` フォルダは、Mac は `~/Library/OpenUtau/` の中、Windows は展開したフォルダの中です。
- wiki の手作業の代わりに、同梱の登録スクリプトでも NEUTRINO の配置と歌手の登録ができます。Mac は dmg の中の `NEUTRINOを登録.command` をターミナルで `bash` に渡して実行し、Windows は `register-neutrino.bat` をダブルクリックします。Mac の「壊れているため開けません」の警告もこれで外れます。wiki の手順とどちらか一方で入れてください。
- 画面を出さずに書き出すこともできます。Mac は `OpenUtau.app/Contents/MacOS/OpenUtau render --input x.ustx --output x.wav`(アプリ同梱の .NET で動きます)、Windows は `OpenUtau.exe render --input x.ustx --output x.wav` です。

## 本人の配布版・PR との違い

PR のソース(rokujyushi/OpenUtau の `neutrino` ブランチ、コミット [`203c44f5`](https://github.com/rokujyushi/OpenUtau/commit/203c44f5d1374697abfa69626a2b52a19627184b)、2026-09-13)に、次の変更を足してビルドしています。

- **mel 編集**: v3 の歌声でも「声色(GENC)」と「明るさ(TENC)」のカーブが効きます。rokujyushi さんの試作で、PR にはまだ入っていません(`Editid-Mel-spectrogram` ブランチ)。([`c5cb0f47`](https://github.com/rokujyushi/OpenUtau/commit/c5cb0f47243a012d20c79d73b58d354b9149d7af))
- **direct でもビブラートと PITD が効く**: direct を on にした句でも、NEUTRINO の素の F0 に、音符のビブラートと PITD(ピッチのカーブ)を足して鳴らします。([`7a637f20`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7a637f201e12ad5840cef3371d6fe8fb8fb187e0))
- **鳴る音程を画面に描く**: direct の句では、画面の灰色の線(最終ピッチ)が実際に鳴る F0 になり、ペンもその高さを基準に描けます。([`7ee7cbc8`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/7ee7cbc8e951d3338f1fa45c88394b8cf8b32498))
- **Mac の Shift+ホイールで横スクロール**: ピアノロールとトラックの画面で、Shift を押しながらホイールを回すと横に動きます。([`4e6c5f7c`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/4e6c5f7c3e890da4d003b5d8f39653b74ef7789a))
- **画面なしの書き出し(CLI)**: 本家への PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162)(christianazinn さん、まだ本家に入っていません)を足し、NEUTRINO 対応版で動くように直しています。使い方は上の「使い方」にあります。([`d8c3da40`](https://github.com/pika-02-03/OpenUtau-NEUTRINO/commit/d8c3da40b535c7bc35108dfcb90e4abea883d345))

配布のための違いもあります。

- 2種類を配っています。NEUTRINO 対応と上の変更は同じで、OpenUtau 本体の版だけが違います。同じ名前のアプリなので、入れるのはどちらか一方です。
  - **固定版**(ファイル名に `latest` が付かない方): OpenUtau v0.1.570.3。PR のブランチが本家から最後に取り込んだ版のままです。
  - **最新版**(ファイル名に `latest` が付く方): OpenUtau v0.1.577.9。本家の最新(master の [`f920e242`](https://github.com/openutau/OpenUtau/commit/f920e2423113f9a936ec77eebf1f192e02a7ea6a)、2026-11-05)を混ぜたものです。PR の作者も本家も試していない組み合わせなので、動くとは限りません。本家が更新されるたびに作り直します。
- 本人の配布版と違い、アプリの名前と設定の保存場所は本家 OpenUtau と同じです。
- アプリの「更新の確認」は、この配布元を見ます。本家版(NEUTRINO 非対応)に上書きされないようにするためです。

## 注意

- **非公式・無保証です。** 本家 OpenUtau・rokujyushi さん・NEUTRINO の開発元とは関係ありません。使って起きた問題に、配布者は責任を負いません。
- **不具合の伝え先。** 上の一覧で足した変更や登録スクリプトの問題は、本家にも rokujyushi さんにも問い合わせないでください。NEUTRINO 対応そのものの不具合は、PR [openutau/OpenUtau#2136](https://github.com/openutau/OpenUtau/pull/2136) へ伝えられます。その時は、この非公式ビルドで試したことと、上のコミットを書き添えてください。
- **ライセンス・利用規約は本家に従います。** OpenUtau は本家の [MIT ライセンス](https://github.com/openutau/OpenUtau/blob/master/LICENSE.txt)、NEUTRINO は公式の [NEUTRINO 利用許諾契約書](https://studio-neutrino.com/downloads/manual/EULA_NEUTRINO.pdf) と [歌声ライブラリ利用許諾契約書](https://studio-neutrino.com/downloads/manual/EURA_neutrino_singer_library.pdf)、キャラクターごとの決まりは公式の [ライセンス・ガイドライン](https://studio-neutrino.com/ufaq-category/license/) に従ってください。NEUTRINO 本体と歌声モデルは、この配布物に含まれていません。

## 自動更新の仕組み

- 毎朝6時(日本時間)に、元のブランチと本家 OpenUtau の master が更新されていないかを GitHub Actions が確かめます。更新されていれば Mac 版と Windows 版をビルドし、Release を出します。Release は元のソースのコミットごとに1つで、同じソースで作り直した時は中身を差し替えます。
- 固定版は元のソースに [`packaging/extra-commits.txt`](packaging/extra-commits.txt) のコミットを、最新版は本家の master を混ぜた上に [`packaging/extra-commits-latest.txt`](packaging/extra-commits-latest.txt) のコミットを足します。一覧を変えた時も作り直します。同じ変更が PR に入れば自動で飛ばし、当たらなくなればその版だけビルドを止めて知らせます(もう一方の版は出します)。
- アプリの版は、それぞれが取り込んでいる本家 OpenUtau の版に合わせます。
- PR が本家にマージされたら、ビルドをやめ、本家の [Releases](https://github.com/openutau/OpenUtau/releases) への案内を出して止まります。
- このブランチ (`release-kit`) には配布用の設定だけがあります。説明文は [`packaging/`](packaging) の部品から、この README・Release の本文・同梱の説明書を組み立てています。最後に配布した版は [`state/last-build.json`](state/last-build.json) にあります。
