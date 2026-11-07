## このビルドについて

- 中身は、本家へ出ている NEUTRINO 対応の提案 [openutau/OpenUtau#2136](https://github.com/openutau/OpenUtau/pull/2136)(作者 rokujyushi さん、まだ本家に入っていません)です。{{SOURCE_REPO}} の `{{SOURCE_REF}}` ブランチ、コミット [`{{SOURCE_SHORT}}`](https://github.com/{{SOURCE_REPO}}/commit/{{SOURCE_SHA}})({{SOURCE_DATE}})をもとにビルドしています。
- 版は **v{{BASE_VERSION}}** です。提案が本家から最後に取り込んだ OpenUtau の版で、それより新しい本家の変更は入っていません。アプリに出る版の番号も同じです。
- 提案のままではなく、次の変更を足しています。
{{CHANGES}}
- 登録スクリプトは、NEUTRINO の中身を変えません。警告の印を外し、OpenUtau の設定フォルダにリンクと歌手の設定を置くだけです。
- ライセンスと利用規約は本家に準拠します。OpenUtau 部分は本家と同じ [MIT ライセンス](https://github.com/openutau/OpenUtau/blob/master/LICENSE.txt)です。NEUTRINO 本体と歌声モデルはこの配布物に含まれず、公式の規約([NEUTRINO](https://studio-neutrino.com/downloads/manual/EULA_NEUTRINO.pdf)・[歌声ライブラリ](https://studio-neutrino.com/downloads/manual/EURA_neutrino_singer_library.pdf)・[ライセンス・ガイドライン](https://studio-neutrino.com/ufaq-category/license/))に従います。
- 無保証です。このビルドやスクリプトを使って起きたいかなる問題(データが消えた、動かない、など)にも配布者は責任を負わず、問い合わせへの対応も約束しません。

