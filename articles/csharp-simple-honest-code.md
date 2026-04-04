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

この記事では「シンプルで正直なコード」を書くことの大切さを、C#の実例を交えてお伝えします。

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

このテストを読むだけで「`ApplyDiscount` に何を渡すと何が返ってくるか」が具体的にわかります。ドキュメントが古くなっても、テストは常に最新の動作を示します。

---

## やめてほしい「後でつらくなる書き方」

### リフレクションで画面要素を動的に取得する

「フォームのコントロールを汎用的に設定したい」という気持ちはわかります。でも、リフレクションを使った途端に、コードは追跡不能になります。

```csharp
// やめよう：リフレクションで画面要素を動的に操作
public void SetFieldValues(Form form, Dictionary<string, string> values)
{
    foreach (var kvp in values)
    {
        var control = form.Controls.Find(kvp.Key, true).FirstOrDefault();
        var prop = control?.GetType().GetProperty("Text");
        prop?.SetValue(control, kvp.Value);
    }
}
```

このコードには次の問題があります。

- コントロール名を**文字列で指定**するため、タイポしてもコンパイルエラーにならない
- IDEの「参照を探す」が機能しないため、**どこから呼ばれているか追えない**
- テストを書いても何をテストしているかわかりにくい

```csharp
// こうしよう：冗長でも素直に書く
public void SetFieldValues(OrderForm form, OrderData data)
{
    form.NameTextBox.Text    = data.CustomerName;
    form.EmailTextBox.Text   = data.Email;
    form.AddressTextBox.Text = data.Address;
}
```

行数は増えますが、何をしているかは一目瞭然です。フィールドを追加・削除するときも、変更箇所がコンパイルエラーで明示されます。「冗長」に見えるこの書き方こそが、後で助かるコードです。

### 「賢い」一行コード

```csharp
// 一瞬止まる書き方
var result = items?.Where(x => x?.IsActive == true)?.Select(x => x!.Name)?.ToList() ?? new List<string>();

// 素直に書くとこうなる
if (items == null)
    return new List<string>();

return items
    .Where(item => item != null && item.IsActive)
    .Select(item => item.Name)
    .ToList();
```

後者の方が行数は多いですが、初めて見た人が迷わず読めます。「読みやすさ」は後から修正するコストを下げる、立派なパフォーマンスです。

---

## まとめ

シンプルなコードを書くということは、手を抜くことではありません。「読む人」のことを考えて設計することです。

- **メソッド名と中身を一致させる**（嘘をつかない）
- **コマンドとクエリを分離する**（副作用を隠さない）
- **テストコードで「使い方と動作」を伝える**
- **リフレクションやメタプログラミングは後でつらくなるから使わない**

「賢いコード」は書いた瞬間だけ気持ちいいですが、「シンプルなコード」は長期的に自分を助けます。

あなたが書く次のコードが、未来の誰か（たぶん自分!）にとって「読みやすくて助かった」と思ってもらえるものになることを願っています。

では、また！
