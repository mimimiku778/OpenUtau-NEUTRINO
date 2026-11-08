### Mac

1. **NEUTRINO を置く**: NEUTRINO の zip をダブルクリックで展開し、NEUTRINO のフォルダをホーム(Finder で ⌘+Shift+H を押すと開く、家のマークのフォルダ)など、消さない場所へ移す。
2. **OpenUtau を入れる**: この配布の dmg を開き、`OpenUtau.app` を隣の `Applications` へドラッグする。
3. **ターミナルを開く**: ⌘+スペースを押し、「ターミナル」と打って Enter。
4. **登録スクリプトを動かす**: ターミナルに `bash `(bash と空白 1 つ)と打ち、dmg の中の `NEUTRINOを登録.command` をターミナルへドラッグして Enter。
5. **NEUTRINO のフォルダを渡す**: 「NEUTRINO のフォルダをこのウィンドウへドラッグして、Enter を押してください。」と出たら、手順 1 の NEUTRINO のフォルダをターミナルへドラッグして Enter。
6. **終わり**: 「完了。」と出たら OpenUtau を起動する。歌手の一覧に `ZUNDAMON (NEUTRINO)` のように、モデルの名前 + `(NEUTRINO)` で並ぶ。

途中で「アクセスを許可しますか」と聞かれたら「許可」を押す。

**うまくいかない時**

| こうなった | 直し方 |
|---|---|
| `NEUTRINO のフォルダではないようです` と出る | 渡したフォルダが違う。開いて `bin` と `model` が見えるフォルダで手順 4 から |
| `model/ の中に歌声モデルが見つかりませんでした` と出る | `model` の中が空。NEUTRINO 公式から歌声モデルを入れて手順 4 から |
| OpenUtau を開こうとすると止められる | 手順 2 の前に手順 4 をした。手順 4 からやり直す |

歌声モデルを足した時や NEUTRINO のフォルダを動かした時も、手順 3〜5 をもう一度。

登録スクリプトは、OpenUtau と NEUTRINO から「ネットから来たファイル」の印を外し(署名が無いので、そのままだと macOS に止められる)、`~/Library/OpenUtau` に NEUTRINO へのリンクと歌手を作ります。NEUTRINO 本体とモデルには書き込みません。外す時は、dmg の `NEUTRINOの登録を解除.command` を同じように動かします(NEUTRINO・モデル・作品は消えません)。
