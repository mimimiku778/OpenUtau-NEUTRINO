## 自動更新の仕組み

- 毎朝6時(日本時間)に、元のブランチと本家 OpenUtau の master が更新されていないかを GitHub Actions が確かめます。更新されていれば Mac 版と Windows 版をビルドし、Release を出します。Release は元のソースのコミットごとに1つで、同じソースで作り直した時は中身を差し替えます。
- 固定版は元のソースに [`packaging/extra-commits.txt`](packaging/extra-commits.txt) のコミットを、最新版は本家の master を混ぜた上に [`packaging/extra-commits-latest.txt`](packaging/extra-commits-latest.txt) のコミットを足します。一覧を変えた時も作り直します。同じ変更が PR に入れば自動で飛ばし、当たらなくなればその版だけビルドを止めて知らせます(もう一方の版は出します)。
- アプリの版は、それぞれが取り込んでいる本家 OpenUtau の版に合わせます。
- PR が本家にマージされたら、ビルドをやめ、本家の [Releases](https://github.com/openutau/OpenUtau/releases) への案内を出して止まります。
- このブランチ (`release-kit`) には配布用の設定だけがあります。説明文は [`packaging/`](packaging) の部品から、この README・Release の本文・同梱の説明書を組み立てています。最後に配布した版は [`state/last-build.json`](state/last-build.json) にあります。
