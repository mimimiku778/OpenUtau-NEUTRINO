## 本人の配布版・提案との違い

提案のソース({{SOURCE_REPO}} の `{{SOURCE_REF}}` ブランチ、コミット [`{{SOURCE_SHORT}}`](https://github.com/{{SOURCE_REPO}}/commit/{{SOURCE_SHA}})、{{SOURCE_DATE}})に、次の変更を足してビルドしています。

{{CHANGES}}

配布のための違いもあります。

- 版は **v{{BASE_VERSION}}** です。提案が本家から最後に取り込んだ OpenUtau の版です。
- 本人の配布版と違い、アプリの名前と設定の保存場所は本家 OpenUtau と同じです。
- アプリの「更新の確認」は、この配布元を見ます。本家版(NEUTRINO 非対応)に上書きされないようにするためです。

