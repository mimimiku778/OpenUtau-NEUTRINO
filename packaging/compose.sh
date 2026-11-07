#!/usr/bin/env bash
# packaging/ の部品から説明文を組み立てる。
# 使い方: compose.sh readme|release|mac|windows
#   readme=リポジトリの README / release=Release の本文 / mac・windows=配布物に同梱する説明書
# 環境変数: SOURCE_REPO SOURCE_REF SOURCE_SHA SOURCE_DATE、BUILD_REPO (無ければ GITHUB_REPOSITORY)
#   BASE_VERSION=固定版の版、LATEST_VERSION=最新版の版 (その版が無ければ空)
#   LATEST_UPSTREAM_SHA・LATEST_UPSTREAM_DATE=最新版に混ぜた本家の master、FLAVOR=同梱の説明書の版 (fixed|latest)
set -euo pipefail
d=$(cd "$(dirname "$0")" && pwd)
BASE_VERSION=${BASE_VERSION:-} LATEST_VERSION=${LATEST_VERSION:-}
LATEST_UPSTREAM_SHA=${LATEST_UPSTREAM_SHA:-} LATEST_UPSTREAM_DATE=${LATEST_UPSTREAM_DATE:-}
# 元のソースに足した変更の一覧 (extra-commits.txt から)。説明が「-」の行は載せない。
changes() {
  awk '!/^[[:space:]]*(#|$)/ { sha = $1; repo = $2; $1 = ""; $2 = ""; sub(/^ +/, "")
       if ($0 == "-") next
       printf "- %s([`%s`](https://github.com/%s/commit/%s))\n", $0, substr(sha, 1, 8), repo, sha }' "$d/extra-commits.txt"
}
# ダウンロードの表。作れた版だけを載せる。
downloads() {
  local b="https://github.com/{{BUILD_REPO}}/releases/latest/download"
  echo "| | Mac(Apple Silicon) | Windows(64bit、動作未確認) |"
  echo "|---|---|---|"
  if [ -n "$BASE_VERSION" ]; then
    echo "| **固定版** OpenUtau v${BASE_VERSION} | [dmg]($b/OpenUtau-NEUTRINO-macos-arm64.dmg) | [zip]($b/OpenUtau-NEUTRINO-win-x64.zip) |"
  fi
  if [ -n "$LATEST_VERSION" ]; then
    echo "| **最新版** OpenUtau v${LATEST_VERSION} | [dmg]($b/OpenUtau-NEUTRINO-latest-macos-arm64.dmg) | [zip]($b/OpenUtau-NEUTRINO-latest-win-x64.zip) |"
  fi
  echo
  echo "固定版と最新版の違いは、下の「本人の配布版・提案との違い」にあります。同じ名前のアプリなので、入れるのはどちらか一方です。"
}
# 2種類の版の説明。
flavors() {
  echo "- 2種類を配っています。NEUTRINO 対応と上の変更は同じで、OpenUtau 本体の版だけが違います。同じ名前のアプリなので、入れるのはどちらか一方です。"
  if [ -n "$BASE_VERSION" ]; then
    echo "  - **固定版**(ファイル名に \`latest\` が付かない方): OpenUtau v${BASE_VERSION}。提案が本家から最後に取り込んだ版のままです。"
  else
    echo "  - **固定版**は、この版では作れませんでした。"
  fi
  if [ -n "$LATEST_VERSION" ]; then
    echo "  - **最新版**(ファイル名に \`latest\` が付く方): OpenUtau v${LATEST_VERSION}。本家の最新(master の [\`${LATEST_UPSTREAM_SHA:0:8}\`](https://github.com/openutau/OpenUtau/commit/${LATEST_UPSTREAM_SHA})、${LATEST_UPSTREAM_DATE})を混ぜたものです。提案の作者も本家も試していない組み合わせなので、動くとは限りません。本家が更新されるたびに作り直します。"
  else
    echo "  - **最新版**(本家の最新を混ぜたもの)は、今は作れていません。"
  fi
}
part() {
  while IFS= read -r line; do
    case "$line" in
      "{{CHANGES}}") changes ;;
      "{{DOWNLOADS}}") downloads ;;
      "{{FLAVORS}}") flavors ;;
      *) printf '%s\n' "$line" ;;
    esac
  done < "$d/$1.md"
}
flavor_name() { case "${FLAVOR:-}" in latest) echo "・最新版" ;; fixed) echo "・固定版" ;; *) echo "" ;; esac; }
case "${1:-}" in
  readme)  { part top; part howto; part about; part notice; part bottom; } ;;
  release) { part howto; part about; part notice; } ;;
  mac)     { echo "# OpenUtau NEUTRINO対応版(非公式)Mac 用$(flavor_name)"; echo; part howto; part about; part notice; } ;;
  windows) { echo "# OpenUtau NEUTRINO対応版(非公式)Windows 用$(flavor_name)"; echo; part howto; part about; part notice; } ;;
  *) echo "usage: $0 readme|release|mac|windows" >&2; exit 2 ;;
esac | sed -e "s#{{SOURCE_REPO}}#${SOURCE_REPO}#g" -e "s#{{SOURCE_REF}}#${SOURCE_REF}#g" \
           -e "s#{{SOURCE_SHA}}#${SOURCE_SHA}#g" -e "s#{{SOURCE_SHORT}}#${SOURCE_SHA:0:8}#g" \
           -e "s#{{SOURCE_DATE}}#${SOURCE_DATE}#g" \
           -e "s#{{BUILD_REPO}}#${BUILD_REPO:-${GITHUB_REPOSITORY:?BUILD_REPO or GITHUB_REPOSITORY}}#g"
