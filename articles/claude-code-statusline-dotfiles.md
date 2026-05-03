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

Claude Code のステータスバーは、シェルスクリプトの標準出力をそのまま表示する仕組みです。`settings.json` に1行設定を追加するだけで有効になり、表示内容は完全に自由に組み立てられます。

この記事では、[kamranahmedse/claude-statusline](https://github.com/kamranahmedse/claude-statusline/tree/main/bin) を参考に実装したスクリプトをもとに、各要素を**どのコードで取得・表示するか**を解説します。

完成したコードは [GitHub](https://github.com/jam006097/dotfiles/tree/main/claude) で公開しています。

## 完成イメージ

Claude Code のステータスバーがこのように変わります。

```
claude-sonnet-4-6 │ ✍️ 12% │ zenn-content (main) │ ⏱ 5m │ ◑ default

current ●●○○○○○○○○  12% ⟳ 3:45pm
weekly  ●○○○○○○○○○   8% ⟳ may 3
```

上段に作業状況、下段にレートリミットのバーが表示されます。

## settings.json の設定

`~/.claude/settings.json` に以下を追加します。

```json
{
  "statusLine": {
    "type": "command",
    "command": "bash \"$HOME/.claude/statusline.sh\""
  }
}
```

Claude Code は状態が更新されるたびにこのコマンドを実行し、スクリプトの標準出力をそのままステータスバーに表示します。

## スクリプトの基本構造

Claude Code はスクリプトの**標準入力**に JSON を渡します。まずそれを受け取ります。

```bash
#!/bin/bash
set -f  # JSON 内の * などがグロブ展開されないように無効化

input=$(cat)

# 入力がなければ最低限の表示をして終了
if [ -z "$input" ]; then
    printf "Claude"
    exit 0
fi
```

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
    "five_hour": { "used_percentage": 12.5, "resets_at": "2026-05-03T15:00:00Z" },
    "seven_day":  { "used_percentage": 8.0,  "resets_at": "2026-05-10T00:00:00Z" }
  }
}
```

以降はこの JSON を `jq` で読み取って各要素を取り出します。

## 各要素の実装コード

### モデル名

```bash
model_name=$(echo "$input" | jq -r '.model.display_name // "Claude"')
```

`.model.display_name` がなければ `"Claude"` にフォールバックします。

### コンテキスト使用率

通常のトークンに加え、キャッシュ生成・キャッシュ読み込みトークンも合算します。3種類を足してウィンドウサイズで割ることで正確な使用率が出ます。

```bash
size=$(echo "$input" | jq -r '.context_window.context_window_size // 200000')
[ "$size" -eq 0 ] 2>/dev/null && size=200000

input_tokens=$(echo "$input" | jq -r '.context_window.current_usage.input_tokens // 0')
cache_create=$(echo "$input" | jq -r '.context_window.current_usage.cache_creation_input_tokens // 0')
cache_read=$(echo "$input" | jq -r '.context_window.current_usage.cache_read_input_tokens // 0')

current=$(( input_tokens + cache_create + cache_read ))
pct_used=$(( current * 100 / size ))
```

### 作業ディレクトリと Git ブランチ

`.cwd` が渡されるので `basename` でディレクトリ名を取り出し、`git` コマンドでブランチを取得します。

```bash
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
```

:::message
`--no-optional-locks` を付けないと、Claude Code がファイルを並列操作しているときにロック競合が起きます。
:::

### セッション経過時間

`.session.start_time` は ISO 8601 形式なので epoch に変換して経過秒数を計算します。macOS と Linux で `date` コマンドの書式が異なるため、両対応の処理が必要です。

```bash
session_start=$(echo "$input" | jq -r '.session.start_time // empty')
if [ -n "$session_start" ] && [ "$session_start" != "null" ]; then
    # Linux: date -d、macOS: date -j -f でそれぞれ変換を試みる
    stripped="${session_start%%.*}"
    stripped="${stripped%%Z}"
    start_epoch=$(env TZ=UTC date -d "${stripped/T/ }" +%s 2>/dev/null || \
                  env TZ=UTC date -j -f "%Y-%m-%dT%H:%M:%S" "$stripped" +%s 2>/dev/null)

    if [ -n "$start_epoch" ]; then
        elapsed=$(( $(date +%s) - start_epoch ))
        if [ "$elapsed" -ge 3600 ]; then
            session_duration="$(( elapsed / 3600 ))h$(( (elapsed % 3600) / 60 ))m"
        elif [ "$elapsed" -ge 60 ]; then
            session_duration="$(( elapsed / 60 ))m"
        else
            session_duration="${elapsed}s"
        fi
    fi
