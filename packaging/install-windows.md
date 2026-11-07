### Windows

zip の中身(展開したフォルダが、そのまま OpenUtau 本体です):

| ファイル | 何か |
|---|---|
| `OpenUtau.exe` | アプリ本体 |
| `register-neutrino.bat` | 登録スクリプト |
| `unregister-neutrino.bat` | 登録を外すスクリプト |
| `はじめにお読みください.md` | この説明 |

手順:

1. zip を好きな場所に展開する。
2. 展開したフォルダの `register-neutrino.bat` をダブルクリックする(黒いウィンドウが開く)。
3. 「NEUTRINO のフォルダをこのウィンドウへドラッグして」と出たら、NEUTRINO のフォルダをドラッグして Enter。
4. 「完了」と出たら `OpenUtau.exe` を起動する。歌手に「`<モデル名>` (NEUTRINO)」が並ぶ。

登録スクリプトがすること(管理者権限は要らない):

- OpenUtau と NEUTRINO のファイルのブロックを外す。署名が無いので、そのままだと Windows に止められるため。中身は変えない。
- OpenUtau のフォルダの中に、NEUTRINO へのリンクと、モデルごとの歌手を作る。NEUTRINO 本体とモデルには書き込まない。
- モデルを足したり消したりした時は、もう一度実行すれば合わせ直す。

`unregister-neutrino.bat` は、登録で作った物だけを外します。NEUTRINO 本体・モデル・作品は消えません。
