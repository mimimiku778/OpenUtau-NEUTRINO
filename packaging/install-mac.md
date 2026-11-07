### Mac

dmg の中身:

| ファイル | 何か |
|---|---|
| `OpenUtau.app` | アプリ本体 |
| `NEUTRINOを登録.command` | 登録スクリプト |
| `NEUTRINOの登録を解除.command` | 登録を外すスクリプト |
| `はじめにお読みください.md` | この説明 |

手順:

1. dmg を開き、`OpenUtau.app` を隣の `Applications` へドラッグする。
2. dmg の中の `NEUTRINOを登録.command` をダブルクリックする(ターミナルが開く)。
3. 「NEUTRINO のフォルダをこのウィンドウへドラッグして」と出たら、NEUTRINO のフォルダをドラッグして Enter。
4. 「完了」と出たら OpenUtau を起動する。歌手に「`<モデル名>` (NEUTRINO)」が並ぶ。

ダブルクリックで開けない時は、ターミナルに `bash `(後ろに空白)と打ってから `NEUTRINOを登録.command` をドラッグし、Enter。

登録スクリプトがすること:

- OpenUtau と NEUTRINO から「ネットから来たファイル」の印を外す。署名が無いので、そのままだと macOS に止められるため。中身は変えない。
- `~/Library/OpenUtau` に、NEUTRINO へのリンクと、モデルごとの歌手を作る。NEUTRINO 本体とモデルには書き込まない。
- モデルを足したり消したりした時は、もう一度実行すれば合わせ直す。

`NEUTRINOの登録を解除.command` は、登録で作った物だけを外します。NEUTRINO 本体・モデル・作品は消えません。
