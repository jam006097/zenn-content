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

Claude Code を使っていると、こんな疑問が浮かびませんか？

- 「今どのくらいコンテキストを使っている？」
- 「レートリミットはあとどれくらい残っている？」
- 「今どのブランチで作業していたっけ？」

Claude Code には**ステータスバーをカスタマイズする機能**があります。シェルスクリプトを1つ用意するだけで、使用状況・レートリミット・Git ブランチなどをリアルタイムで表示できます。

この記事では、[kamranahmedse/claude-statusline](https://github.com/kamranahmedse/claude-statusline/tree/main/bin) と [こちらの YouTube 動画](https://www.youtube.com/watch?v=0fYBrfhsmiI) を参考に、自分の dotfiles に組み込む方法を紹介します。

コードの全文は [こちらの GitHub リポジトリ](https://github.com/jam006097/dotfiles/tree/main/claude) で公開しています。

## 完成イメージ

Claude Code のステータスバーがこのように変わります。

```
claude-sonnet-4-6 │ ✍️ 12% │ zenn-content (main) │ ⏱ 5m │ ◑ default

current ●●○○○○○○○○  12% ⟳ 3:45pm
weekly  ●○○○○○○○○○   8% ⟳ may 3
```

ひと目でわかる情報が上段に並び、下段にはレートリミットのバーが表示されます。

## 何が表示されるのか、そしてなぜそれが必要なのか

ステータスバーに表示する情報はそれぞれ「作業を止めずに判断できること」を意図して選んでいます。

### モデル名

```
claude-sonnet-4-6
```

複数のモデルを切り替えながら使っていると、「今どのモデルで動いているか」を忘れがちです。特に Opus と Sonnet ではレートリミットの消費速度が大きく異なるため、常に目に入る場所に置いておくことで誤操作を防げます。

### コンテキスト使用率

```
✍️ 12%
```

Claude Code はセッション中のやりとりをすべてコンテキストとして保持します。これが上限（200,000 トークン前後）に近づくと、応答の精度が落ちたり、古い情報が押し出されて意図しない挙動が起きたりします。

使用率が高くなってきたら「セッションを新しく始める」「`/compact` でコンテキストを圧縮する」といった判断ができます。使用率に応じてバーの色が変わるのもそのためです。50% を超えるとオレンジ、70% で黄、90% で赤になります。

### 作業ディレクトリと Git ブランチ

```
zenn-content (main*)
```

Claude Code はファイルを読み書きするため、「どのブランチにいるか」を常に把握しておく必要があります。`*` はコミットされていない変更があることを示します。作業ブランチと思っていたら `main` だった、というミスをこれで防げます。

### セッション経過時間

```
⏱ 5m
```

セッションが長くなるほどコンテキストは積み上がり、Claude の「記憶」は重くなります。経過時間を見ることで「そろそろ新しいセッションに切り替えどきかな」という判断の目安になります。

### Effort レベル

```
◑ default
```

Claude Code には `low` / `default` / `high` の Effort レベルがあり、思考の深さとトークン消費量が変わります。設定を変えたことを忘れたまま使い続けるのを防ぐために、常に表示しています。

### レートリミット（5時間・週次）

```
current ●●○○○○○○○○  12% ⟳ 3:45pm
weekly  ●○○○○○○○○○   8% ⟳ may 3
```

Claude Code（Max プラン）には 5 時間ウィンドウと週次ウィンドウの 2 種類のレートリミットがあります。それぞれ「いつリセットされるか」も合わせて表示することで、「あと何時間待てば使えるか」を作業しながら確認できます。

:::message
レートリミットの情報は Claude Code から直接渡されます。渡されない場合は API を叩いて取得し、60 秒間キャッシュしています。毎回 API を呼ぶとステータスバーの更新が遅くなるためです。
:::

## 仕組みの概要

Claude Code のステータスバーは `settings.json` に `statusLine.command` を設定するだけで有効になります。

```json
{
  "statusLine": {
    "type": "command",
    "command": "bash \"$HOME/.claude/statusline.sh\""
  }
}
```

Claude Code が状態を更新するたびにこのコマンドが呼び出され、スクリプトの出力がそのままステータスバーに表示されます。スクリプトには現在のモデル・コンテキスト使用量・セッション情報などが JSON 形式で渡されるので、それを読み取って整形して出力するだけです。

## dotfiles で管理するメリット

今回は `~/.claude/` に直接ファイルを置くのではなく、dotfiles リポジトリで管理してシンボリックリンクで繋ぐ構成にしています。

```
dotfiles/claude/
├── settings.json    # Claude Code の設定
├── statusline.sh    # ステータスバーを生成するスクリプト
└── setup.sh         # シンボリックリンクを張るセットアップスクリプト
```

この構成にする理由は主に 2 つです。

**1. どの環境でも同じ設定を再現できる**
新しい Mac やサーバーでも `git clone` してセットアップスクリプトを実行するだけで環境が整います。

**2. 設定の変更履歴が残る**
dotfiles を Git で管理していれば、「いつ・何を・なぜ変えたか」をコミットメッセージで追えます。うまく動かなくなったときに前の状態に戻すことも簡単です。

## セットアップ手順

```bash
# 1. 依存ツールをインストール（macOS）
brew install jq curl git

# 2. dotfiles リポジトリをクローン
git clone https://github.com/jam006097/dotfiles.git ~/dotfiles

# 3. 実行権限を付与
chmod +x ~/dotfiles/claude/setup.sh
chmod +x ~/dotfiles/claude/statusline.sh

# 4. セットアップを実行（シンボリックリンクが作成される）
bash ~/dotfiles/claude/setup.sh

# 5. Claude Code を再起動して反映を確認
```

セットアップスクリプトは実行前に `jq`・`curl`・`git` の存在を確認します。また `~/.claude/settings.json` がすでに存在する場合は `.bak` として退避してからリンクを作成するので、既存の設定を上書きしてしまう心配もありません。

## まとめ

Claude Code のステータスバーは、ターミナルを離れることなく作業状況を把握するための仕組みです。

- **コンテキスト使用率** → 詰め込みすぎを防ぎ、新しいセッションへの切り替えタイミングがわかる
- **レートリミット** → いつ・どのくらい使えるかを見ながら作業量を調整できる
- **Git ブランチ** → 作業ブランチの確認をうっかり忘れるミスがなくなる
- **セッション時間** → 長すぎるセッションのリセットタイミングの目安になる

コードの全文は [GitHub](https://github.com/jam006097/dotfiles/tree/main/claude) で公開しています。表示項目や色は自由に変更できるので、自分の作業スタイルに合わせてカスタマイズしてみてください。

## 参考

- [kamranahmedse/claude-statusline（GitHub）](https://github.com/kamranahmedse/claude-statusline/tree/main/bin)
- [Claude Code の使い手の快適さを上げる（YouTube）](https://www.youtube.com/watch?v=0fYBrfhsmiI)
