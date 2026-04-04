---
title: "嘘をつかないコードを書く：シンプルなコードのほうがちょっと幸せになれるよって話"
emoji: "📖"
type: "tech"
topics: ["csharp", "dotnet", "design", "clean-code", "testing"]
published: true
---

## はじめに

「このコード、何をやっているんだろう...」
「書いた本人に聞かないと、触るのが怖い...」

そんなコードに出会ったことはありませんか？僕はしょっちゅうあります。😅

プログラミングに慣れてくると、「もっとスマートに書けるはず」「こんな書き方もできるぞ」という誘惑が出てきます。
今の僕ですね👻
リーダブルコードをはじめとした名著たちに助けてもらって立ち止まっている今日この頃です。

**そのコード、初めて見た人に伝わりますか？**

この記事では「嘘をつかないコード」を書くことの大切さを、C#の実例を交えてお伝えします。

---

## 「嘘をつくコード」とは？

「嘘をつくコード」とは、メソッド名や見た目と、実際の中身がズレているコードのことです。

例えば、次のメソッドを見てください。

```csharp
// 嘘をつくコード
public User GetActiveUser(int userId)
{
    var user = _repository.Find(userId);
    user.LastLoginAt = DateTime.Now; // ← 取得しているのに、状態も変えている！
    _repository.Save(user);
    return user;
}
```

`Get` という名前がついているので「ユーザーを取得するメソッドだ」と思って呼び出したら、実はDBの更新まで走っていた。これが「嘘をつくコード」です。

呼び出した側には何も知らされず、副作用が起きている。この手のコードは、デバッグ中に「なぜか値が変わっている」という謎バグを生み出します。

### コマンドクエリ分離（CQS）の原則

この問題を防ぐシンプルな原則があります。**コマンドクエリ分離（Command Query Separation）** です。

- **クエリ（Query）**：値を返す。状態を変えない。
- **コマンド（Command）**：状態を変える。値を返さない。

この2つを混ぜないだけで、コードの「嘘」が大幅に減ります。

```csharp
// クエリ：値を返すだけ、状態を変えない
public User GetUser(int userId)
{
    return _repository.Find(userId);
}

// コマンド：状態を変えるだけ、値を返さない
public void RecordLogin(int userId)
{
    var user = _repository.Find(userId);
    user.LastLoginAt = DateTime.Now;
    _repository.Save(user);
}
```

呼び出す側は「取得したいなら `GetUser`」「ログインを記録したいなら `RecordLogin`」と、名前を見るだけで意図がわかります。メソッド名が嘘をつかなくなります。

---

## 引数も「嘘をつく」

メソッド名だけでなく、引数も嘘をつくことがあります。よくあるのが **booleanの引数を並べる** パターンです。

```csharp
// 呼び出す側からは何もわからない
ProcessOrder(order, true, false);
```

`true` と `false` が何を意味するのか、呼び出し側のコードを読んだだけではわかりません。定義を見に行くまで意味が不明です。これは「引数が嘘をつく」状態です。

### 名前付き引数で意図を伝える

C#では**名前付き引数**を使うことで、引数に意味を持たせることができます。

```csharp
// 名前付き引数で意図が伝わる
ProcessOrder(
    order,
    sendConfirmationEmail: true,
    requireManagerApproval: false
);
```

呼び出す側のコードだけで「確認メールを送る」「上長承認は不要」と読み取れます。定義を見に行く必要がありません。

### enumで「ありえない組み合わせ」を防ぐ

さらに一歩進めると、`bool` 2つを `enum` に置き換えることで、より正直なコードになります。

```csharp
public enum EmailNotification { Send, Skip }
public enum ManagerApproval   { Required, NotRequired }

// 呼び出し側：何をしているか一目でわかる
ProcessOrder(order, EmailNotification.Send, ManagerApproval.NotRequired);
```

`bool` では「どちらが何を指しているか」を常に覚えておく必要がありますが、`enum` にすることで名前が意図を語ります。引数の順番を間違えたとき、コンパイルエラーで気づけるのも利点です。

---

## テストコードが「動く説明書」になる

シンプルなコードとセットで効果を発揮するのが、テストコードです。

テストコードは品質のためだけにあるのではありません。「このメソッドはこう使うもので、こういう結果を返す」という**使い方の例**を実行可能な形で残せます。

次のシンプルな割引計算メソッドを例にしてみましょう。

```csharp
public decimal ApplyDiscount(decimal price, int discountPercent)
{
    if (discountPercent < 0 || discountPercent > 100)
        throw new ArgumentException("割引率は0〜100の間で指定してください");

    return price * (1 - discountPercent / 100m);
}
```

このメソッドに対してテストを書くとこうなります。

```csharp
public class DiscountServiceTests
{
    [Fact]
    public void ApplyDiscount_10パーセント割引_正しく計算される()
    {
        // Arrange：準備
        var service = new DiscountService();
        decimal price = 1000m;
        int discountPercent = 10;

        // Act：実行
        decimal result = service.ApplyDiscount(price, discountPercent);

        // Assert：検証
        Assert.Equal(900m, result);
    }

    [Fact]
    public void ApplyDiscount_割引率が100を超える場合_例外が発生する()
    {
        var service = new DiscountService();

        // 範囲外の割引率は例外になることをテストで明示する
        Assert.Throws<ArgumentException>(() =>
            service.ApplyDiscount(1000m, 150));
    }
}
```

### テストコードが「嘘をつかない」ために

テストにも「嘘をつかない」という視点が大切です。注目してほしいのが**テストメソッドの名前**です。

- `ApplyDiscount_10パーセント割引_正しく計算される`
- `ApplyDiscount_割引率が100を超える場合_例外が発生する`

このように「何をテストして、何が起きるか」を名前に込めることで、テスト一覧を見ただけで仕様が読み取れます。`Test1` や `TestMethod_Case2` のような名前では、テストが嘘をつく——というより、何も語らない状態になってしまいます。

テストコードも同じコードです。名前と中身を一致させることが、後で読む人（未来の自分）への誠実さです。

---

## まとめ

シンプルなコードを書くということは、手を抜くことではありません。「読む人」のことを考えて設計することです。
むしろ難しいです。

- **メソッド名と中身を一致させる**（GetなのにSaveしない）
- **コマンドとクエリを分離する**（副作用を名前に隠さない）
- **引数に意味を持たせる**（boolの羅列をやめ、名前付き引数やenumを使う）
- **テストの名前で仕様を語る**（テストコードも嘘をつかない）

「賢いコード」は書いた瞬間だけ気持ちいいですが、「シンプルなコード」は長期的に自分を助けます。

あなたが書く次のコードが、未来の誰か（たぶん自分！）にとって「読みやすくて助かった」と思ってもらえるものになることを願っています。

では、また！👻
