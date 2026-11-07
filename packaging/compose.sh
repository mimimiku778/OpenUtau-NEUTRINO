#!/usr/bin/env bash
# packaging/ の部品から説明文を組み立てる。
# 使い方: compose.sh readme|release|mac|windows
#   readme=リポジトリの README / release=Release の本文 / mac・windows=配布物に同梱する説明書
# 環境変数: BUILD_REPO (無ければ GITHUB_REPOSITORY)、BASE_VERSION・LATEST_VERSION (その版を出していなければ空)
set -euo pipefail
d=$(cd "$(dirname "$0")" && pwd)
BASE_VERSION=${BASE_VERSION:-} LATEST_VERSION=${LATEST_VERSION:-}
# 足した機能の一覧 (extra-commits.txt の説明から)。説明が「-」の行は載せない。
changes() {
  awk '!/^[[:space:]]*(#|$)/ { $1 = ""; $2 = ""; sub(/^ +/, ""); if ($0 != "-") printf "- %s\n", $0 }' "$d/extra-commits.txt"
}
# ダウンロードの表。出している版だけを載せる。
downloads() {
  local b="https://github.com/{{BUILD_REPO}}/releases/latest/download"
  echo "| | Mac | Windows(動作未確認) |"
  echo "|---|---|---|"
  if [ -n "$BASE_VERSION" ]; then
    echo "| 固定版 | [dmg]($b/OpenUtau-NEUTRINO-macos-arm64.dmg) | [zip]($b/OpenUtau-NEUTRINO-win-x64.zip) |"
  fi
  if [ -n "$LATEST_VERSION" ]; then
    echo "| 最新版 | [dmg]($b/OpenUtau-NEUTRINO-latest-macos-arm64.dmg) | [zip]($b/OpenUtau-NEUTRINO-latest-win-x64.zip) |"
  fi
  echo
  echo "最新版は本家 OpenUtau の最新を混ぜたもの(動作保証なし)。入れるのはどちらか一方です。"
}
part() {
  while IFS= read -r line; do
    case "$line" in
      "{{CHANGES}}") changes ;;
      "{{DOWNLOADS}}") downloads ;;
      *) printf '%s\n' "$line" ;;
    esac
  done < "$d/$1.md"
}
case "${1:-}" in
  readme)  { part top; part howto; part about; part notice; } ;;
  release) { echo "最新版(ファイル名に latest)は本家 OpenUtau の最新を混ぜたもの(動作保証なし)。入れるのはどちらか一方です。"; echo; part howto; part about; part notice; } ;;
  mac)     { echo "# OpenUtau NEUTRINO対応版(非公式)"; echo; part howto; part about; part notice; } ;;
  windows) { echo "# OpenUtau NEUTRINO対応版(非公式)"; echo; part howto; part about; part notice; } ;;
  *) echo "usage: $0 readme|release|mac|windows" >&2; exit 2 ;;
esac | sed -e "s#{{BUILD_REPO}}#${BUILD_REPO:-${GITHUB_REPOSITORY:?BUILD_REPO or GITHUB_REPOSITORY}}#g"
