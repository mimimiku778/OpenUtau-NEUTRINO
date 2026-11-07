#!/usr/bin/env bash
# 元のソースが、本家 OpenUtau のどの版までを取り込んでいるかを調べて、その版を出す。
# 本家のタグのうち、元のソースのコミットから辿れる一番新しいもの (例: 0.1.570.3-alpha → 0.1.570.3)。
# 使い方: base-version.sh <元のソースのリポジトリ> <コミット>
# 見つからない時は何も出さずに終わる。
set -euo pipefail
src_repo=$1 sha=$2
upstream=${UPSTREAM_REPO:-openutau/OpenUtau}
dir=$(mktemp -d); trap 'rm -rf "$dir"' EXIT
git init -q --bare "$dir"
# 中身は要らないので、コミットの履歴とタグだけを取る
git -C "$dir" fetch -q --filter=tree:0 --no-tags "https://github.com/$upstream" '+refs/tags/*:refs/upstream/*'
git -C "$dir" fetch -q --filter=tree:0 --no-tags "https://github.com/$src_repo" "$sha"
# 本家の版の並び: X.Y.Z.n-alpha (X.Y.Z に向けた開発版) < X.Y.Z-beta < X.Y.Z
git -C "$dir" for-each-ref --merged "$sha" --format='%(refname:lstrip=2)' refs/upstream \
  | grep -E '^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?(-alpha|-beta)?$' \
  | awk '{ split($0, a, "-"); n = split(a[1], v, ".")
           if (n == 4) key = a[1] ".0"
           else key = a[1] ".999999." (a[2] == "beta" ? 1 : 2)
           print key, a[1] }' \
  | sort -V | tail -1 | cut -d' ' -f2
