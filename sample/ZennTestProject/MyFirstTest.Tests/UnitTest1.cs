using System;
using Xunit;

// このクラスは、BusinessDayCalculator クラスの動作が正しいかどうかを確認するためのテストクラスです
namespace MyFirstTest.Tests
{
    /// <summary>
    /// ビジネスデー（営業日）を計算するロジックのテストを行うクラス
    /// [Fact]属性が付いたメソッドは、xunitによって自動的にテストとして実行されます
    /// </summary>
    public class BusinessDayCalculatorTests
    {
        [Fact]
        public void 営業日数を計算する場合_同じ週の期間を指定すると正しい日数を返すべき()
        {
            // Arrange（準備）: テストに必要なデータを用意する
            var calculator = new BusinessDayCalculator(); // テスト対象のクラスをインスタンス化
            var startDate = new DateTime(2026, 1, 19); // 開始日：2026年1月19日（月曜日）
            var endDate = new DateTime(2026, 1, 23);   // 終了日：2026年1月23日（金曜日）
            int expected = 5; // 期待される営業日数：5日

            // Act（実行）: テスト対象のメソッドを実行する
            int actual = calculator.CalculateBusinessDays(startDate, endDate);

            // Assert（検証）: 実行結果が期待値と一致するかを確認する
            Assert.Equal(expected, actual); // 期待値と実際の結果が同じかチェック
        }

        [Fact]
        public void 営業日数を計算する場合_週末を含む期間を指定すると土日が除外されるべき()
        {
            // Arrange（準備）: テストに必要なデータを用意する
            var calculator = new BusinessDayCalculator(); // テスト対象のクラスをインスタンス化
            var startDate = new DateTime(2026, 1, 19); // 開始日：2026年1月19日（月曜日）
            var endDate = new DateTime(2026, 1, 25);   // 終了日：2026年1月25日（日曜日）
            int expected = 5; // 期待される営業日数：5日（土日は除外される）

            // Act（実行）: テスト対象のメソッドを実行する
            int actual = calculator.CalculateBusinessDays(startDate, endDate);

            // Assert（検証）: 実行結果が期待値と一致するかを確認する
            Assert.Equal(expected, actual); // 期待値と実際の結果が同じかチェック
        }
    }
}