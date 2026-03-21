---
title: "制約は優しさ：C#でバグを減らす『不自由な』設計のすすめ"
emoji: "🛡️"
type: "tech"
topics: ["csharp", "architecture", "design", "security"]
published: true
---

## はじめに

「このメソッド、引数に何を入れても動いちゃうけど、本当に大丈夫かな？」
「いつの間にか変数の値が変わっていて、原因を探すのに半日溶かした...」

プログラミングをしていると、自由度が高すぎることが逆に牙を向くことがあります。開発者なら誰でも、「何でもできるコード」が「何が起きるかわからない恐怖」に変わる瞬間を経験したことがあるはずです。

この記事では、**「制約（ルール）を増やすことで、逆にバグを減らし、開発を楽にする」**という考え方をご紹介します。

この考え方は、名著『Secure by Design』や『Good Code, Bad Code』でも一貫して語られている重要なトピックです。C#を使って、具体的にどう「不自由」にすれば安全になれるのか、一緒に見ていきましょう。

## なぜ「制約」がバグを減らすのか？

直感的には、「自由なほうが作りやすい」と感じるかもしれません。しかし、ソフトウェア開発における自由は、しばしば**「間違える自由」**も含んでしまいます。

- 負の値を入れられる `int` 型の「年齢」
- どこからでも書き換えられる「グローバルな変数」
- どんな文字列でも受け付ける「メールアドレス」

これらはすべて、バグが入り込む隙間になります。制約を課すということは、この**「間違える隙間」をコードの構造で物理的に塞いでしまう**ということなのです。

## 1. プリミティブ型への執着を捨てる（Value Object）

一番身近な例は、`int` や `string` といった基本型（プリミティブ型）の使いすぎです。これを「プリミティブ型への執着」と呼びます。

### 良くない例：ただの `int`
```csharp
public void RegisterUser(string name, int age) 
{
    // age に -100 とか 20000 とかが入ってくるかもしれない...
    if (age < 0 || age > 150) throw new ArgumentException("無効な年齢です");
    // ...
}
```
このコードでは、メソッドを呼び出すたびに「正しい値かな？」と心配する必要があります。

### 良い例：制約を持った `Age` 型
C#の `record` を使って、適切な制約を閉じ込めた専用の型（Value Object）を作りましょう。

```csharp
public record Age
{
    public int Value { get; }

    public Age(int value)
    {
        if (value < 0 || value > 150) 
            throw new ArgumentException("年齢は0から150の間でなければなりません");
        Value = value;
    }
}

// 使うとき
public void RegisterUser(string name, Age age) 
{
    // ここに来た時点で、age は必ず「正しい範囲内」であることが保証されている！
}
```
型を `int` から `Age` に変えるだけで、「不正な年齢によるバグ」は物理的に発生しなくなります。

## 2. 「後から変えられない」という安心（不変性）

バグの温床になりやすいのが「状態の変化」です。

### 良くない例：いつでも書き換え可能
```csharp
public class User
{
    public string Name { get; set; }
    public int Score { get; set; }
}
```
`set;` が公開されていると、予期せぬ場所で `Score` が書き換えられてしまうかもしれません。

### 良い例：`init` と `readonly`
C# 9.0以降で導入された `init` キーワードや `record` を使うと、「作成時だけ設定可能で、後は読み取り専用」という制約を簡単に作れます。

```csharp
public record User(string Name, int Score);

// またはプロパティで制限する
public class User
{
    public string Name { get; init; } // 初期化時のみ設定可能
    public int Score { get; private set; } // 変更はクラス内部からのみ

    public void AddScore(int point) => Score += point;
}
```
「一度作ったオブジェクトは変わらない」という制約があるだけで、デバッグの際に「どこで値が変わったんだ！？」と犯人探しをする必要がなくなります。

## 3. 「ありえない状態」を型で消し去る

例えば、「注文」の状態を管理するとき、フラグを複数使うと矛盾が生じることがあります。

### 良くない例：複数の bool
```csharp
public class Order
{
    public bool IsShipped { get; set; }
    public bool IsCancelled { get; set; }
    // もし両方が true になったら...？（ありえない状態の発生）
}
```

### 良い例：列挙型（Enum）と switch 式
状態を一つに絞る制約を加えます。

```csharp
public enum OrderStatus
{
    Pending,
    Shipped,
    Cancelled
}

public class Order
{
    public OrderStatus Status { get; private set; }

    public void Ship()
    {
        if (Status != OrderStatus.Pending) return;
        Status = OrderStatus.Shipped;
    }
}
```
さらに、C#の強力な **パターンマッチング** を使えば、すべての状態を考慮しているかコンパイラにチェックさせることもできます。

## まとめ：制約は開発者への「優しさ」

『Secure by Design』では、「ドメインプリミティブ」という考え方で、型そのものにルールを閉じ込める重要性を説いています。また、『Good Code, Bad Code』では、「間違った使い方ができないように設計する」ことが良いコードの条件だとしています。

一見すると、制約を増やすのは面倒に思えるかもしれません。しかし、その少しの手間が、将来のあなたやチームメイトを**「終わりのないデバッグ」から救ってくれます。**

- **「何でもできる」** ではなく **「正しいことしかできない」**
- **「実行時に気づく」** ではなく **「コンパイル時に気づく」**

今日から、あなたのコードに少しだけ「優しい制約」を加えてみませんか？

## 参考文献
- [Secure by Design: Proper design promotes security](https://www.manning.com/books/secure-by-design)
- [Good Code, Bad Code: Think like a software engineer](https://www.manning.com/books/good-code-bad-code)
