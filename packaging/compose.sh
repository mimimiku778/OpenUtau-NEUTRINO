#!/usr/bin/env bash
# packaging/ の部品から説明文を組み立てる。
# 使い方: compose.sh readme|release|mac|windows
#   readme=リポジトリの README / release=Release の本文 / mac・windows=配布物に同梱する説明書
# 環境変数: SOURCE_REPO SOURCE_REF SOURCE_SHA SOURCE_DATE BASE_VERSION、BUILD_REPO (無ければ GITHUB_REPOSITORY)
set -euo pipefail
d=$(cd "$(dirname "$0")" && pwd)
# 元のソースに足した変更の一覧 (extra-commits.txt から)。部品の中の {{CHANGES}} の行がこれに置き換わる。
changes() {
  awk '!/^[[:space:]]*(#|$)/ { sha = $1; repo = $2; $1 = ""; $2 = ""; sub(/^ +/, "")
       printf "- %s([`%s`](https://github.com/%s/commit/%s))\n", $0, substr(sha, 1, 8), repo, sha }' "$d/extra-commits.txt"
}
part() { while IFS= read -r line; do if [ "$line" = "{{CHANGES}}" ]; then changes; else printf '%s\n' "$line"; fi; done < "$d/$1.md"; }
case "${1:-}" in
  readme)  { part top; part howto; part about; part notice; part bottom; } ;;
  release) { part howto; part about; part notice; } ;;
  mac)     { echo "# OpenUtau NEUTRINO対応版(非公式)Mac 用"; echo; part howto; part about; part notice; } ;;
  windows) { echo "# OpenUtau NEUTRINO対応版(非公式)Windows 用"; echo; part howto; part about; part notice; } ;;
  *) echo "usage: $0 readme|release|mac|windows" >&2; exit 2 ;;
esac | sed -e "s#{{SOURCE_REPO}}#${SOURCE_REPO}#g" -e "s#{{SOURCE_REF}}#${SOURCE_REF}#g" \
           -e "s#{{SOURCE_SHA}}#${SOURCE_SHA}#g" -e "s#{{SOURCE_SHORT}}#${SOURCE_SHA:0:8}#g" \
           -e "s#{{SOURCE_DATE}}#${SOURCE_DATE}#g" -e "s#{{BASE_VERSION}}#${BASE_VERSION}#g" \
           -e "s#{{BUILD_REPO}}#${BUILD_REPO:-${GITHUB_REPOSITORY:?BUILD_REPO or GITHUB_REPOSITORY}}#g"
