# OpenUtau NEUTRINO対応版(非公式)

rokujyushi さんの NEUTRINO 対応 PR [openutau/OpenUtau#2136](https://github.com/openutau/OpenUtau/pull/2136) に機能を足した非公式ビルドです。

![OpenUtau でずんだもん (NEUTRINO) を編集している画面](docs/screenshot.webp)

## ダウンロード

| | Mac | Windows(動作未確認) |
|---|---|---|
| 固定版 | [dmg](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-macos-arm64.dmg) | [zip](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-win-x64.zip) |
| 最新版 | [dmg](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-latest-macos-arm64.dmg) | [zip](https://github.com/pika-02-03/OpenUtau-NEUTRINO/releases/latest/download/OpenUtau-NEUTRINO-latest-win-x64.zip) |

最新版は本家 OpenUtau の最新を混ぜたもの(動作保証なし)。入れるのはどちらか一方です。

## 使い方

- 導入は [rokujyushi さんの wiki](https://github.com/rokujyushi/OpenUtau/wiki/OpenUtau%E2%80%90for%E2%80%90NEUTRINO) のとおり(アプリ名は `OpenUtau`)。同梱の登録スクリプトでも入れられます。
- 画面なしで書き出し: `OpenUtau render --input x.ustx --output x.wav`

## 足した機能

- mel 編集(v3 でも GENC・TENC が効く)
- direct でもビブラート・PITD が効く
- direct の句で、実際に鳴る音程を線で描く
- Mac の Shift+ホイールで横スクロール
- 画面なしの書き出し(CLI)

## 注意

非公式・無保証です。OpenUtau は [MIT ライセンス](https://github.com/openutau/OpenUtau/blob/master/LICENSE.txt)、NEUTRINO は[公式の規約](https://studio-neutrino.com/downloads/manual/EULA_NEUTRINO.pdf)に従ってください。
