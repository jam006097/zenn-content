using System;

namespace MyFirstTest
{
    /// <summary>
    /// 営業日を計算するクラスです
    /// </summary>
    public class BusinessDayCalculator
    {
        /// <summary>
        /// 指定した期間内の営業日数を計算するメソッドです
        /// </summary>
        /// <param name="startDate">計算開始日</param>
        /// <param name="endDate">計算終了日</param>
        /// <returns>営業日の数</returns>
        public int CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            // 開始日が終了日より後ろの場合はエラーなので0を返す
            if (startDate > endDate)
            {
                return 0;
            }

            // 営業日をカウントする変数を初期化（0から始める）
            int businessDays = 0;

            // startDateからendDateまで1日ずつループして確認していく
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // DayOfWeekプロパティで曜日を判定
                // 土曜日（Saturday）と日曜日（Sunday）以外の日を営業日とする
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    // 営業日だったのでカウントを1増やす
                    businessDays++;
                }
            }

            // 計算結果の営業日数を返す
            return businessDays;
        }
    }
}