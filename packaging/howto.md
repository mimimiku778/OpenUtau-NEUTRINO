## 使い方

導入のしかた、NEUTRINO の置き方、歌手の入れ方は、rokujyushi さんの wiki を見てください: [OpenUtau-for-NEUTRINO インストール方法](https://github.com/rokujyushi/OpenUtau/wiki/OpenUtau%E2%80%90for%E2%80%90NEUTRINO)

wiki を読むときは、次のように読み替えてください。

- アプリの名前は `OpenUtau.app`(Mac)・`OpenUtau.exe`(Windows)です。wiki の `OpenUtau-for-NEUTRINO` を読み替えます。
- `Dependencies`・`Singers` フォルダは、Mac は `~/Library/OpenUtau/` の中、Windows は展開したフォルダの中です。
- wiki の手作業の代わりに、同梱の登録スクリプトでも NEUTRINO の配置と歌手の登録ができます。Mac は dmg の中の `NEUTRINOを登録.command` をターミナルで `bash` に渡して実行し、Windows は `register-neutrino.bat` をダブルクリックします。Mac の「壊れているため開けません」の警告もこれで外れます。wiki の手順とどちらか一方で入れてください。
- 画面を出さずに書き出すこともできます。Mac は `OpenUtau.app/Contents/MacOS/OpenUtau render --input x.ustx --output x.wav`(アプリ同梱の .NET で動きます)、Windows は `OpenUtau.exe render --input x.ustx --output x.wav` です。

