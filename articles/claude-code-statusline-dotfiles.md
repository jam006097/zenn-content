---
title: "Claude Code のステータスバーをカスタマイズして使用状況を一目で把握する"
emoji: "📊"
type: "tech"
topics:
  - claude
  - claudecode
  - bash
  - dotfiles
  - terminal
published: true
---

## はじめに

Claude Code のステータスバーは、シェルスクリプトの出力をそのまま表示する仕組みです。`settings.json` に設定を追加するだけで有効になり、表示内容は完全に自由に組み立てられます。

この記事では完成したスクリプトの**全文**を示した上で、各表示項目をどのコードで実現しているかを解説します。コードをそのまま使えば同じステータスバーが作れます。

完成したコードは [GitHub](https://github.com/jam006097/dotfiles/tree/main/claude) で公開しています。

## 完成イメージ

```
claude-sonnet-4-6 │ ✍️ 12% │ zenn-content (main) │ ⏱ 5m │ ◑ default

current ●●○○○○○○○○  12% ⟳ 3:45pm
weekly  ●○○○○○○○○○   8% ⟳ may 3
```

上段にモデル名・コンテキスト使用率・作業ディレクトリ（Git ブランチ）・セッション時間・Effort レベル、下段にレートリミットのプログレスバーが表示されます。

## 事前準備

`jq`・`curl`・`git` が必要です。

```bash
# macOS
brew install jq curl git
```

## ファイル構成

`~/.claude/` に2つのファイルを置きます。

```
~/.claude/
├── settings.json    # ステータスバーの有効化設定
└── statusline.sh    # 表示内容を生成するスクリプト
```

## 1. settings.json

```json
{
  "statusLine": {
    "type": "command",
    "command": "bash \"$HOME/.claude/statusline.sh\""
  }
}
```

Claude Code は状態が更新されるたびにこのコマンドを実行し、スクリプトの標準出力をそのままステータスバーに表示します。すでに `settings.json` がある場合は `statusLine` キーだけ追記してください。

## 2. statusline.sh（完全版）

以下のスクリプトを `~/.claude/statusline.sh` として保存します。

:::details statusline.sh 全文（クリックで展開）
```bash
#!/bin/bash
set -f

input=$(cat)

if [ -z "$input" ]; then
    printf "Claude"
    exit 0
fi

# ── Colors ──────────────────────────────────────────────
blue='\033[38;2;0;153;255m'
orange='\033[38;2;255;176;85m'
green='\033[38;2;0;175;80m'
cyan='\033[38;2;86;182;194m'
red='\033[38;2;255;85;85m'
yellow='\033[38;2;230;200;0m'
white='\033[38;2;220;220;220m'
magenta='\033[38;2;180;140;255m'
dim='\033[2m'
reset='\033[0m'

sep=" ${dim}│${reset} "

# ── Helpers ─────────────────────────────────────────────
color_for_pct() {
    local pct=$1
    if [ "$pct" -ge 90 ]; then printf "$red"
    elif [ "$pct" -ge 70 ]; then printf "$yellow"
    elif [ "$pct" -ge 50 ]; then printf "$orange"
    else printf "$green"
    fi
}

build_bar() {
    local pct=$1
    local width=$2
    [ "$pct" -lt 0 ] 2>/dev/null && pct=0
    [ "$pct" -gt 100 ] 2>/dev/null && pct=100

    local filled=$(( pct * width / 100 ))
    local empty=$(( width - filled ))
    local bar_color
    bar_color=$(color_for_pct "$pct")

    local filled_str="" empty_str=""
    for ((i=0; i<filled; i++)); do filled_str+="●"; done
    for ((i=0; i<empty; i++)); do empty_str+="○"; done

    printf "${bar_color}${filled_str}${dim}${empty_str}${reset}"
}

format_epoch_time() {
    local epoch=$1
    local style=$2
    [ -z "$epoch" ] || [ "$epoch" = "null" ] || [ "$epoch" = "0" ] && return

    local result=""
    case "$style" in
        time)
            result=$(date -j -r "$epoch" +"%l:%M%p" 2>/dev/null)
            [ -z "$result" ] && result=$(date -d "@$epoch" +"%l:%M%P" 2>/dev/null)
            result=$(echo "$result" | sed 's/^ //; s/\.//g' | tr '[:upper:]' '[:lower:]')
            ;;
        datetime)
            result=$(date -j -r "$epoch" +"%b %-d, %l:%M%p" 2>/dev/null)
            [ -z "$result" ] && result=$(date -d "@$epoch" +"%b %-d, %l:%M%P" 2>/dev/null)
            result=$(echo "$result" | sed 's/  / /g; s/^ //; s/\.//g' | tr '[:upper:]' '[:lower:]')
            ;;
        *)
            result=$(date -j -r "$epoch" +"%b %-d" 2>/dev/null)
            [ -z "$result" ] && result=$(date -d "@$epoch" +"%b %-d" 2>/dev/null)
            result=$(echo "$result" | tr '[:upper:]' '[:lower:]')
            ;;
    esac
    printf "%s" "$result"
}

iso_to_epoch() {
    local iso_str="$1"

    local epoch
    epoch=$(date -d "${iso_str}" +%s 2>/dev/null)
    if [ -n "$epoch" ]; then
        echo "$epoch"
        return 0
    fi

    local stripped="${iso_str%%.*}"
    stripped="${stripped%%Z}"
    stripped="${stripped%%+*}"
    stripped="${stripped%%-[0-9][0-9]:[0-9][0-9]}"

    if [[ "$iso_str" == *"Z"* ]] || [[ "$iso_str" == *"+00:00"* ]] || [[ "$iso_str" == *"-00:00"* ]]; then
        epoch=$(env TZ=UTC date -j -f "%Y-%m-%dT%H:%M:%S" "$stripped" +%s 2>/dev/null)
        [ -z "$epoch" ] && epoch=$(env TZ=UTC date -d "${stripped/T/ }" +%s 2>/dev/null)
    else
        epoch=$(date -j -f "%Y-%m-%dT%H:%M:%S" "$stripped" +%s 2>/dev/null)
        [ -z "$epoch" ] && epoch=$(date -d "${stripped/T/ }" +%s 2>/dev/null)
    fi

    if [ -n "$epoch" ]; then
        echo "$epoch"
        return 0
    fi

    return 1
}

# ── Extract JSON data ───────────────────────────────────
model_name=$(echo "$input" | jq -r '.model.display_name // "Claude"')

size=$(echo "$input" | jq -r '.context_window.context_window_size // 200000')
[ "$size" -eq 0 ] 2>/dev/null && size=200000

input_tokens=$(echo "$input" | jq -r '.context_window.current_usage.input_tokens // 0')
cache_create=$(echo "$input" | jq -r '.context_window.current_usage.cache_creation_input_tokens // 0')
cache_read=$(echo "$input" | jq -r '.context_window.current_usage.cache_read_input_tokens // 0')
current=$(( input_tokens + cache_create + cache_read ))

if [ "$size" -gt 0 ]; then
    pct_used=$(( current * 100 / size ))
else
    pct_used=0
fi

effort="default"
settings_path="$HOME/.claude/settings.json"
if [ -f "$settings_path" ]; then
    effort=$(jq -r '.effortLevel // "default"' "$settings_path" 2>/dev/null)
fi

# ── LINE 1 ──────────────────────────────────────────────
pct_color=$(color_for_pct "$pct_used")
cwd=$(echo "$input" | jq -r '.cwd // ""')
[ -z "$cwd" ] || [ "$cwd" = "null" ] && cwd=$(pwd)
dirname=$(basename "$cwd")

git_branch=""
git_dirty=""
if git -C "$cwd" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    git_branch=$(git -C "$cwd" symbolic-ref --short HEAD 2>/dev/null)
    if [ -n "$(git -C "$cwd" --no-optional-locks status --porcelain 2>/dev/null)" ]; then
        git_dirty="*"
    fi
fi

session_duration=""
session_start=$(echo "$input" | jq -r '.session.start_time // empty')
if [ -n "$session_start" ] && [ "$session_start" != "null" ]; then
    start_epoch=$(iso_to_epoch "$session_start")
    if [ -n "$start_epoch" ]; then
        now_epoch=$(date +%s)
        elapsed=$(( now_epoch - start_epoch ))
        if [ "$elapsed" -ge 3600 ]; then
            session_duration="$(( elapsed / 3600 ))h$(( (elapsed % 3600) / 60 ))m"
        elif [ "$elapsed" -ge 60 ]; then
            session_duration="$(( elapsed / 60 ))m"
        else
            session_duration="${elapsed}s"
        fi
    fi
fi

skip_perms=""
parent_cmd=$(ps -o args= -p "$PPID" 2>/dev/null)
if [[ "$parent_cmd" == *"--dangerously-skip-permissions"* ]]; then
    skip_perms="⚡  "
fi

line1="${blue}${model_name}${reset}"
line1+="${sep}"
line1+="✍️ ${pct_color}${pct_used}%${reset}"
line1+="${sep}"
line1+="${skip_perms}${cyan}${dirname}${reset}"
if [ -n "$git_branch" ]; then
    line1+=" ${green}(${git_branch}${red}${git_dirty}${green})${reset}"
fi
if [ -n "$session_duration" ]; then
    line1+="${sep}"
    line1+="${dim}⏱ ${reset}${white}${session_duration}${reset}"
fi
line1+="${sep}"
case "$effort" in
    high)   line1+="${magenta}● ${effort}${reset}" ;;
    medium) line1+="${dim}◑ ${effort}${reset}" ;;
    low)    line1+="${dim}◔ ${effort}${reset}" ;;
    *)      line1+="${dim}◑ ${effort}${reset}" ;;
esac

# ── Rate limits from stdin ───────────────────────────────
has_stdin_rates=false
five_hour_pct=""
five_hour_reset_epoch=""
seven_day_pct=""
seven_day_reset_epoch=""

stdin_five_pct=$(echo "$input" | jq -r '.rate_limits.five_hour.used_percentage // empty')
if [ -n "$stdin_five_pct" ]; then
    has_stdin_rates=true
    five_hour_pct=$(printf "%.0f" "$stdin_five_pct")
    five_hour_reset_epoch=$(echo "$input" | jq -r '.rate_limits.five_hour.resets_at // empty')
    seven_day_pct=$(echo "$input" | jq -r '.rate_limits.seven_day.used_percentage // empty' | awk '{printf "%.0f", $1}')
    seven_day_reset_epoch=$(echo "$input" | jq -r '.rate_limits.seven_day.resets_at // empty')
fi

# ── Fallback: API call (cached 60s) ─────────────────────
cache_file="/tmp/claude/statusline-usage-cache.json"
cache_max_age=60
mkdir -p /tmp/claude

usage_data=""
extra_enabled="false"

if ! $has_stdin_rates; then
    needs_refresh=true

    if [ -f "$cache_file" ]; then
        cache_mtime=$(stat -c %Y "$cache_file" 2>/dev/null || stat -f %m "$cache_file" 2>/dev/null)
        now=$(date +%s)
        cache_age=$(( now - cache_mtime ))
        if [ "$cache_age" -lt "$cache_max_age" ]; then
            needs_refresh=false
            usage_data=$(cat "$cache_file" 2>/dev/null)
        fi
    fi

    if $needs_refresh; then
        token=""
        if [ -n "$CLAUDE_CODE_OAUTH_TOKEN" ]; then
            token="$CLAUDE_CODE_OAUTH_TOKEN"
        elif command -v security >/dev/null 2>&1; then
            blob=$(security find-generic-password -s "Claude Code-credentials" -w 2>/dev/null)
            if [ -n "$blob" ]; then
                token=$(echo "$blob" | jq -r '.claudeAiOauth.accessToken // empty' 2>/dev/null)
            fi
        fi
        if [ -z "$token" ] || [ "$token" = "null" ]; then
            creds_file="${HOME}/.claude/.credentials.json"
            if [ -f "$creds_file" ]; then
                token=$(jq -r '.claudeAiOauth.accessToken // empty' "$creds_file" 2>/dev/null)
            fi
        fi

        if [ -n "$token" ] && [ "$token" != "null" ]; then
            response=$(curl -s --max-time 5 \
                -H "Accept: application/json" \
                -H "Content-Type: application/json" \
                -H "Authorization: Bearer $token" \
                -H "anthropic-beta: oauth-2025-04-20" \
                -H "User-Agent: claude-code/2.1.34" \
                "https://api.anthropic.com/api/oauth/usage" 2>/dev/null)
            if [ -n "$response" ] && echo "$response" | jq -e '.five_hour' >/dev/null 2>&1; then
                usage_data="$response"
                echo "$response" > "$cache_file"
            fi
        fi
        if [ -z "$usage_data" ] && [ -f "$cache_file" ]; then
            usage_data=$(cat "$cache_file" 2>/dev/null)
        fi
    fi

    if [ -n "$usage_data" ] && echo "$usage_data" | jq -e . >/dev/null 2>&1; then
        five_hour_pct=$(echo "$usage_data" | jq -r '.five_hour.utilization // 0' | awk '{printf "%.0f", $1}')
        five_hour_reset_iso=$(echo "$usage_data" | jq -r '.five_hour.resets_at // empty')
        five_hour_reset_epoch=$(iso_to_epoch "$five_hour_reset_iso")
        seven_day_pct=$(echo "$usage_data" | jq -r '.seven_day.utilization // 0' | awk '{printf "%.0f", $1}')
        seven_day_reset_iso=$(echo "$usage_data" | jq -r '.seven_day.resets_at // empty')
        seven_day_reset_epoch=$(iso_to_epoch "$seven_day_reset_iso")
        extra_enabled=$(echo "$usage_data" | jq -r '.extra_usage.is_enabled // false')
    fi
else
    if [ -f "$cache_file" ]; then
        usage_data=$(cat "$cache_file" 2>/dev/null)
        if [ -n "$usage_data" ] && echo "$usage_data" | jq -e . >/dev/null 2>&1; then
            extra_enabled=$(echo "$usage_data" | jq -r '.extra_usage.is_enabled // false')
        fi
    fi
fi

# ── Rate limit lines ─────────────────────────────────────
rate_lines=""
bar_width=10

if [ -n "$five_hour_pct" ]; then
    five_hour_reset=$(format_epoch_time "$five_hour_reset_epoch" "time")
    five_hour_bar=$(build_bar "$five_hour_pct" "$bar_width")
    five_hour_pct_color=$(color_for_pct "$five_hour_pct")
    five_hour_pct_fmt=$(printf "%3d" "$five_hour_pct")

    rate_lines+="${white}current${reset} ${five_hour_bar} ${five_hour_pct_color}${five_hour_pct_fmt}%${reset}"
    [ -n "$five_hour_reset" ] && rate_lines+=" ${dim}⟳${reset} ${white}${five_hour_reset}${reset}"
fi

if [ -n "$seven_day_pct" ]; then
    seven_day_reset=$(format_epoch_time "$seven_day_reset_epoch" "datetime")
    seven_day_bar=$(build_bar "$seven_day_pct" "$bar_width")
    seven_day_pct_color=$(color_for_pct "$seven_day_pct")
    seven_day_pct_fmt=$(printf "%3d" "$seven_day_pct")

    [ -n "$rate_lines" ] && rate_lines+="\n"
    rate_lines+="${white}weekly${reset}  ${seven_day_bar} ${seven_day_pct_color}${seven_day_pct_fmt}%${reset}"
    [ -n "$seven_day_reset" ] && rate_lines+=" ${dim}⟳${reset} ${white}${seven_day_reset}${reset}"
fi

if [ "$extra_enabled" = "true" ] && [ -n "$usage_data" ]; then
    extra_pct=$(echo "$usage_data" | jq -r '.extra_usage.utilization // 0' | awk '{printf "%.0f", $1}')
    extra_used=$(echo "$usage_data" | jq -r '.extra_usage.used_credits // 0' | awk '{printf "%.2f", $1/100}')
    extra_limit=$(echo "$usage_data" | jq -r '.extra_usage.monthly_limit // 0' | awk '{printf "%.2f", $1/100}')
    extra_bar=$(build_bar "$extra_pct" "$bar_width")
    extra_pct_color=$(color_for_pct "$extra_pct")

    extra_reset=$(date -v+1m -v1d +"%b %-d" 2>/dev/null | tr '[:upper:]' '[:lower:]')
    if [ -z "$extra_reset" ]; then
        extra_reset=$(date -d "$(date +%Y-%m-01) +1 month" +"%b %-d" 2>/dev/null | tr '[:upper:]' '[:lower:]')
    fi

    [ -n "$rate_lines" ] && rate_lines+="\n"
    rate_lines+="${white}extra${reset}   ${extra_bar} ${extra_pct_color}\$${extra_used}${dim}/${reset}${white}\$${extra_limit}${reset} ${dim}⟳${reset} ${white}${extra_reset}${reset}"
fi

# ── Output ───────────────────────────────────────────────
printf "%b" "$line1"
[ -n "$rate_lines" ] && printf "\n\n%b" "$rate_lines"

exit 0
```
:::

## スクリプトの解説

全体の流れは「JSON を受け取る → 各値を取り出す → 1行目を組み立てる → レートリミットを取得して出力する」です。

### 仕組み：Claude Code がスクリプトに JSON を渡す

Claude Code はスクリプトの**標準入力**に JSON を渡します。`input=$(cat)` で受け取り、空なら即終了します。

渡される JSON の構造は次のとおりです。

```json
{
  "model": { "display_name": "claude-sonnet-4-6" },
  "context_window": {
    "context_window_size": 200000,
    "current_usage": {
      "input_tokens": 24000,
      "cache_creation_input_tokens": 0,
      "cache_read_input_tokens": 0
    }
  },
  "cwd": "/Users/you/project",
  "session": { "start_time": "2026-05-03T10:00:00Z" },
  "rate_limits": {
    "five_hour": { "used_percentage": 12.5, "resets_at": "..." },
    "seven_day":  { "used_percentage": 8.0,  "resets_at": "..." }
  }
}
```

以降はこの JSON を `jq` で読み取って各値を取り出します。

### モデル名の表示

```bash
model_name=$(echo "$input" | jq -r '.model.display_name // "Claude"')
```

`jq -r` で JSON から文字列を取り出します。`// "Claude"` はキーが存在しないときのフォールバックです。

### コンテキスト使用率の計算

コンテキストは3種類のトークンに分かれているため、すべて合算してから割り算します。

```bash
input_tokens=$(echo "$input" | jq -r '.context_window.current_usage.input_tokens // 0')
cache_create=$(echo "$input" | jq -r '.context_window.current_usage.cache_creation_input_tokens // 0')
cache_read=$(echo "$input" | jq -r '.context_window.current_usage.cache_read_input_tokens // 0')
current=$(( input_tokens + cache_create + cache_read ))
pct_used=$(( current * 100 / size ))
```

使用率に応じて色を変えるのが `color_for_pct` 関数です。50% 超でオレンジ、70% 超で黄、90% 超で赤になります。

### 作業ディレクトリと Git ブランチ

`.cwd` でカレントディレクトリのパスが渡されます。`basename` でフォルダ名だけ取り出し、`git` コマンドでブランチ名と未コミット変更の有無を取得します。

```bash
dirname=$(basename "$cwd")
git_branch=$(git -C "$cwd" symbolic-ref --short HEAD 2>/dev/null)
git -C "$cwd" --no-optional-locks status --porcelain 2>/dev/null
```

:::message
`--no-optional-locks` は必須です。Claude Code がファイルを並列操作しているときに `git status` がロックファイルを作成すると競合が起きるため、ロック取得をスキップしています。
:::

未コミット変更があれば `git_dirty="*"` をセットし、ブランチ名の後ろに `*` を付けて出力します。

### セッション経過時間の計算

`.session.start_time` が ISO 8601 形式（`2026-05-03T10:00:00Z`）で渡されます。これを Unix タイムスタンプに変換して現在時刻との差を計算します。

macOS と Linux で `date` コマンドの書式が異なるため、`iso_to_epoch` 関数で両方を試みています。変換できたら `elapsed` 秒を `5m` や `1h30m` の形式に整形します。

### Effort レベルの取得

Effort レベルは JSON には含まれません。`settings.json` を直接 `jq` で読み取ります。

```bash
effort=$(jq -r '.effortLevel // "default"' "$HOME/.claude/settings.json" 2>/dev/null)
```

`high` / `medium` / `low` / `default` に応じてアイコン（`●` `◑` `◔`）と色を変えています。

### レートリミットの取得（stdin → API フォールバック）

表示される3行はそれぞれ異なるソースと計算方法を持っています。

```
current ●●●●○○○○○○  49% ⟳ 1:10am
weekly  ●○○○○○○○○○  17% ⟳ may 4, 2:00am
extra   ○○○○○○○○○○ $0.00/$20.00 ⟳ jun 1
```

#### データの取得元：stdin 優先、API フォールバック

レートリミットはまず stdin の JSON から取得を試みます。

```bash
stdin_five_pct=$(echo "$input" | jq -r '.rate_limits.five_hour.used_percentage // empty')
```

`.rate_limits.five_hour.used_percentage` が存在すれば stdin から使います。存在しない場合は Anthropic API（`/api/oauth/usage`）を呼び出します。認証トークンは macOS キーチェーン → `~/.claude/.credentials.json` → 環境変数 `CLAUDE_CODE_OAUTH_TOKEN` の順で探します。

API の結果は `/tmp/claude/statusline-usage-cache.json` に **60 秒間キャッシュ**します。ステータスバーは頻繁に更新されるため、毎回 API を呼ぶと表示が遅くなるためです。

#### current（5時間ウィンドウ）

```
current ●●●●○○○○○○  49% ⟳ 1:10am
```

`49%` は5時間ウィンドウの使用率です。stdin からは `.rate_limits.five_hour.used_percentage`、API からは `.five_hour.utilization` で取得します。小数で渡されるため `printf "%.0f"` や `awk` で整数に丸めます。

`⟳ 1:10am` はリセット時刻です。`.rate_limits.five_hour.resets_at`（ISO 8601 形式）を `iso_to_epoch` で Unix タイムスタンプに変換し、`format_epoch_time "$epoch" "time"` で時刻のみの表示（`1:10am`）に整形しています。

```bash
five_hour_pct=$(printf "%.0f" "$stdin_five_pct")
five_hour_reset_epoch=$(echo "$input" | jq -r '.rate_limits.five_hour.resets_at // empty')

five_hour_reset=$(format_epoch_time "$five_hour_reset_epoch" "time")  # → "1:10am"
five_hour_bar=$(build_bar "$five_hour_pct" "$bar_width")
five_hour_pct_fmt=$(printf "%3d" "$five_hour_pct")                   # 右揃え3桁

rate_lines+="${white}current${reset} ${five_hour_bar} ${five_hour_pct_color}${five_hour_pct_fmt}%${reset}"
[ -n "$five_hour_reset" ] && rate_lines+=" ${dim}⟳${reset} ${white}${five_hour_reset}${reset}"
```

#### weekly（7日間ウィンドウ）

```
weekly  ●○○○○○○○○○  17% ⟳ may 4, 2:00am
```

`17%` は7日間ウィンドウの使用率です。stdin からは `.rate_limits.seven_day.used_percentage`、API からは `.seven_day.utilization` で取得します。

`⟳ may 4, 2:00am` はリセット日時です。current との違いは `format_epoch_time` に渡すスタイルが `"datetime"` になる点で、日付＋時刻（`may 4, 2:00am`）形式で表示されます。current が時刻のみなのは、5時間以内にリセットされるので日付が不要なためです。

```bash
seven_day_pct=$(echo "$input" | jq -r '.rate_limits.seven_day.used_percentage // empty' | awk '{printf "%.0f", $1}')
seven_day_reset_epoch=$(echo "$input" | jq -r '.rate_limits.seven_day.resets_at // empty')

seven_day_reset=$(format_epoch_time "$seven_day_reset_epoch" "datetime")  # → "may 4, 2:00am"
```

#### extra（従量課金）

```
extra   ○○○○○○○○○○ $0.00/$20.00 ⟳ jun 1
```

Extra Usage は Claude Code Max の使用量が上限に達したあとに従量課金で使い続けられる機能です。`extra_usage.is_enabled` が `true` のときだけ表示されます。

金額表示の `$0.00/$20.00` は API レスポンスの `used_credits` と `monthly_limit` をそれぞれ100で割ってドルに換算しています。**API はセント単位で返すため、100で割らないと金額が100倍になります。**

`⟳ jun 1` のリセット日は API に含まれません。毎月1日リセットなので、`date` コマンドで翌月1日を計算しています。macOS と Linux で書式が異なるため両方を試みます。

```bash
extra_enabled=$(echo "$usage_data" | jq -r '.extra_usage.is_enabled // false')

if [ "$extra_enabled" = "true" ]; then
    extra_pct=$(echo "$usage_data" | jq -r '.extra_usage.utilization // 0' | awk '{printf "%.0f", $1}')
    extra_used=$(echo "$usage_data" | jq -r '.extra_usage.used_credits // 0' | awk '{printf "%.2f", $1/100}')  # セント→ドル
    extra_limit=$(echo "$usage_data" | jq -r '.extra_usage.monthly_limit // 0' | awk '{printf "%.2f", $1/100}')

    # 翌月1日を計算（macOS: date -v、Linux: date -d）
    extra_reset=$(date -v+1m -v1d +"%b %-d" 2>/dev/null | tr '[:upper:]' '[:lower:]')
    [ -z "$extra_reset" ] && \
        extra_reset=$(date -d "$(date +%Y-%m-01) +1 month" +"%b %-d" 2>/dev/null | tr '[:upper:]' '[:lower:]')
fi
```

### 出力の組み立て

ANSI エスケープコードで文字に色を付け、`printf "%b"` で出力します。

```bash
# 1行目を文字列として組み立てる
line1="${blue}${model_name}${reset}${sep}✍️ ${pct_color}${pct_used}%${reset}..."

# 最後にまとめて出力
printf "%b" "$line1"

# \n\n で区切ると Claude Code が2行目として認識する
[ -n "$rate_lines" ] && printf "\n\n%b" "$rate_lines"
```

`build_bar` 関数は使用率から `●○` のプログレスバーを組み立てます。幅（`bar_width=10`）を変えると棒グラフの長さが変わります。

## セットアップ手順

```bash
# 1. 依存ツールをインストール
brew install jq curl git

# 2. スクリプトを保存
#    上記「statusline.sh 完全版」の内容を ~/.claude/statusline.sh に保存する

# 3. 実行権限を付与
chmod +x ~/.claude/statusline.sh

# 4. ~/.claude/settings.json に statusLine を追記して保存

# 5. Claude Code を再起動して反映を確認
```

## まとめ

Claude Code のステータスバーは「シェルスクリプトの stdout がそのまま表示される」シンプルな仕組みです。JSON のキー構造を把握すれば、`jq` で好きなフィールドを取り出してフォーマットするだけです。色・区切り文字・並び順・追加項目はすべてスクリプト側で自由に変えられます。

## 参考

- [kamranahmedse/claude-statusline（GitHub）](https://github.com/kamranahmedse/claude-statusline/tree/main/bin)
- [Claude Code の使い手の快適さを上げる（YouTube）](https://www.youtube.com/watch?v=0fYBrfhsmiI)
