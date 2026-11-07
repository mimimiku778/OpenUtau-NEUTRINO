#!/usr/bin/env bash
# packaging/ の部品から説明文を組み立てる。
# 使い方: compose.sh readme|release|mac|windows
#   readme=リポジトリの README / release=Release の本文 / mac・windows=配布物に同梱する説明書
# 環境変数: BUILD_REPO (無ければ GITHUB_REPOSITORY)、BASE_VERSION・LATEST_VERSION (その版を出していなければ空)、
#   SOURCE_REPO・SOURCE_REF・SOURCE_SHA・LATEST_UPSTREAM_SHA・LATEST_UPSTREAM_DATE (渡さなければ state/last-build.json から)
set -euo pipefail
d=$(cd "$(dirname "$0")" && pwd)
# 渡されていない値は、前に配布した記録 (state/last-build.json) から取る (手元で組む時用。CI は全部渡す)
st="$d/../state/last-build.json"
from_state() { if [ -f "$st" ] && command -v jq > /dev/null; then jq -r "$1 // \"\"" "$st"; fi; }
: "${SOURCE_REPO=$(from_state .source_repo)}" "${SOURCE_REF=$(from_state .source_ref)}" "${SOURCE_SHA=$(from_state .source_sha)}"
: "${BASE_VERSION=$(from_state .version)}" "${LATEST_VERSION=$(from_state .latest.version)}"
: "${LATEST_UPSTREAM_SHA=$(from_state .latest.upstream_sha)}" "${LATEST_UPSTREAM_DATE=$(from_state .latest.upstream_date)}"
SOURCE_REPO=${SOURCE_REPO:-rokujyushi/OpenUtau} SOURCE_REF=${SOURCE_REF:-neutrino} SOURCE_SHA=${SOURCE_SHA:-}
BASE_VERSION=${BASE_VERSION:-} LATEST_VERSION=${LATEST_VERSION:-}
LATEST_UPSTREAM_SHA=${LATEST_UPSTREAM_SHA:-} LATEST_UPSTREAM_DATE=${LATEST_UPSTREAM_DATE:-}
UPSTREAM_REPO=${UPSTREAM_REPO:-openutau/OpenUtau}
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
    echo "| 固定版(OpenUtau ${BASE_VERSION}) | [dmg]($b/OpenUtau-NEUTRINO-macos-arm64.dmg) | [zip]($b/OpenUtau-NEUTRINO-win-x64.zip) |"
  fi
  if [ -n "$LATEST_VERSION" ]; then
    echo "| 最新版(本家 [OpenUtau](https://github.com/openutau/OpenUtau) の最新) | [dmg]($b/OpenUtau-NEUTRINO-latest-macos-arm64.dmg) | [zip]($b/OpenUtau-NEUTRINO-latest-win-x64.zip) |"
  fi
  echo
  echo "固定版は PR のまま、最新版は本家の最新を混ぜたものです(動作保証なし)。入れるのはどちらか一方です。"
}
commit_link() { echo "[${2:0:8}](https://github.com/$1/commit/$2)"; }
# 素の OpenUtau に何を重ねたかの表。出していない版は「出していません」。
layers() {
  local fixed="(今は出していません)" latest="(今は出していません)"
  [ -n "$BASE_VERSION" ] && fixed="$BASE_VERSION(下の NEUTRINO 対応が取り込んだ版)"
  if [ -n "$LATEST_VERSION" ]; then
    latest="$LATEST_VERSION(本家 \`master\` の $(commit_link "$UPSTREAM_REPO" "$LATEST_UPSTREAM_SHA")、${LATEST_UPSTREAM_DATE})"
  fi
  local src="rokujyushi さんの PR [openutau/OpenUtau#2136](https://github.com/openutau/OpenUtau/pull/2136)。ソースは [${SOURCE_REPO}](https://github.com/${SOURCE_REPO}/tree/${SOURCE_REF}) の \`${SOURCE_REF}\` ブランチ($(commit_link "$SOURCE_REPO" "$SOURCE_SHA"))"
  echo "| | 固定版 | 最新版 |"
  echo "|---|---|---|"
  echo "| 1. 本家 OpenUtau | $fixed | $latest |"
  echo "| 2. NEUTRINO 対応 | $src | 同じ |"
  echo "| 3. 画面なしの書き出し(CLI) | christianazinn さんの PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162) と、NEUTRINO 対応版で動くようにした直し | 同じ(本家の最新に当たる形) |"
  echo "| 4. この版で足した機能 | 下の「足した機能」 | 同じ(本家の最新に当たる形) |"
  echo
  echo "VOICEVOX で歌う機能は、1 の本家 OpenUtau に元からある物です。どのコミットを誰のどのブランチから入れたかは、下の「ソース」にあります。"
}
# ビルドしたソースを並べたブランチの表。台帳 (source-branches.txt) が今のソースと合う版だけを載せる。
source_branches() {
  local last_fixed last_latest rows=()
  last_fixed=$(awk '!/^[[:space:]]*(#|$)/ {s = $1} END {print s}' "$d/extra-commits.txt")
  last_latest=$(awk '!/^[[:space:]]*(#|$)/ {s = $1} END {print s}' "$d/extra-commits-latest.txt")
  while read -r flavor branch head src up; do
    case "$flavor" in ''|'#'*) continue ;; esac
    local link="[\`$branch\`](https://github.com/{{BUILD_REPO}}/tree/$branch)"
    if [ "$flavor" = fixed ] && [ -n "$BASE_VERSION" ] && [ "$head" = "$last_fixed" ] && [ "$src" = "$SOURCE_SHA" ]; then
      rows+=("| 固定版 | $link | 2 の \`${SOURCE_REF}\` に、下の表の「固定版」のコミットを上から順に足した物 |")
      clone=$branch
    elif [ "$flavor" = latest ] && [ -n "$LATEST_VERSION" ] && [ "$head" = "$last_latest" ] && [ "$src" = "$SOURCE_SHA" ] && [ "$up" = "$LATEST_UPSTREAM_SHA" ]; then
      rows+=("| 最新版 | $link | 2 の \`${SOURCE_REF}\` に本家の \`master\`($(commit_link "$UPSTREAM_REPO" "$up"))をマージし、下の表の「最新版」のコミットを上から順に足した物 |")
      clone=${clone:-$branch}
    fi
  done < "$d/source-branches.txt"
  if [ ${#rows[@]} -eq 0 ]; then
    echo "(今のソースでは、まだブランチに並べていません。下の手順で組み立ててください)"
    return
  fi
  echo "| | ブランチ | 中身 |"
  echo "|---|---|---|"
  printf '%s\n' "${rows[@]}"
  echo
  echo '```bash'
  echo "git clone -b $clone https://github.com/{{BUILD_REPO}}.git"
  echo '```'
}
# README のコミットの表 (source.md) に、足すコミットが全部載っているかを確かめる
check_source_table() {
  local missing
  missing=$(awk '!/^[[:space:]]*(#|$)/ {print substr($1, 1, 8)}' "$d/extra-commits.txt" "$d/extra-commits-latest.txt" \
    | while read -r c; do grep -q "$c" "$d/source.md" || echo "$c"; done)
  if [ -n "$missing" ]; then
    echo "packaging/source.md のコミットの表に載っていないコミットがあります: $(echo $missing)" >&2
    exit 1
  fi
}
part() {
  while IFS= read -r line; do
    case "$line" in
      "{{CHANGES}}") changes ;;
      "{{DOWNLOADS}}") downloads ;;
      "{{LAYERS}}") layers ;;
      "{{SOURCE_BRANCHES}}") source_branches ;;
      *) printf '%s\n' "$line" ;;
    esac
  done < "$d/$1.md"
}
case "${1:-}" in
  readme)  { check_source_table; part top; part howto; part about; part source; part notice; } ;;
  release) { echo "固定版は OpenUtau ${BASE_VERSION}(PR のまま)、最新版(ファイル名に latest)は本家 [OpenUtau](https://github.com/openutau/OpenUtau) の最新を混ぜたものです(動作保証なし)。入れるのはどちらか一方です。"; echo; part howto; part about; part notice; } ;;
  mac)     { echo "# OpenUtau NEUTRINO対応版(非公式)"; echo; part howto; part about; part notice; } ;;
  windows) { echo "# OpenUtau NEUTRINO対応版(非公式)"; echo; part howto; part about; part notice; } ;;
  *) echo "usage: $0 readme|release|mac|windows" >&2; exit 2 ;;
esac | sed -e "s#{{BUILD_REPO}}#${BUILD_REPO:-${GITHUB_REPOSITORY:?BUILD_REPO or GITHUB_REPOSITORY}}#g"
