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

テスト駆動開発（TDD）のリズム「レッド→グリーン→リファクタリング」に従って、最初のテストを書いていきましょう。題材は「2つの日付間の日数を計算する関数」です。

### 2.1 [レッド] 最初のテストを書き、そして失敗させる

TDDのサイクルは、まず **「失敗するテスト（レッド）」** を書くことから始まります。

1.  **テストファイルの準備**
    まず、テストコードを置くためのファイルを作成します。`src` ディレクトリがなければ作成してください。

    ```bash
    mkdir -p src
    # テストファイルを作成（まだ実装ファイルは作らない）
    touch src/date-calculator.test.ts
    ```

2.  **テストコードの記述**
    次に、`src/date-calculator.test.ts` に、これから作る `calculateDaysBetweenDates` 関数の振る舞いを定義するテストを書きます。まだ関数そのものは存在しないことに注意してください。

    ```typescript
    // src/date-calculator.test.ts
    // まだ存在しないファイルからimportしようとします
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
    });
    ```

3.  **テストの失敗を確認**
    この状態でテストを実行すると、`date-calculator.ts` が存在しないため、当然エラーになります。

    ```bash
    npm test
    ```
    
    ```bash
     FAIL  src/date-calculator.test.ts
      ● Test suite failed to run

        Cannot find module './date-calculator' from 'src/date-calculator.test.ts'
    ```
    おめでとうございます！これがTDDにおける最初の「赤信号（レッド）」です。私たちは今、実装すべき目標をコードで明確に定義しました。

### 2.2 [グリーン] テストをパスさせる

赤信号を青信号（グリーン）に変えましょう。テストをパスさせるための、最もシンプルな実装を追加します。

1.  **実装ファイルの作成と最小限の実装**
    エラーを解決するため、`src/date-calculator.ts` を作成し、テストをパスするためだけのコードを書きます。

    ```bash
    touch src/date-calculator.ts
    ```

    ```typescript
    // src/date-calculator.ts
    // とにかくテストをパスさせるためだけに「4」を返す
    export function calculateDaysBetweenDates(date1: Date, date2: Date): number {
      return 4;
    }
    ```

2.  **テストの成功を確認**
    再度テストを実行すると、今度は成功するはずです。

    ```bash
    npm test

    > typescript-test-project@1.0.0 test
    > jest

     PASS  src/date-calculator.test.ts
      calculateDaysBetweenDates
        ✓ 2023年1月1日と2023年1月5日の間の日数を正しく計算すべきである (2 ms)

    Test Suites: 1 passed, 1 total
    Tests:       1 passed, 1 total
    Snapshots:   0 total
    Time:        0.315 s
    ```
    これで「グリーン」の状態になりました。しかし、現在の実装は明らかに不完全です。

### 2.3 [リファクタリング] 仕様を追加し、実装を改善する

TDDのサイクルの最後は「リファクタリング」です。テストを追加して隠れた要求をあぶり出し、コードをより汎用的に、より綺麗にしていきます。

1.  **新しいテストを追加して、再び[レッド]へ**
    現在の実装の問題点を暴く新しいテストを追加します。「同じ日付なら0を返すはずだ」という仕様をテストコードで表現しましょう。
    `src/date-calculator.test.ts`の`describe`ブロック内に、以下の`it`ブロックを追記します。
    ```typescript
    // src/date-calculator.test.ts (itブロックを追記)
    it('同じ日付なら0日を返すべきである', () => {
      const date = new Date('2023-03-15');
      expect(calculateDaysBetweenDates(date, date)).toBe(0);
    });
    ```
    
    このテストは現在の `return 4;` という実装では失敗し、再び「レッド」の状態に戻ります。

    ```bash
     FAIL  src/date-calculator.test.ts
      calculateDaysBetweenDates
        ✓ 2023年1月1日と2023年1月5日の間の日数を正しく計算すべきである (2 ms)
      ● calculateDaysBetweenDates › 同じ日付なら0日を返すべきである

        expect(received).toBe(expected) // Object.is equality

        Expected: 0
        Received: 4
    ```

