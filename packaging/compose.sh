#!/usr/bin/env bash
# packaging/ の部品から説明文を組み立てる。
# 使い方: compose.sh readme|release|mac|windows
#   readme=リポジトリの README / release=Release の本文 / mac・windows=配布物に同梱する説明書
# 環境変数: SOURCE_REPO SOURCE_REF SOURCE_SHA SOURCE_DATE BASE_VERSION、BUILD_REPO (無ければ GITHUB_REPOSITORY)
set -euo pipefail
d=$(cd "$(dirname "$0")" && pwd)
# 元のソースに足した変更の一覧。部品の中の {{CHANGES}} の行がこれに置き換わる。
changes() {
  echo "  - アプリの「更新の確認」の確認先を、この配布元にしています。本家版(NEUTRINO 非対応)に上書きされないようにするためです。"
  awk '!/^[[:space:]]*(#|$)/ { sha = $1; $1 = ""; sub(/^ +/, "")
       printf "  - [`%s`](https://github.com/{{SOURCE_REPO}}/commit/%s): %s\n", substr(sha, 1, 8), sha, $0 }' "$d/extra-commits.txt"
}
part() { while IFS= read -r line; do if [ "$line" = "{{CHANGES}}" ]; then changes; else printf '%s\n' "$line"; fi; done < "$d/$1.md"; }
trouble() { echo "## 困ったとき"; echo; part trouble-common; for os in "$@"; do part "trouble-$os"; done; echo; }
case "${1:-}" in
  readme)  { part top; part notice; part requirements; echo "## 導入方法"; echo; part install-mac; part install-windows; trouble mac windows; part about; part bottom; } ;;
  release) { part notice; part requirements; echo "## 導入方法"; echo; part install-mac; part install-windows; trouble mac windows; part about; } ;;
  mac)     { echo "# OpenUtau NEUTRINO対応版(非公式)Mac 用"; echo; part notice; part requirements; echo "## 導入方法"; echo; part install-mac; trouble mac; part about; } ;;
  windows) { echo "# OpenUtau NEUTRINO対応版(非公式)Windows 用"; echo; part notice; part requirements; echo "## 導入方法"; echo; part install-windows; trouble windows; part about; } ;;
  *) echo "usage: $0 readme|release|mac|windows" >&2; exit 2 ;;
esac | sed -e "s#{{SOURCE_REPO}}#${SOURCE_REPO}#g" -e "s#{{SOURCE_REF}}#${SOURCE_REF}#g" \
           -e "s#{{SOURCE_SHA}}#${SOURCE_SHA}#g" -e "s#{{SOURCE_SHORT}}#${SOURCE_SHA:0:8}#g" \
           -e "s#{{SOURCE_DATE}}#${SOURCE_DATE}#g" -e "s#{{BASE_VERSION}}#${BASE_VERSION}#g" \
           -e "s#{{BUILD_REPO}}#${BUILD_REPO:-${GITHUB_REPOSITORY:?BUILD_REPO or GITHUB_REPOSITORY}}#g"
