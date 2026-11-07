# OpenUtau NEUTRINO対応版(非公式)

rokujyushi さんの NEUTRINO 対応 PR [openutau/OpenUtau#2136](https://github.com/openutau/OpenUtau/pull/2136) に機能を足した非公式ビルドです。

![OpenUtau でずんだもん (NEUTRINO) を編集している画面](docs/screenshot.webp)

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

## 注意

非公式・無保証です。OpenUtau は [MIT ライセンス](https://github.com/openutau/OpenUtau/blob/master/LICENSE.txt)、NEUTRINO は[公式の規約](https://studio-neutrino.com/downloads/manual/EULA_NEUTRINO.pdf)に従ってください。
