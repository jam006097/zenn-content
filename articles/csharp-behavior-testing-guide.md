---
title: "テストコードは「振る舞い」に対してテストするといいよという話"
emoji: "🛠️"
type: "tech"
topics: ["csharp", "testing", "xunit", "architecture"]
published: true
---

## はじめに

「コードを綺麗に書き直しただけなのに、テストが大量に落ちてしまった...」
「テストを直すのが面倒で、リファクタリングを躊躇してしまう...」

そんな経験はありませんか？
僕はめちゃくちゃあります。

テストコードを書いているはずなのに、逆に開発のスピードが落ちているなら、それは**「実装の詳細」をテストしてしまっている**からかも。

この記事では、壊れにくく、保守しやすいテストを書くための鍵である**「振る舞いのテスト」**について、C#のコード例とともに見ていきましょう。

## 「実装の詳細」vs「振る舞い」

テストコードは大きく2つに分けられます。

1.  **実装の詳細のテスト**: 「メソッドAの中でメソッドBが呼ばれているか」「この変数がどう書き換えられているか」など、**内部のやり方**をチェックする。
2.  **振る舞いのテスト**: 「この入力を与えたら、期待通りの結果が返るか」という、**外部から見た結果**をチェックする。

### なぜ実装の詳細をテストしてはいけないのか？

実装の詳細をテストすると、コードの内部構造（アルゴリズムの改善や変数名の変更など）を変えるたびにテストコードも修正が必要になります。これを**「脆いテスト（Brittle Tests）」**と呼びます。

TDDの過程で設計が少し変わって、その都度テストコードを書き直していたら黄色信号です。

逆に、振る舞いをテストしていれば、内部の実装をどう書き換えても、最終的なアウトプットが変わらない限りテストは通り続けます。これこそが、安心してリファクタリングできる状態です。

要は、「振る舞い」のテストのほうが開発者にとって幸福度が高くなるんですよね。

## publicメソッドは「振る舞い」の入り口

振る舞いをテストする最もシンプルで確実な方法は、**publicメソッド（公開されたインターフェース）のみをテストすること**です。
C#では、基本publicメソッドしかできません。

手間をかければprivateメソッドもできはしますが、そんなことしなくていいです。
苦労して自分の首を自分で絞めているようなもんです。

### 悪い例：privateメソッドを無理やりテストする

例えば、注文の合計金額を計算するクラスを考えてみましょう。

```csharp
public class OrderCalculator
{
    public decimal CalculateTotal(decimal price, int quantity)
    {
        decimal subtotal = price * quantity;
        return ApplyTax(subtotal); // 内部で消費税計算を呼んでいる
    }

    // 内部ロジックなのでprivate
    private decimal ApplyTax(decimal amount)
    {
        return amount * 1.1m;
    }
}
```

「消費税計算が大事だから」といって、リフレクションなどを使って `ApplyTax` を直接テストしようとするのはNGです。なぜなら、将来「税率計算を外部サービスに任せる」ように内部実装を変えた瞬間、そのテストはゴミになってしまうからです。

### 良い例：publicメソッドを通じて結果を検証する

```csharp
[Fact]
public void CalculateTotal_ReturnsAmountIncludingTax()
{
    var calculator = new OrderCalculator();
    
    // publicメソッドから「合計金額が正しく計算されるか」という結果だけを見る
    var result = calculator.CalculateTotal(1000, 2);
    
    Assert.Equal(2200m, result);
}
```

利用者が関心があるのは「合計金額がいくらか」であって、内部でどう計算されているかではありません。publicメソッドをテストすることで、自然と「振る舞い」にフォーカスできます。

また、このほうが何したいかを将来の自分やほかの開発者が意図を理解しやすくなります。

## 「privateをテストしたい」は設計見直しのサイン

「でも、このprivateメソッドのロジックが複雑すぎて、public経由だとテストしにくいんだ！」

そう感じたときが、**設計を見直す絶好のタイミング**です。
privateメソッドが複雑すぎるということは、そのクラスが**「複数の責任を持ちすぎている」**可能性があります。

