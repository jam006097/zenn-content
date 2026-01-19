---
title: "[ちょっと幸せになる]テストコードの第一歩_C#編"
emoji: "😊"
type: "tech"
topics: ["csharp", "xunit", "testing", "beginner"]
published: true
---

## はじめに

「1ヶ月前に自分が書いたコードなのに、どうしてこう書いたのか思い出せない...」
「この部分を修正したいけど、どこまで影響があるのかわからなくて怖い...」

開発者なら誰でも一度は経験する、そんな悩みを抱えていませんか？🤔

この記事では、そんなあなたを助けてくれる「テストコード」の第一歩を、C#とxUnitを使ってご紹介します。

「テストコードって品質管理のためでしょ？なんだか難しそう...」と感じるかもしれません。もちろん、品質はとても大切です。でも、テストコードがもたらす一番の恩恵は、**未来のあなた自身を助けてくれる**ことにあるのです。この記事を読み終える頃には、きっとその意味がわかるはずです。

## テストコードは「未来の自分を助けるための贈り物」です。

なぜテストコードが未来の自分を助けてくれるのでしょうか？

- **動く仕様書になる**: テストコードは「このコードは、こういう入力に対して、こう動くべきだ」という仕様を、実行可能な形で記録したものです。数ヶ月後にコードを見返したとき、テストコードを読めば、そのコードが何をするためのものだったのかをすぐに思い出せます。
- **安心して修正できるお守りになる**: 機能追加やリファクタリングでコードを修正したとき、テストを実行すれば、既存の機能が壊れていない（デグレしていない）ことを一瞬で確認できます。この安心感は、コードを積極的に改善していく上で、強力な武器になります。

つまりテストコードは、**1ヶ月後の自分が仕様を忘れても大丈夫なように残しておく「動く仕様書」であり、「未来の自分への贈り物」** なのです。

## 事前準備: VS Code で C# 環境を整えよう

テストコードを書いていく前に、C#の開発環境をVS Codeで準備しましょう。

