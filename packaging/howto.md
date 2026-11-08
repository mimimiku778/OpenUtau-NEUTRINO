## 入れ方

**用意する物**

| 物 | どこから |
|---|---|
| この OpenUtau(Mac は dmg、Windows は zip) | [Releases](https://github.com/{{BUILD_REPO}}/releases/latest) |
| NEUTRINO 本体(v3.2 以上) | [NEUTRINO 公式](https://studio-neutrino.com/)。この配布には入っていない |

**「NEUTRINO のフォルダ」**: NEUTRINO の zip を展開すると出てくるフォルダ。開いて `bin` と `model` が見えれば、それです。

```
NEUTRINO      ← これが「NEUTRINO のフォルダ」
├── bin
├── model     ← 歌声モデル (ZUNDAMON など) が 1 つずつ入っている
├── score
└── …
```

- 開いても `bin` と `model` が無く、フォルダが 1 つだけ入っている時は、その中が NEUTRINO のフォルダ。
- 登録した後にこのフォルダを動かしたり名前を変えたりすると歌えなくなる。先に置き場所を決めておく(動かした時は登録をやり直す)。

{{INSTALL}}

手で入れる時は、[rokujyushi さんの wiki](https://github.com/rokujyushi/OpenUtau/wiki/OpenUtau%E2%80%90for%E2%80%90NEUTRINO) のとおりです(アプリ名は `OpenUtau`)。

## 使い方

- **画面なしで書き出す**: `OpenUtau render --input x.ustx --output x.wav`。オプションは [CLI の説明](https://github.com/{{BUILD_REPO}}/blob/release-kit/docs/cli.md)。
- **補足**: VOICEVOX の声で歌わせる時は、VOICEVOX のアプリを裏で起動しておく(起動していないと動かない)。

