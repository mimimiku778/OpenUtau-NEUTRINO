#!/usr/bin/env bash
# extra-commits.txt のコミットを、上から順に、今いるフォルダ (元のソースの checkout) に足す。
# コミットはせず、作業ツリーに当てるだけ。
# 使い方: apply-extra-commits.sh <extra-commits.txt>
# - 元のソースにもう入っている変更は、当てても何も変わらないので、そう知らせるだけ。
# - 当たらない時はビルドを止める。
set -euo pipefail
list=$1
while read -r sha repo _; do
  case "$sha" in ''|'#'*) continue ;; esac
  if ! [[ $sha =~ ^[0-9a-f]{40}$ ]]; then
    echo "::error::コミットが未記入の行があります ($sha $repo)。$(basename "$list") を埋めてください"
    exit 1
  fi
  git fetch -q --no-tags --depth=2 "https://github.com/$repo" "$sha"
  before=$(git write-tree)
  if ! git -c user.name=build -c user.email=build@localhost cherry-pick --no-commit "$sha"; then
    echo "::error::${sha:0:8} ($repo) が元のソースに当たりません。packaging/extra-commits.txt を見直してください"
    exit 1
  fi
  if [ "$(git write-tree)" = "$before" ]; then
    echo "::notice::${sha:0:8} は元のソースにもう入っています。packaging/extra-commits.txt から外せます"
  else
    echo "${sha:0:8} ($repo) を足しました"
  fi
done < "$list"
