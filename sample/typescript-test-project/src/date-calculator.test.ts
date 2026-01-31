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

  it('無効な日付が渡された場合にエラーを投げるべきである', () => {
    const invalidDate = new Date('not a date');
    const validDate = new Date('2023-01-01');

    // toThrow: 関数が特定のエラーを投げることを検証する
    expect(() => calculateDaysBetweenDates(invalidDate, validDate)).toThrow('Invalid Date object provided.');
  });

  it('閏年をまたぐ計算が正しく行われるべきである', () => {
    const date1 = new Date('2024-02-28'); // 2024年は閏年
    const date2 = new Date('2024-03-01');
    expect(calculateDaysBetweenDates(date1, date2)).toBe(2);
  });
});
