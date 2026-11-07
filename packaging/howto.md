## 入れ方

ダウンロードした dmg(Mac)・zip(Windows)には、アプリ本体と一緒に **登録スクリプト** が入っています。

- **登録スクリプト**: 手元の NEUTRINO を、この OpenUtau から使えるようにする物。NEUTRINO のフォルダを渡すだけで、中の歌声モデルが OpenUtau の歌手に並びます。
- NEUTRINO 本体は入っていません。[NEUTRINO 公式](https://studio-neutrino.com/)から v3.2 以上を入手して、好きな場所に展開しておいてください。

{{INSTALL}}

手で入れる時は、[rokujyushi さんの wiki](https://github.com/rokujyushi/OpenUtau/wiki/OpenUtau%E2%80%90for%E2%80%90NEUTRINO) のとおりです(アプリ名は `OpenUtau`)。

## 使い方

- **画面なしで書き出す**: `OpenUtau render --input x.ustx --output x.wav`。オプションは [CLI の説明](https://github.com/{{BUILD_REPO}}/blob/release-kit/docs/cli.md)。
- **補足**: VOICEVOX の声で歌わせる時は、VOICEVOX のアプリを裏で起動しておく(起動していないと動かない)。