2.  **実装を改善して、再び[グリーン]へ**
    両方のテストをパスさせるために、ハードコードされた実装を汎用的なロジックに修正（リファクタリング）します。

    ```typescript
    // src/date-calculator.ts (全体を修正)
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
    
    `npm test` を実行すると、すべてのテストがパスすることを確認できます。

3.  **さらにテストを追加して堅牢にする**
    最後に、「日付の順序が逆でも同じ結果を返すべき」という仕様もテストで保証しましょう。
    先ほどと同様に、テストファイルに`it`ブロックを追記します。
    ```typescript
    // src/date-calculator.test.ts (さらにitブロックを追記)
    it('日付の順序が逆でも同じ結果を返すべきである', () => {
      const date1 = new Date('2023-01-05');
      const date2 = new Date('2023-01-01');
      expect(calculateDaysBetweenDates(date1, date2)).toBe(4);
    });
    ```
    
    `Math.abs()` を使ったおかげで、このテストも最初からパスします。こうしてテストを追加していくことで、関数の振る舞いが明確になり、コードがより堅牢になっていくのです。テストコードは **準備(Arrange)・実行(Act)・検証(Assert)** の3ステップで構成すると、意図が明確になります。

## 第3章: テストの実行と呼吸

`npm test` コマンドでテストを実行します。

テストがすべて成功すると、`PASS src/date-calculator.test.ts` のように、全テストが成功したことを示す緑色のメッセージが表示されます。これが、あなたが安心して次の開発に進むための「青信号」です。
``` bash
npm test

> typescript-test-project@1.0.0 test
> jest

 PASS  src/date-calculator.test.ts
  calculateDaysBetweenDates
    ✓ 2023年1月1日と2023年1月5日の間の日数を正しく計算すべきである (1 ms)
    ✓ 同じ日付なら0日を返すべきである
    ✓ 日付の順序が逆でも同じ結果を返すべきである

Test Suites: 1 passed, 1 total
Tests:       3 passed, 3 total
Snapshots:   0 total
Time:        0.327 s
Ran all test suites.
```

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

テストを書くことは、単なる確認作業ではありません。それはコードの振る舞いを定義し、設計を導くための能動的な活動です。特にテスト駆動開発（TDD）では、「失敗するテストを先に書く」というサイクルを通じて、より堅牢で保守しやすいコードを生み出します。この章では、TDDのリズム（レッド→グリーン→リファクタリング）を体験しながら、テストが設計をどう変えるかを見ていきましょう。

### 4.1 [レッド→グリーン] テストで仕様を定義する：エラー処理

まず、仕様を明確にするための問いを立てます。「もし、無効な日付が渡されたらどうなるべきか？」現在の関数はこのケースを考慮していません。私たちは「不正な入力を受け付けず、エラーを投げる」という仕様をここで決定します。

#### 1. [レッド] 失敗するテストを先に書く

TDDの第一歩は、これから実装する機能に対する **失敗するテスト（レッド）** を書くことです。この新しい仕様を、まずはテストコードで表現しましょう。

```typescript
// src/date-calculator.test.ts に追記
it('無効な日付が渡された場合にエラーを投げるべきである', () => {
  const invalidDate = new Date('not a date');
  const validDate = new Date('2023-01-01');

  // toThrow: 関数が特定のエラーを投げることを検証する
  expect(() => calculateDaysBetweenDates(invalidDate, validDate)).toThrow('Invalid Date object provided.');
});
```

この時点で実装はまだないので、`npm test` を実行すると、期待通りテストは失敗します。

```bash
 FAIL  src/date-calculator.test.ts
  calculateDaysBetweenDates
    ✓ 2023年1月1日と2023年1月5日の間の日数を正しく計算すべきである (1 ms)
    ✓ 同じ日付なら0日を返すべきである
    ✓ 日付の順序が逆でも同じ結果を返すべきである
  ● calculateDaysBetweenDates › 無効な日付が渡された場合にエラーを投げるべきである

    expect(received).toThrow(expected)

    Expected message: "Invalid Date object provided."
    Received function did not throw
