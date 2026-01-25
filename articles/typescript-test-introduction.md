---
title: "手を動かして学ぶ、TypeScriptのテスト入門 〜自信と安心を手に入れる最初の一歩〜"
emoji: "🧪"
type: "tech"
topics: ["typescript", "jest", "test"]
published: false
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

まず、`npm`を使ってプロジェクトを初期化し、TypeScriptとJestをインストールします。

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

上記コマンドで生成された`tsconfig.json`は、Jestで動作させるための基本的なオプションを含んでいます。

続いて、Jestの設定ファイル `jest.config.js` を作成します。

```javascript
// jest.config.js
/** @type {import('ts-jest').JestConfigWithTsJest} */
module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  testMatch: ["**/?(*.)+(spec|test).[tj]s?(x)"],
};
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

`src/date-calculator.ts` というファイルを作成し、以下の関数を記述します。

```typescript
// src/date-calculator.ts
export function calculateDaysBetweenDates(date1: Date, date2: Date): number {
  // NOTE: この関数はシンプルさのため、Dateオブジェクトを直接扱っています。
  // より複雑な日付操作が必要なドメインでは、date-fnsのようなライブラリで抽象化する設計も有効です。

  // 1日のミリ秒数
  const ONE_DAY_IN_MILLISECONDS = 1000 * 60 * 60 * 24;

  // タイムゾーンの影響を避けるため、UTCで日付の差を計算
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

// `describe`で、関連するテストをグループ化します
describe('calculateDaysBetweenDates', () => {
  // `it` (または `test`) で、個々のテストケースを記述します
  it('2023年1月1日と2023年1月5日の間の日数を正しく計算すべきである', () => {
    // Arrange (準備): テストに必要なデータや状態を整える
    const date1 = new Date('2023-01-01');
    const date2 = new Date('2023-01-05');
    
    // Act (実行): テスト対象のコードを実行する
    const days = calculateDaysBetweenDates(date1, date2);

    // Assert (検証): 実行結果が期待通りか確認する
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

テストコードは **Arrange-Act-Assert (準備-実行-検証)** というパターンで構造化すると、意図が明確になります。

## 第3章: テストの実行と結果の確認

`npm test` コマンドでテストを実行します。

**成功した場合**、Jestは `PASS` というメッセージと共に、どのテストが成功したかを表示します。緑の文字は安心の色です。

```
 PASS  src/date-calculator.test.ts
  calculateDaysBetweenDates
    ✓ 2023年1月1日と2023年1月5日の間の日数を正しく計算すべきである (2ms)
    ✓ 同じ日付なら0日を返すべきである
    ✓ 日付の順序が逆でも同じ結果を返すべきである
```

**失敗した場合**、`FAIL` というメッセージと、どのテストがなぜ失敗したかの詳細が表示されます。

```
 FAIL  src/date-calculator.test.ts
  ● calculateDaysBetweenDates › 同じ日付なら0日を返すべきである

    expect(received).toBe(expected) // Object.is equality

    Expected: 0
    Received: 1

      at src/date-calculator.test.ts:21:20
```

`Expected` (期待した値) と `Received` (実際の値) の違いが明確に示されるため、問題の特定が容易です。

## 第4章: テストで仕様を明確にする

テストは、最初に想定していなかった「エッジケース」を洗い出し、関数の振る舞いをより明確に定義する手助けとなります。

### 4.1 エラー処理の仕様を決める

もし、無効な日付が渡されたらどうなるでしょうか？この関数の責務として、不正な入力を受け付けないことを明確にするため、エラーを投げる設計にしてみましょう。 **この判断自体が、テストを書くことで促進される設計活動です。**

まず、`calculateDaysBetweenDates` 関数にバリデーションを追加します。

```typescript
// src/date-calculator.ts (冒頭部分)
export function calculateDaysBetweenDates(date1: Date, date2: Date): number {
  if (!date1 || !date2 || isNaN(date1.getTime()) || isNaN(date2.getTime())) {
    throw new Error('Invalid Date object provided.');
  }
  // ... 以下、実装は同じ
```

そして、この新しい「仕様」をテストコードで言語化します。

```typescript
// src/date-calculator.test.ts
describe('calculateDaysBetweenDates', () => {
  // ... 既存のテスト ...

  it('無効な日付が渡された場合にエラーを投げるべきである', () => {
    const invalidDate = new Date('not a date');
    const validDate = new Date('2023-01-01');

    // toThrow: 関数が特定のエラーを投げることを検証する
    expect(() => calculateDaysBetweenDates(invalidDate, validDate)).toThrow('Invalid Date object provided.');
  });
});
```

`toThrow` マッチャーを使うことで、「エラーを投げること」がこの関数の正しい振る舞いである、と定義できました。

### 4.2 隠れた仕様を発見する：閏年の考慮

日付計算には「閏年」という隠れた仕様が常に存在します。これもテストによって明確にしましょう。

```typescript
// src/date-calculator.test.ts
describe('calculateDaysBetweenDates', () => {
  // ... 既存のテスト ...

  it('閏年をまたぐ計算が正しく行われるべきである', () => {
    const date1 = new Date('2024-02-28'); // 2024年は閏年
    const date2 = new Date('2024-03-01');
    expect(calculateDaysBetweenDates(date1, date2)).toBe(2);
  });

  it('平年の計算が正しく行われるべきである', () => {
    const date1 = new Date('2023-02-28'); // 2023年は平年
    const date2 = new Date('2023-03-01');
    expect(calculateDaysBetweenDates(date1, date2)).toBe(1);
  });
});
```

これらのテストを通じて、私たちの関数が閏年を正しく扱えることが保証されました。テストを書かなければ、この仕様は曖昧なままだったかもしれません。

## おわりに

本記事では、TypeScriptとJestを使ったテストの基本を解説しました。テストは単なるバグチェックではなく、コードの仕様を明確にし、設計を改善し、未来の変更に対する自信と安心感を与えてくれる強力なツールです。

最初は難しく感じるかもしれませんが、まずは小さな関数からテストを書いてみてください。テストが一つ、また一つと増えるたびに、あなたのコードベースはより堅牢なものになっていくはずです。

さらに学びたい方は、外部APIとの連携をテストする「モック」や、UIコンポーネントのテスト手法についても調べてみることをお勧めします。

テストを書く習慣を身につけ、自信に満ちた開発ライフを送りましょう！
