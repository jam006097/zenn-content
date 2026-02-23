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

複雑なロジックを別のクラスに切り出し、それを「インターフェース」越しに使うようにすると、以下のようなメリットがあります。

1.  **役割が明確になる**: 「注文管理」クラスは「税金計算」の細かいルールを知らなくて良くなります。
2.  **部品としてテストできる**: 切り出した「税金計算」クラス単体で、あらゆるパターンのテストを徹底的に行えます。
3.  **テストで「偽物」に差し替えられる**: インターフェースに依存させることで、テスト時に本物の代わりに「テスト用の偽物（スタブやモック）」を渡せるようになります。

※ロンドン学派と古典学派の宗教論があるかと思いますが、私自身はバランス派です。
必要な時にはモックやスタブもOKだと思っています。

### 実践：インターフェースによる分離とDI（依存性の注入）

もし消費税計算が「国ごとに違う」「外部APIに問い合わせる」といった、**「テストの時に動かすと面倒（遅い、お金がかかる、ネット環境が必要）」**なものなら、まず「何をするか」という**契約（インターフェース）**を作ります。

```csharp
// 1. 「税金を適用する」という機能だけを定義（契約）
public interface ITaxCalculator
{
    decimal Apply(decimal amount);
}

// 2. 本物のロジック。
// 例えば、実際には「外部の複雑かる遅い税計算API」を叩くような重い処理だと想像してください
public class JapanTaxCalculator : ITaxCalculator
{
    public decimal Apply(decimal amount) => amount * 1.1m; 
}

// 3. 注文クラスは「インターフェース」を受け取るようにする（依存性の注入）
public class OrderCalculator
{
    private readonly ITaxCalculator _taxCalculator;

    // コンストラクタで「道具（インターフェース）」を受け取る
    // これにより、OrderCalculatorは「本物」が誰かを知らなくて良くなります
    public OrderCalculator(ITaxCalculator taxCalculator)
    {
        _taxCalculator = taxCalculator;
    }

    public decimal CalculateTotal(decimal price, int quantity)
    {
        decimal subtotal = price * quantity;
        return _taxCalculator.Apply(subtotal);
    }
}
```

### JapanTaxCalculator はどこで使われるの？

「じゃあ、本物の `JapanTaxCalculator` はいつ登場するの？」と思いますよね。それは、**アプリケーションの起動時（エントリーポイント）**です。

```csharp
// Program.cs など（アプリケーションの実行時）
var realTax = new JapanTaxCalculator(); // ここで本物を作る
var calculator = new OrderCalculator(realTax); // 本物を注入する
var result = calculator.CalculateTotal(1000, 2);
```

このように、本番では「本物」を渡し、テストでは「偽物」を渡す。
これがインターフェースに分離する最大のメリットです。

### 分離した後のテストはどう変わる？

インターフェースに分離したことで、それぞれのクラスが持つ**「振る舞い」**を、他の影響を受けずにテストできるようになります。

ここで重要なのは、**「振る舞い」の定義をそれぞれのクラスの役割に合わせる**ことです。

#### 1. JapanTaxCalculator のテスト（計算ロジックの振る舞い）
「日本の税金計算機」としての振る舞い、つまり「10%の税金が正しく加算されるか」をテストします。

```csharp
[Fact]
public void Apply_ReturnsAmountWith10PercentTax()
{
    var taxCalc = new JapanTaxCalculator();
    
    // 「1000円なら1100円になる」という、このクラス自身の振る舞いを検証
    var result = taxCalc.Apply(1000m);
    
    Assert.Equal(1100m, result);
}
```

#### 2. OrderCalculator のテスト（注文管理の振る舞い）
「注文管理」としての振る舞い、つまり「価格×個数を計算し、税金計算を正しく依頼しているか」をテストします。ここでは本物の税率が10%か8%かは重要ではないため、偽物（スタブ）を使って切り離します。

```csharp
[Fact]
public void CalculateTotal_CallsTaxCalculatorWithCorrectAmount()
{
    // 準備：常に100円を足すだけの「偽物」を使う
    var stubTax = new StubTaxCalculator(); 
    var calculator = new OrderCalculator(stubTax);

    // 実行：1000円×1個 ＋ 偽物の税金(100円)
    var result = calculator.CalculateTotal(1000, 1);

    // 検証：合計が1100円になっているか
    Assert.Equal(1100m, result);
}

public class StubTaxCalculator : ITaxCalculator
{
    public decimal Apply(decimal amount) => amount + 100m; // テストしやすい固定値
}
```

### なぜこれが「壊れにくい」の？

このように「計算の振る舞い」と「管理の振る舞い」を分けてテストしておくと、将来が楽になります。

例えば、**「消費税が15%に上がった」**という変更を想像してください。
このとき修正が必要なのは、`JapanTaxCalculator` のロジックと、そのテストコードだけです。

`OrderCalculator` のテストは、偽物（スタブ）を使っているため**一切落ちることはありません。**

もしインターフェースに分けず、`OrderCalculator` のテストで本物の税金計算を使っていたら、税率が変わるたびに関連するすべてのテストが全滅してしまいます。これが「実装の詳細（特定の税率）」に依存しない、**「振る舞い（役割）のテスト」**の強みです。

## まとめ：良いテストが、良い設計を作る

テストコードを書く目的は、バグを見つけることだけではありません。**「保守しやすい、綺麗なコードを維持し続けること」**にあります。

- **振る舞いをテストする**: 実装を隠蔽し、リファクタリング耐性を高める。
- **publicを入り口にする**: クラスの利用者の視点で仕様を定義する。
- **インターフェースを活用する**: 複雑な依存関係を切り離し、テストの自由度を上げる。

この原則を守ることで、テストコードはあなたの足を引っ張る「お荷物」から、開発を力強く支える「お守り」へと変わるはずです。

今日から、privateメソッドのテストをやめて、そのクラスが提供する「価値（振る舞い）」をテストしてみては？

では、また!
