---
title: "[ちょっと幸せになる」TypeScriptのテスト入門 〜自信と安心〜"
emoji: "🧪"
type: "tech"
topics: ["typescript", "jest", "test"]
published: true
---

## はじめに

「この一行を変えたら、どこかで何かが壊れるかも…」そんな不安を感じながらコードを改修した経験はありませんか？

テストコードは、単にバグを見つけるためだけのツールではありません。それは、私達開発者が **安心してコードを書き、自信を持ってプロダクトを成長させていくための強力な武器** であり、精神的な安定を保つためのパートナーです。

この記事では、TypeScriptとJestを使い、テスト環境の構築から基本的なテストの書き方までを、CLIでの操作を中心に丁寧に解説します。この記事を読み終える頃には、テストを書くことへの第一歩を踏み出し、開発者としての自信と安心感を手に入れているはずです。

**対象読者**

*   テストコードを書いた経験がない、または少ない方
*   TypeScriptでの開発経験が少しある方
*   Unix系のOS（Linux, macOSなど）で開発されている方

## 第1章: テスト環境を準備する

まず、テストを実行するための環境を整えましょう。Node.jsがインストール済みの環境を想定しています。

### 1.1 プロジェクトのセットアップ

`npm`を使ってプロジェクトを初期化し、TypeScriptとJestをインストールします。

```bash
# 作業ディレクトリを作成し、移動します
mkdir typescript-test-project
cd typescript-test-project

# npmプロジェクトを初期化
npm init -y

# TypeScriptとJest関連ライブラリを開発依存としてインストール
npm install -D typescript @types/node jest @types/jest ts-jest
```

### 1.2 設定ファイルの作成

次に、TypeScriptとJestの設定ファイルを作成します。

```bash
# TypeScriptの設定ファイル (tsconfig.json) を作成
npx tsc --init --target es2016 --module commonjs --strict --esModuleInterop --skipLibCheck --forceConsistentCasingInFileNames
```

上記コマンドで`tsconfig.json`を生成し、Jestのための基本的な設定を行います。続いて、`jest.config.js`を作成します。以下のコマンドでファイルを作成できます。

```bash
cat > jest.config.js << 'EOF'
/** @type {import('ts-jest').JestConfigWithTsJest} */
module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  testMatch: ["**/?(*.)+(spec|test).[tj]s?(x)"],
};
EOF
```

最後に、`package.json`の`scripts`を修正し、`npm test`でJestが実行されるようにします。

```json
// package.json (scripts部分のみ)
"scripts": {
  "test": "jest"
},
```

これで環境構築は完了です。

## 第2章: はじめてのテストを書いてみる

テスト対象の関数を作り、その振る舞いを定義するテストを書いていきましょう。題材は「2つの日付間の日数を計算する関数」です。

### 2.1 テスト対象の関数

`src/date-calculator.ts` を作成し、以下の関数を記述します。

```typescript
// src/date-calculator.ts
export function calculateDaysBetweenDates(date1: Date, date2: Date): number {
  // NOTE: この関数はシンプルさのため、Dateオブジェクトを直接扱っています。
  // より複雑な日付操作が必要なドメインでは、date-fnsのようなライブラリで抽象化する設計も有効です。

  const ONE_DAY_IN_MILLISECONDS = 1000 * 60 * 60 * 24;

  const utcDate1 = Date.UTC(date1.getFullYear(), date1.getMonth(), date1.getDate());
  const utcDate2 = Date.UTC(date2.getFullYear(), date2.getMonth(), date2.getDate());

  const diffMilliseconds = Math.abs(utcDate1 - utcDate2);
  return Math.floor(diffMilliseconds / ONE_DAY_IN_MILLISECONDS);
}
```

### 2.2 テストが仕様を言語化する

これから書くテストコードが、この `calculateDaysBetweenDates` 関数の **「動く仕様書」** となります。テストをパスするコードだけが、私達が意図した仕様を満たしていると言えるのです。

`src/date-calculator.test.ts` を作成し、最初のテストを書いてみましょう。