fi
```

### Effort レベル

Effort レベルは JSON では渡されません。`settings.json` を直接 `jq` で読みます。

```bash
effort="default"
settings_path="$HOME/.claude/settings.json"
if [ -f "$settings_path" ]; then
    effort=$(jq -r '.effortLevel // "default"' "$settings_path" 2>/dev/null)
fi
```

### レートリミット

stdin の JSON に含まれていれば直接取得します。含まれていない場合は Anthropic API を呼び出します。

```bash
five_hour_pct=$(echo "$input" | jq -r '.rate_limits.five_hour.used_percentage // empty')

if [ -z "$five_hour_pct" ]; then
    # macOS キーチェーンからトークンを取得
    blob=$(security find-generic-password -s "Claude Code-credentials" -w 2>/dev/null)
    token=$(echo "$blob" | jq -r '.claudeAiOauth.accessToken // empty' 2>/dev/null)

    # ~/.claude/.credentials.json からの取得（Linux 等）
    if [ -z "$token" ] || [ "$token" = "null" ]; then
        token=$(jq -r '.claudeAiOauth.accessToken // empty' \
                "$HOME/.claude/.credentials.json" 2>/dev/null)
    fi

    response=$(curl -s --max-time 5 \
        -H "Authorization: Bearer $token" \
        -H "anthropic-beta: oauth-2025-04-20" \
        -H "User-Agent: claude-code/2.1.34" \
        "https://api.anthropic.com/api/oauth/usage")

    five_hour_pct=$(echo "$response" | jq -r '.five_hour.utilization // 0' \
                    | awk '{printf "%.0f", $1}')
    seven_day_pct=$(echo "$response" | jq -r '.seven_day.utilization // 0' \
                    | awk '{printf "%.0f", $1}')
else
    five_hour_pct=$(printf "%.0f" "$five_hour_pct")
    seven_day_pct=$(echo "$input" | jq -r '.rate_limits.seven_day.used_percentage // 0' \
                    | awk '{printf "%.0f", $1}')
fi
```

:::message
API の呼び出し結果は `/tmp/claude/statusline-usage-cache.json` に 60 秒間キャッシュします。ステータスバーは頻繁に更新されるため、毎回 API を呼ぶと表示が遅くなるためです。
:::

## 出力の組み立て

ANSI エスケープコードで色を定義し、`printf "%b"` で出力します。

```bash
blue='\033[38;2;0;153;255m'
green='\033[38;2;0;175;80m'
cyan='\033[38;2;86;182;194m'
dim='\033[2m'
reset='\033[0m'
sep=" ${dim}│${reset} "

# 1行目を組み立てる
line1="${blue}${model_name}${reset}"
line1+="${sep}✍️ ${pct_used}%"
line1+="${sep}${cyan}${dirname}${reset}"
[ -n "$git_branch" ] && line1+=" ${green}(${git_branch}${git_dirty})${reset}"
[ -n "$session_duration" ] && line1+="${sep}⏱ ${session_duration}"
line1+="${sep}${dim}◑ ${effort}${reset}"

printf "%b" "$line1"

# 2行目以降は \n\n で区切ると Claude Code が複数行として認識する
if [ -n "$five_hour_pct" ]; then
    printf "\n\ncurrent %s%%" "$five_hour_pct"
fi
if [ -n "$seven_day_pct" ]; then
    printf "\nweekly  %s%%" "$seven_day_pct"
fi
```

## セットアップ手順

```bash
# 1. 依存ツールをインストール（macOS）
brew install jq curl git

# 2. スクリプトを配置
mkdir -p ~/.claude
curl -o ~/.claude/statusline.sh \
  https://raw.githubusercontent.com/jam006097/dotfiles/main/claude/statusline.sh
chmod +x ~/.claude/statusline.sh

# 3. settings.json を編集して statusLine を追加
# （前述の JSON を ~/.claude/settings.json に追記）

# 4. Claude Code を再起動して反映を確認
```

## まとめ

Claude Code のステータスバーは「シェルスクリプトの stdout がそのまま表示される」シンプルな仕組みです。今回紹介したキー以外にも、渡される JSON には拡張余地があります。色・区切り文字・並び順はすべてスクリプト側で自由に変えられるので、必要な情報だけを絞り込んで自分専用のステータスバーを作ってみてください。

## 参考

- [kamranahmedse/claude-statusline（GitHub）](https://github.com/kamranahmedse/claude-statusline/tree/main/bin)
- [Claude Code の使い手の快適さを上げる（YouTube）](https://www.youtube.com/watch?v=0fYBrfhsmiI)
