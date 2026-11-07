# 画面なしの書き出し(CLI)

OpenUtau を起動せずに、プロジェクトを wav に書き出します。本家への PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162) の機能です。

## 書き方

```
# Mac
/Applications/OpenUtau.app/Contents/MacOS/OpenUtau render --input 曲.ustx --output 曲.wav

# Windows
OpenUtau.exe render --input 曲.ustx --output 曲.wav
```

- `--input` にフォルダを渡すと、その中のプロジェクトを1つずつ、`--output` のフォルダへ同じ名前の `.wav` で書き出します(下のフォルダは見ません)。
- `render --help` で、このオプション一覧が出ます。
- `--input 曲.ustx` の代わりに `--input=曲.ustx` とも書けます。

## オプション

| オプション | 中身 |
|---|---|
| `--input`, `-i` | 書き出すプロジェクト(ustx・ust など画面で開けるもの)か、そのフォルダ。必須 |
| `--output`, `-o` | 書き出す wav か、そのフォルダ。必須 |
| `--singer` | 歌手を差し替える(ID か名前) |
| `--renderer` | レンダラーを差し替える(`NEUTRINO`・`CLASSIC`・`WORLDLINE-R`・`WORLDLINE-R2`・`ENUNU`・`VOGEN`・`DIFFSINGER`・`VOICEVOX`) |
| `--phonemizer` | フォネマイザーを差し替える(画面の表示名、`JA VCV` のような短い名前、型の名前) |
| `--resampler` | リサンプラーを差し替える(UTAU 音源) |
| `--wavtool` | wavtool を差し替える(UTAU 音源) |
| `--singers-path` | 歌手を探すフォルダを足す |
| `--onnx-runner` | ONNX を動かす方法(`CPU`・`DirectML`・`CoreML`・`NNAPI`) |
| `--onnx-gpu` | 使う GPU の番号 |
| `--diffsinger-depth`、`--diffsinger-steps`、`--diffsinger-variance-steps`、`--diffsinger-pitch-steps`、`--diffsinger-tensor-cache` | DiffSinger の設定 |

- 指定しなかったものは、プロジェクトに保存された設定のまま書き出します。NEUTRINO の歌手なら、ふつうは `--input` と `--output` だけで足ります。
- 差し替えはすべてのトラックにかかります。トラックごとには指定できません。
- オプションはその実行のときだけ使われ、アプリの設定は変わりません。

## 終わったときの値

| 値 | 意味 |
|---|---|
| `0` | 書き出せた |
| `1` | 書き出せなかった(フォルダをまとめて書き出した時は、1つでも失敗したら `1`。残りは続けて書き出します) |
| `2` | 書き方の誤り(知らないオプション、`--input`・`--output` が無い、など) |