### 1. .NET SDK と VS Code の準備
まだの方は、以下の2つをインストールしておきましょう。
- **[.NET SDK](https://dotnet.microsoft.com/ja-jp/download)**: C#のプログラムを開発・実行するための基本ツールです。
- **[Visual Studio Code](https://code.visualstudio.com/download)**: 高機能なテキストエディタです。

### 2. VS Code 拡張機能のインストール
VS Codeで快適にC#を扱うために、Microsoft公式の拡張機能をインストールします。

1. VS Codeを開き、左側の拡張機能アイコンをクリックします。
2. 検索バーに「**C# Dev Kit**」と入力し、インストールします。これ一つで、C#のコーディング、デバッグ、テストに必要な機能がまとめて手に入ります。
![](/images/vscode/vscode-install-csharpDevKit.png)

### 3. プロジェクトの作成
次に、VS Codeに内蔵されているターミナルを使って、今回のプロジェクト構成を準備します。

1. VS Codeで「ターミナル」メニューから「新しいターミナル」を選択します。
2. 以下のコマンドを一つずつ実行し、プロジェクトの雛形を作成します。

```bash
# 1. 作業用のフォルダを作成して、そこに移動します
mkdir ZennTestProject
cd ZennTestProject

# 2. プロジェクトを管理するソリューションファイル(.sln)を作成します
dotnet new sln -n MySolution

# 3. アプリケーション本体となるプロジェクトを作成します
dotnet new console -o MyFirstTest

# 4. テストコードを置くためのテストプロジェクトを作成します
dotnet new xunit -o MyFirstTest.Tests

# 5. ソリューションに各プロジェクトを追加します
dotnet sln add MyFirstTest
dotnet sln add MyFirstTest.Tests

# 6. テストプロジェクトからアプリケーション本体を参照できるように設定します
dotnet add MyFirstTest.Tests reference MyFirstTest
```

これで準備は完了です！ `ZennTestProject` フォルダをVS Codeで開いておきましょう。
![](/images/vscode/vscode-setupproject-csharp.png)

## C# と xUnit で最初のテストを書いてみよう

それでは、実際に簡単なテストコードを書いてみましょう。ここでは、C#の公式テストフレームワークの一つである「xUnit」を使います。

### 4. テスト対象のコードを記述する

次に、`MyFirstTest` プロジェクトに、テストしたいロジックを記述します。

1. `MyFirstTest` フォルダ（プロジェクト）の中に、`BusinessDayCalculator.cs` という名前で新しいファイルを作成してください。
2. 作成した `BusinessDayCalculator.cs` ファイルに、以下の稼働日数を計算するクラスのコードを記述します。

```csharp
// BusinessDayCalculator.cs
using System;

namespace MyFirstTest
{
    public class BusinessDayCalculator
    {
        public int CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                return 0;
            }

            int businessDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    businessDays++;
                }
            }
            return businessDays;
        }
    }
}
```
以下はvscodeに実際に記入した画像です。
読みやすいようにコメント追加しています。
![](/images/csharp/csharp-test-calcday-code.png)

> [!NOTE]
> `MyFirstTest` プロジェクトにもとからある `Program.cs` ファイルは、アプリケーションの開始点です。今回はテストの書き方を学ぶのが目的なので、このファイルは編集せず、初期状態（`Console.WriteLine("Hello, World!");`などが書かれた状態）のままで大丈夫です。

### 5. テストコードを書く

次に、`BusinessDayCalculator` クラスをテストするためのコードを書きます。`MyFirstTest.Tests` プロジェクト内にある `UnitTest1.cs` ファイルを開き、以下の内容に書き換えます。xUnitでは、`[Fact]` という属性を付けたメソッドがテストとして実行されます。

```csharp
// BusinessDayCalculatorTests.cs
using System;
using Xunit;

namespace MyFirstTest.Tests
{
    public class BusinessDayCalculatorTests
    {
        [Fact]
        public void CalculateBusinessDays_SameWeek_ReturnsCorrectDays()
        {
            // Arrange: 準備
            var calculator = new BusinessDayCalculator();
            var startDate = new DateTime(2026, 1, 19); // 月曜日
            var endDate = new DateTime(2026, 1, 23);   // 金曜日
            int expected = 5;

            // Act: 実行
            int actual = calculator.CalculateBusinessDays(startDate, endDate);

            // Assert: 検証
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CalculateBusinessDays_IncludingWeekend_ReturnsCorrectDays()
        {
            // Arrange
            var calculator = new BusinessDayCalculator();
            var startDate = new DateTime(2026, 1, 19); // 月曜日
            var endDate = new DateTime(2026, 1, 25);   // 日曜日
            int expected = 5;

            // Act
            int actual = calculator.CalculateBusinessDays(startDate, endDate);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
```

以下はvscodeに実際に記入した画像です。
読みやすいようにコメント追加しています。
![](/images/csharp/csharp-test-calcday-testcode.png)

テストコードは、一般的に**AAAパターン**（Arrange-Act-Assert）と呼ばれる3つのステップで構成されます。

1.  **Arrange (準備)**: テストに必要な `BusinessDayCalculator` のインスタンスや、`DateTime` 型の期間を準備します。
2.  **Act (実行)**: テスト対象のメソッド `CalculateBusinessDays` を実行し、結果を取得します。
3.  **Assert (検証)**: 実行結果が期待通り（`expected`）であるかを `Assert.Equal` で確認します。

この一つ目のテストは、「`CalculateBusinessDays` メソッドは、月曜から金曜の期間を渡されると、5日と計算するはずだ」という仕様をコードで表現しているのです。

そして二つ目のテストは、「週末を含む月曜から日曜の期間を渡されても、土日を除いた5日間と正しく計算する」という仕様を検証しています。このように、正常なケース（平日のみ）と、少し意地悪なケース（週末を含む）の両方をテストすることで、コードの信頼性がより高まるのです。

### 6. テストを実行する

作成したテストを実行してみましょう。VS Codeを使えば、GUIで直感的に実行する方法と、従来通りターミナルで実行する方法があります。初心者の方にはGUIがおすすめです。

#### 方法1: VS CodeのGUIを使う（おすすめ）

C# Dev Kit拡張機能には「テスト エクスプローラー」という便利な機能が含まれています。

1.  VS Codeの左側のアクティビティバーにある、**フラスコ（試験管）のアイコン**をクリックします。
2.  テストエクスプローラーが開き、プロジェクト内のテストが自動で検出されて一覧表示されます。
3.  一番上にある「▶︎」ボタン（すべて実行）を押すか、各テストの横にある「▶︎」ボタンを押してみてください。

![](/images/csharp/csharp-test-calcday-executetest.png)

テストが実行され、成功すると緑色のチェックマークが付きます。失敗した場合は赤色のバツ印が表示され、クリックするとエラーの詳細を確認できます。このように、GUIを使うと結果が視覚的に分かりやすく、とても簡単です。

#### 方法2: ターミナルを使う

もちろん、ターミナルからコマンドで実行することもできます。

ソリューションファイル（`.sln`）があるプロジェクトのルートディレクトリ（`ZennTestProject`）で、以下のコマンドを実行してみましょう。

```bash
dotnet test
```

テストが成功すれば、以下のようなメッセージが表示されます。

![](/images/csharp/csharp-test-calcday-executetest-cli.png)

「成功数: 2」と表示されていれば成功です！🎉

---
これで、あなたの `CalculateBusinessDays` メソッドが、週末を含む場合と含まない場合の両方で、期待通りに動作することが証明されました。

## まとめ

お疲れ様でした！これがテストコードの第一歩です。

VS Codeのテストエクスプローラーを使えば、ボタン一つで簡単にテストを実行できることがお分かりいただけたと思います。これなら、コードを少し変更するたびに気軽にテストを実行できますね。

今回は少し複雑な「稼働日数を求める」ロジックを例にしましたが、テストがあったおかげで「週末を含む場合」など、様々なケースを安心して試す土台ができました。

もし将来、「祝日も休日に含める」という仕様変更が来たとします。その時も、まず祝日用のテストケースを追加し、それに合わせてロジックを修正していくことで、既存の土日判定を壊さずに、安全に変更を進めることができます。これがテストの力です。

最初は難しく感じるかもしれませんが、完璧なテストを目指す必要はありません。まずは簡単なメソッド一つからでも大丈夫です。

今日あなたが書いた1行のテストコードが、1ヶ月後、あるいは1年後のあなたをきっと助けてくれます。「あの時の自分、ありがとう！」と思えるように、未来の自分がちょっと幸せになるためのタイムカプセルを、今日のコードに埋め込んでみませんか？
