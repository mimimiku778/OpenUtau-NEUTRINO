## 自動更新の仕組み

- 毎朝6時(日本時間)に、元のブランチが更新されていないかを GitHub Actions が確かめます。更新されていれば Mac 版と Windows 版をビルドし、新しい Release を出します。
- 元のソースに足すコミットは [`packaging/extra-commits.txt`](packaging/extra-commits.txt) にあります。このリストを変えた時も作り直します。同じ変更が提案に入れば自動で飛ばし、当たらなくなればビルドを止めて知らせます。
- アプリの版は、元のソースが取り込んでいる本家 OpenUtau の版に合わせます。
- 提案が本家に取り込まれたら、ビルドをやめ、本家の [Releases](https://github.com/openutau/OpenUtau/releases) への案内を出して止まります。
- このブランチ (`release-kit`) には配布用の設定だけがあります。説明文は [`packaging/`](packaging) の部品から、この README・Release の本文・同梱の説明書を組み立てています。最後に配布した版は [`state/last-build.json`](state/last-build.json) にあります。
