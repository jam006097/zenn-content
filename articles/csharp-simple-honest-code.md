---
title: "嘘をつかないコードを書く：シンプルなコードのほうがちょっと幸せになれるよって話"
emoji: "📖"
type: "tech"
topics: ["csharp", "dotnet", "design", "clean-code", "testing"]
published: false
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

## 「賢いコード」が生む苦しみ

経験を積んだプログラマーほど、陥りやすいワナがあります。

```csharp
// ワナその1：リフレクションで「汎用化」した例
public object GetValue(object obj, string propertyName)
{
    return obj.GetType()
               .GetProperty(propertyName)
               ?.GetValue(obj);
}
```

「プロパティ名を文字列で渡せば何でも取れる！汎用的だ！」と思いたくなりますが、これには大きな問題があります。

- `propertyName` のスペルを間違えても**コンパイルエラーにならない**
- どこから呼ばれているか**IDEで追跡できない**
- テストを書いても**何をテストしているのかわかりにくい**

この手の「賢い書き方」は、書いた本人にしか読めないコードを生み出します。そしてその本人も、3ヶ月後には「自分で書いたのに読めない」という状態になります。

---

## シンプルなコードは「嘘をつかない」

では、良いコードとはどんなコードでしょうか。

```csharp
// シンプルな例：ユーザーに割引を適用する
public decimal ApplyDiscount(decimal price, int discountPercent)
{
    if (discountPercent < 0 || discountPercent > 100)
        throw new ArgumentException("割引率は0〜100の間で指定してください");

    return price * (1 - discountPercent / 100m);
}
```

このコードは短く、やっていることが一目でわかります。メソッド名を見れば「割引を適用する」とわかり、引数を見れば「価格と割引率を渡す」とわかります。

**初めて読んだ人が理解できるコード**、それが「嘘をつかないコード」です。

---

## テストコードが「動く説明書」になる

シンプルなコードとセットで効果を発揮するのが、テストコードです。

テストコードは品質のためだけにあるのではありません。「このメソッドはこう使うもので、こういう結果を返す」という**使い方の例**を実行可能な形で残せます。

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

### メタプログラミング・リフレクション

冒頭で紹介したリフレクションの例がまさにこれです。「汎用化できる」という魅力がありますが、コードの流れを追えなくなります。IDEの「定義へ移動」「参照を探す」が効かなくなり、リファクタリングが地獄になります。

```csharp
// やめよう：何が呼ばれるかコンパイル時にわからない
var method = typeof(OrderService).GetMethod("Process" + orderType);
method?.Invoke(service, new object[] { order });

// こうしよう：素直に書く
if (orderType == "Normal")
    service.ProcessNormalOrder(order);
else if (orderType == "Express")
    service.ProcessExpressOrder(order);
```

下の書き方は「冗長」に見えるかもしれませんが、**何が起きるかコードを読めばわかる**というのは大きな価値です。

### 「賢い」一行コード

```csharp
// 読めなくはないけど、一瞬止まる
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

## シンプルなコードの判断基準

自分が書いたコードを見直すとき、こう自問してみてください。

**「3年目のプログラマーが初めて読んで、5分で理解できるか？」**

この基準は実はかなり高い水準です。ここでいう「3年目」とは「普通のC#は書けるが、特殊な言語機能やマジックナンバーは知らないかもしれない」くらいのイメージです。

もし「わかってもらうには説明が必要」なコードなら、それはコード自体がわかりにくいサインです。コメントを追加するのではなく、**コードそのものをわかりやすく書き直す**ことを検討しましょう。

---

## まとめ

シンプルなコードを書くということは、手を抜くことではありません。「読む人」のことを考えて設計することです。

- **初めて見た人が理解できるコードを書く**
- **テストコードで「使い方と動作」を伝える**
- **リフレクションやメタプログラミングは後でつらくなるから使わない**
- **一行に詰め込みすぎず、素直に書く**

「賢いコード」は書いた瞬間だけ気持ちいいですが、「シンプルなコード」はチームを長期間助け続けます。

あなたが書く次のコードが、未来の誰か（自分かもしれない）にとって「読みやすくて助かった」と思ってもらえるものになることを願っています。

では、また！
