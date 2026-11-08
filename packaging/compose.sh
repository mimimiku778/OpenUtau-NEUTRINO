#!/usr/bin/env bash
# packaging/ の部品から説明文を組み立てる。
# 使い方: compose.sh readme|release|mac|windows
#   readme=リポジトリの README / release=Release の本文 (ダウンロードの表と README への 1 行だけ) / mac・windows=配布物に同梱する説明書
# 環境変数: BUILD_REPO (無ければ GITHUB_REPOSITORY)、BASE_VERSION・LATEST_VERSION (その版を出していなければ空)、
#   SOURCE_REPO・SOURCE_REF・SOURCE_SHA・LATEST_UPSTREAM_SHA・LATEST_UPSTREAM_DATE (渡さなければ state/last-build.json から)
# 部品: top.md・howto.md・about.md・source.md・notice.md (文章)、install-mac.md・install-windows.md (入れ方。mac・windows ではその OS の物だけ)、features.txt (足した機能の台帳)、
#   extra-commits.txt・extra-commits-latest.txt (足すコミット。3 列目の key で features.txt と結ぶ)、source-branches.txt (ソースを並べたブランチ)
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
commit_link() { echo "[${2:0:8}](https://github.com/$1/commit/$2)"; }
# features.txt を 1 行ずつ、US (\037) 区切り (key, 誰の物か, 題, これまで, この版, 本家向けブランチとコミット) で出す
# (タブだと空の列が詰まるので、空白でない区切りにする)
US=$'\037'
features() {
  awk -F' *\\| *' '!/^[[:space:]]*(#|$)/ {
    for (i = 1; i <= 6; i++) gsub(/^ +| +$/, "", $i)
    print $1 "\037" $2 "\037" $3 "\037" $4 "\037" $5 "\037" $6 }' "$d/features.txt"
}
# 足した機能の表 (これまで → この版では)。「この版」が空の行は載せない。題の下に最新版のコミットへのリンク (3 本以上は先頭と「ほか N 本」)。
# README では、packaging/features/<key>.md があれば題からそこへ飛べるようにし、表の下に見出しを付けて並べる
# (各 .md は「何ができる → どこで設定 → こうするとこうなるの表 → 図」。図は説明の 1〜2 行の直後に 1 枚ずつ、後ろに <br> で隙間)。
changes() {
  if [ "$MODE" = readme ]; then
    echo "題にリンクがある物は、表の下に設定する所と実測があります。"
    echo
  fi
  echo "| 機能 | これまで(元の PR・本家) | この版では |"
  echo "|---|---|---|"
  features | while IFS=$US read -r key owner title before after fix; do
    [ -n "$after" ] || continue
    local t="**$title**"
    if [ "$MODE" = readme ] && [ -f "$d/features/$key.md" ]; then t="[**$title**](#$key)"; fi
    local br="<br>"; [ "$MODE" = mac ] || [ "$MODE" = windows ] && br=" "
    echo "| $t$br$(commits_brief "$d/extra-commits-latest.txt" "$key") | $before | $after |"
  done
  [ "$MODE" = readme ] || return 0
  features | while IFS=$US read -r key owner title before after fix; do
    [ -f "$d/features/$key.md" ] || continue
    printf '\n<a id="%s"></a>\n\n### %s\n\n' "$key" "$title"
    cat "$d/features/$key.md"
  done
}
# <一覧> <key> → コミットへのリンク。3 本以上なら先頭の 1 本と「ほか N 本」(全部の並びは extra-commits*.txt)
commits_brief() {
  local links n
  links=$(commits_of "$1" "$2")
  n=$(wc -w <<< "$links")
  if [ "$n" -gt 2 ]; then echo "${links%% *} ほか $((n - 1)) 本"; else echo "$links"; fi
}
# <一覧> <key> → その key のコミットへのリンクを空白区切りで。sha が決まっていない行 (40 桁でない) は「未記入」、1 行も無ければ「—」
commits_of() {
  local out
  out=$(awk -v k="$2" '!/^[[:space:]]*(#|$)/ && $3 == k {print $1, $2}' "$1" | while read -r sha repo; do
    if [[ $sha =~ ^[0-9a-f]{40}$ ]]; then commit_link "$repo" "$sha"; else echo "**未記入**"; fi
  done | paste -s -d ' ' -)
  echo "${out:-—}"
}
# 足したコミットの表: 機能 → 誰の物か → 固定版・最新版のコミット → 本家 master 向けのブランチ
commits_table() {
  echo "| 中身 | 誰の物か | 固定版 | 最新版 | 本家 \`master\` 向け |"
  echo "|---|---|---|---|---|"
  features | while IFS=$US read -r key owner title before after fix; do
    local up="" branch sha
    if [ -n "$fix" ]; then
      read -r branch sha <<< "$fix"
      if [[ ${sha:-} =~ ^[0-9a-f]{40}$ ]]; then
        up="[\`$branch\`](https://github.com/{{BUILD_REPO}}/tree/$branch) $(commit_link '{{BUILD_REPO}}' "$sha")"
      else up="**未記入**"; fi
    fi
    echo "| $title | $owner | $(commits_brief "$d/extra-commits.txt" "$key") | $(commits_brief "$d/extra-commits-latest.txt" "$key") | $up |"
  done
}
# 足すコミットの key が全部 features.txt にあるか (表から漏れないように)
check_keys() {
  local keys bad
  keys=$(features | cut -d "$US" -f1)
  bad=$(awk '!/^[[:space:]]*(#|$)/ {print ($3 == "" ? "(key なし: " substr($1, 1, 8) ")" : $3)}' "$d/extra-commits.txt" "$d/extra-commits-latest.txt" \
    | sort -u | while read -r k; do grep -qxF -- "$k" <<< "$keys" || echo "$k"; done)
  if [ -n "$bad" ]; then
    echo "packaging/features.txt に無い key が extra-commits*.txt にあります: $(echo $bad)" >&2
    exit 1
  fi
}
# ダウンロードの表。出している版だけを載せる。README ではリンク、Release の本文では添付のファイル名。
downloads() {
  local b="https://github.com/{{BUILD_REPO}}/releases/latest/download"
  local fm=OpenUtau-NEUTRINO-macos-arm64.dmg fw=OpenUtau-NEUTRINO-win-x64.zip
  local lm=OpenUtau-NEUTRINO-latest-macos-arm64.dmg lw=OpenUtau-NEUTRINO-latest-win-x64.zip
  local fixed="OpenUtau ${BASE_VERSION}。NEUTRINO 対応の PR のまま" latest="本家の最新 ${LATEST_VERSION} を混ぜた物。動作保証なし"
  echo "| | 中身 | Mac | Windows(動作未確認) |"
  echo "|---|---|---|---|"
  if [ "$MODE" = readme ]; then
    [ -z "$BASE_VERSION" ] || echo "| **固定版** | $fixed | [dmg]($b/$fm) | [zip]($b/$fw) |"
    [ -z "$LATEST_VERSION" ] || echo "| **最新版** | $latest | [dmg]($b/$lm) | [zip]($b/$lw) |"
  else
    [ -z "$BASE_VERSION" ] || echo "| **固定版** | $fixed | \`$fm\` | \`$fw\` |"
    [ -z "$LATEST_VERSION" ] || echo "| **最新版** | $latest | \`$lm\` | \`$lw\` |"
  fi
  echo
  echo "入れるのはどちらか一方です。"
  if [ "$MODE" = readme ]; then
    echo "前に出した版は [Releases](https://github.com/{{BUILD_REPO}}/releases) に残しています(名前に中の OpenUtau の版)。動かなくなった時は、そこから前の版を入れ直してください。"
  fi
}
# 素の OpenUtau に何を重ねたか。出していない版は「今は出していません」。
layers() {
  local fixed="今は出していません" latest="今は出していません"
  [ -n "$BASE_VERSION" ] && fixed="$BASE_VERSION(2 が取り込んだ版)"
  if [ -n "$LATEST_VERSION" ]; then
    latest="$LATEST_VERSION(本家 \`master\` の $(commit_link "$UPSTREAM_REPO" "$LATEST_UPSTREAM_SHA")、${LATEST_UPSTREAM_DATE})"
  fi
  echo "| | 重ねた物 | 中身 |"
  echo "|---|---|---|"
  echo "| 1 | 本家 [OpenUtau](https://github.com/${UPSTREAM_REPO}) | 固定版は ${fixed}、最新版は ${latest}。VOICEVOX で歌う機能はここに元からある |"
  echo "| 2 | NEUTRINO 対応 | rokujyushi さんの PR [openutau/OpenUtau#2136](https://github.com/openutau/OpenUtau/pull/2136)。[${SOURCE_REPO}](https://github.com/${SOURCE_REPO}/tree/${SOURCE_REF}) の \`${SOURCE_REF}\` ブランチ($(commit_link "$SOURCE_REPO" "$SOURCE_SHA")) |"
  echo "| 3 | 画面なしの書き出し(CLI) | christianazinn さんの PR [openutau/OpenUtau#2162](https://github.com/openutau/OpenUtau/pull/2162) を、NEUTRINO 対応版で動くように直した物 |"
  echo "| 4 | この版で足した機能 | 上の「[足した機能](#足した機能)」 |"
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
      rows+=("| 固定版 | $link | 2 の \`${SOURCE_REF}\` に、下の表の「固定版」のコミットを足した物 |")
      clone=$branch
    elif [ "$flavor" = latest ] && [ -n "$LATEST_VERSION" ] && [ "$head" = "$last_latest" ] && [ "$src" = "$SOURCE_SHA" ] && [ "$up" = "$LATEST_UPSTREAM_SHA" ]; then
      rows+=("| 最新版 | $link | 2 の \`${SOURCE_REF}\` に本家の \`master\`($(commit_link "$UPSTREAM_REPO" "$up"))をマージし、下の表の「最新版」のコミットを足した物 |")
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
part() {
  while IFS= read -r line; do
    case "$line" in
      "{{CHANGES}}") changes ;;
      "{{COMMITS}}") commits_table ;;
      "{{DOWNLOADS}}") downloads ;;
      "{{LAYERS}}") layers ;;
      "{{SOURCE_BRANCHES}}") source_branches ;;
      "{{INSTALL}}") case "$MODE" in mac) part install-mac ;; windows) part install-windows ;; *) part install-mac; echo; part install-windows ;; esac ;;
      *) printf '%s\n' "$line" ;;
    esac
  done < "$d/$1.md"
}
check_keys
MODE=${1:-}
case "${1:-}" in
  readme)  { part top; part howto; part about; part source; part notice; } ;;
  # Release の本文は落とす物だけ。入れ方・機能・注意は README に任せて重ねない
  release) { downloads; echo; r="https://github.com/{{BUILD_REPO}}"
    echo "[入れ方]($r#入れ方)・[足した機能]($r#足した機能)は README にあります。NEUTRINO 本体(v3.2 以上)は[公式](https://studio-neutrino.com/)から別に入れてください。非公式・無保証です。"; } ;;
  mac)     { echo "# OpenUtau NEUTRINO対応版(非公式)"; echo; part howto; part about; part notice; } ;;
  windows) { echo "# OpenUtau NEUTRINO対応版(非公式)"; echo; part howto; part about; part notice; } ;;
  *) echo "usage: $0 readme|release|mac|windows" >&2; exit 2 ;;
esac | sed -e "s#{{BUILD_REPO}}#${BUILD_REPO:-${GITHUB_REPOSITORY:?BUILD_REPO or GITHUB_REPOSITORY}}#g"