```typescript
// src/date-calculator.test.ts
import { calculateDaysBetweenDates } from './date-calculator';

describe('calculateDaysBetweenDates', () => {
  it('2023年1月1日と2023年1月5日の間の日数を正しく計算すべきである', () => {
    // 準備 (Arrange)
    const date1 = new Date('2023-01-01');
    const date2 = new Date('2023-01-05');
    
    // 実行 (Act)
    const days = calculateDaysBetweenDates(date1, date2);

    // 検証 (Assert)
    expect(days).toBe(4);
  });

  // 仕様をさらに明確にするテストケースを追加
  it('同じ日付なら0日を返すべきである', () => {
    const date = new Date('2023-03-15');
    expect(calculateDaysBetweenDates(date, date)).toBe(0);
  });

  it('日付の順序が逆でも同じ結果を返すべきである', () => {
    const date1 = new Date('2023-01-05');
    const date2 = new Date('2023-01-01');
    expect(calculateDaysBetweenDates(date1, date2)).toBe(4);
  });
});
```

テストコードは **準備(Arrange)・実行(Act)・検証(Assert)** の3ステップで構成すると、意図が明確になります。

## 第3章: テストの実行と呼吸

`npm test` コマンドでテストを実行します。

テストがすべて成功すると、`PASS src/date-calculator.test.ts` のように、全テストが成功したことを示す緑色のメッセージが表示されます。これが、あなたが安心して次の開発に進むための「青信号」です。

もしテストが一つでも失敗すれば、Jestは `FAIL` というメッセージと共に、どのテストがなぜ失敗したのかを教えてくれます。

```
 FAIL  src/date-calculator.test.ts
  ● calculateDaysBetweenDates › 同じ日付なら0日を返すべきである

    expect(received).toBe(expected) // Object.is equality

    Expected: 0
    Received: 1

      at src/date-calculator.test.ts:21:20
```

`Expected` (期待した値) と `Received` (実際の値) の違いが明確に示されるため、問題の特定が容易です。この「赤信号」があるからこそ、私たちはリファクタリングの際にも安心してコードの海を渡っていけるのです。

## 第4章: テストが設計を変える瞬間

テストを書くことは、受動的な確認作業ではありません。 **テストを書いたことで、「この関数は何を保証すべきか」が初めて言語化され、より良い設計へと導かれる**、能動的な設計活動なのです。

### 4.1 エラー処理という仕様の決定

もし、無効な日付が渡されたらどうなるでしょうか？この問いに答えることで、私たちは関数の仕様を決定します。ここでは「不正な入力を受け付けない」ことを明確にするため、エラーを投げる設計を選びました。

まず、`calculateDaysBetweenDates` 関数にバリデーションを追加します。

```typescript
// src/date-calculator.ts (冒頭部分)
export function calculateDaysBetweenDates(date1: Date, date2: Date): number {
  if (!date1 || !date2 || isNaN(date1.getTime()) || isNaN(date2.getTime())) {
    throw new Error('Invalid Date object provided.');
  }
  // ... 以下、実装は同じ
```

そして、この新しい「仕様」をテストコードで表現します。

```typescript
// src/date-calculator.test.ts
it('無効な日付が渡された場合にエラーを投げるべきである', () => {
  const invalidDate = new Date('not a date');
  const validDate = new Date('2023-01-01');

  // toThrow: 関数が特定のエラーを投げることを検証する
  expect(() => calculateDaysBetweenDates(invalidDate, validDate)).toThrow('Invalid Date object provided.');
});
```

`toThrow` マッチャーにより、「エラーを投げること」がこの関数の正しい振る舞いである、と定義できました。

### 4.2 隠れた仕様の発見：閏年

日付計算には「閏年」という、見落としがちな仕様が常に存在します。これもテストによって明確にしましょう。

```typescript
// src/date-calculator.test.ts
it('閏年をまたぐ計算が正しく行われるべきである', () => {
  const date1 = new Date('2024-02-28'); // 2024年は閏年
  const date2 = new Date('2024-03-01');
  expect(calculateDaysBetweenDates(date1, date2)).toBe(2);
});
```

このテストを書いたことで、私たちの関数が閏年を正しく扱えることが保証されました。ちなみに、平年（例: 2023年）の場合は `toBe(1)` となるテストも追加すると、より仕様が明確になりますね。

## おわりに

本記事では、TypeScriptとJestを使ったテストの基本を解説しました。

テストは、書く前から存在していた仕様を確認するものではなく、 **書く過程で仕様を発見し、定義するための対話的な道具** です。テストが一つ、また一つと増えるたびに、あなたのコードベースはより堅牢なものになっていきます。

さらに学びたい方は、外部依存を扱う「モック」や、UIコンポーネントのテスト手法についても調べてみることをお勧めします。

テストを書く習慣を身につけ、自信に満ちた開発ライフを送りましょう！