```

これが「レッド」の状態です。次に、このテストをパスさせましょう。

#### 2. [グリーン] テストをパスさせる最小限の実装を書く

次に、このテストを **成功させる（グリーン）** ための最小限のコードを実装します。

```typescript
// src/date-calculator.ts (冒頭部分を修正)
export function calculateDaysBetweenDates(date1: Date, date2: Date): number {
  if (!date1 || !date2 || isNaN(date1.getTime()) || isNaN(date2.getTime())) {
    throw new Error('Invalid Date object provided.');
  }
  // ... 以下、実装は同じ
```

再度 `npm test` を実行すると、今度はすべてのテストがパスするはずです。

```bash
 PASS  src/date-calculator.test.ts
  calculateDaysBetweenDates
    ✓ 2023年1月1日と2023年1月5日の間の日数を正しく計算すべきである (2 ms)
    ✓ 同じ日付なら0日を返すべきである
    ✓ 日付の順序が逆でも同じ結果を返すべきである
    ✓ 無効な日付が渡された場合にエラーを投げるべきである (5 ms)

Test Suites: 1 passed, 1 total
Tests:       4 passed, 4 total
Snapshots:   0 total
Time:        0.295 s, estimated 1 s
Ran all test suites.
```

これで、「エラーを投げること」がこの関数の正しい振る舞いであると、コードで定義することができました。この「レッド→グリーン」の短いサイクルが、TDDの基本的なリズムです。

### 4.2 テストが隠れた仕様を発見させる：閏年

TDDのサイクルは、エッジケースの発見も促します。日付計算には「閏年」という、見落としがちな仕様が常に存在します。

この仕様を保証するため、新たなテストケースを追加します。

```typescript
// src/date-calculator.test.ts
it('閏年をまたぐ計算が正しく行われるべきである', () => {
  const date1 = new Date('2024-02-28'); // 2024年は閏年
  const date2 = new Date('2024-03-01');
  expect(calculateDaysBetweenDates(date1, date2)).toBe(2);
});
```

私たちの `calculateDaysBetweenDates` 関数は幸いにも最初からこのテストをパスしますが、もしパスしなかった場合、先ほどと同じようにTDDのサイクル（レッド→グリーン）に従って実装を修正することになります。

このように **テストを書く行為そのものが、開発者に「どのようなケースを考慮すべきか？」を問いかけ、隠れた仕様をあぶり出す** のです。

すべてのテストがパスすることを確認しましょう。

```bash
npm test

> typescript-test-project@1.0.0 test
> jest

 PASS  src/date-calculator.test.ts
  calculateDaysBetweenDates
    ✓ 2023年1月1日と2023年1月5日の間の日数を正しく計算すべきである (1 ms)
    ✓ 同じ日付なら0日を返すべきである
    ✓ 日付の順序が逆でも同じ結果を返すべきである
    ✓ 無効な日付が渡された場合にエラーを投げるべきである (5 ms)
    ✓ 閏年をまたぐ計算が正しく行われるべきである

Test Suites: 1 passed, 1 total
Tests:       5 passed, 5 total
Snapshots:   0 total
Time:        0.283 s, estimated 1 s
Ran all test suites.
```

## おわりに

本記事では、TypeScriptとJestを使ったテストの基本を解説しました。

テストは、書く前から存在していた仕様を確認するものではなく、 **書く過程で仕様を発見し、定義するための対話的な道具** です。テストが一つ、また一つと増えるたびに、あなたのコードベースはより堅牢なものになっていきます。

さらに学びたい方は、外部依存を扱う「モック」や、UIコンポーネントのテスト手法についても調べてみることをお勧めします。

テストを書く習慣を身につけ、自信に満ちた開発ライフを送りましょう！