もし、詳細設計が別の担当者なら相談してみましょう。
コミュニケーションコストはかかりますが、その価値はあります。

### インターフェースへ分離するメリット

複雑なロジックを別のクラスに切り出し、それを「インターフェース」越しに使うようにすると、以下のような劇的なメリットがあります。

1.  **役割が明確になる**: 「注文管理」クラスは「税金計算」の細かいルールを知らなくて良くなります。
2.  **部品としてテストできる**: 切り出した「税金計算」クラス単体で、あらゆるパターンのテストを徹底的に行えます。
3.  **テストで「偽物」に差し替えられる**: インターフェースに依存させることで、テスト時に本物の代わりに「テスト用の偽物（スタブやモック）」を渡せるようになります。

### 実践：インターフェースによる分離とDI（依存性の注入）

もし消費税計算が「国ごとに違う」「外部APIに問い合わせる」といった複雑なものなら、まず「何をするか」という**契約（インターフェース）**を作ります。

```csharp
// 1. 「税金を適用する」という機能だけを定義（契約）
public interface ITaxCalculator
{
    decimal Apply(decimal amount);
}

// 2. 本物のロジックを持つクラス
public class JapanTaxCalculator : ITaxCalculator
{
    public decimal Apply(decimal amount) => amount * 1.1m;
}

// 3. 注文クラスは「インターフェース」を受け取るようにする（依存性の注入）
public class OrderCalculator
{
    private readonly ITaxCalculator _taxCalculator;

    // コンストラクタで「道具（インターフェース）」を受け取る
    public OrderCalculator(ITaxCalculator taxCalculator)
    {
        _taxCalculator = taxCalculator;
    }

    public decimal CalculateTotal(decimal price, int quantity)
    {
        decimal subtotal = price * quantity;
        // 内部でどう計算されるかは知らず、ただ「依頼する」だけ
        return _taxCalculator.Apply(subtotal);
    }
}
```

### 分離した後のテストはどう変わる？

インターフェースに分離したことで、`OrderCalculator`のテストは**「複雑な税金計算に振り回されること」**がなくなります。テストでは「偽物の税金計算機（スタブ）」を渡すことができるからです。

```csharp
[Fact]
public void CalculateTotal_CallsTaxCalculatorWithCorrectAmount()
{
    // 準備：常に100円を足すだけの「偽物の税金計算機」を用意する
    // これにより、本物のAPI通信や複雑な計算を気にせずテストできる
    var stubTax = new StubTaxCalculator(); 
    var calculator = new OrderCalculator(stubTax);

    // 実行
    var result = calculator.CalculateTotal(1000, 1);

    // 検証
    // OrderCalculatorが「正しく外部に依頼を出して、結果を返したか」を検証
    Assert.Equal(1100m, result);
}

// テスト用のシンプルな実装（スタブ）
public class StubTaxCalculator : ITaxCalculator
{
    public decimal Apply(decimal amount) => amount + 100m;
}
```

このように分離すると、**「税金計算のロジック自体のテスト」**と**「注文管理が正しく税金計算機を使いこなしているかのテスト」**を切り離して考えることができます。
もし将来、税金計算の仕様がどれだけ複雑になっても、`OrderCalculator`のテストを書き直す必要はありません。

## まとめ：良いテストが、良い設計を作る

テストコードを書く目的は、バグを見つけることだけではありません。**「保守しやすい、綺麗なコードを維持し続けること」**にあります。

- **振る舞いをテストする**: 実装を隠蔽し、リファクタリング耐性を高める。
- **publicを入り口にする**: クラスの利用者の視点で仕様を定義する。
- **インターフェースを活用する**: 複雑な依存関係を切り離し、テストの自由度を上げる。

この原則を守ることで、テストコードはあなたの足を引っ張る「お荷物」から、開発を力強く支える「お守り」へと変わるはずです。

今日から、privateメソッドのテストをやめて、そのクラスが提供する「価値（振る舞い）」に目を向けてみませんか？
