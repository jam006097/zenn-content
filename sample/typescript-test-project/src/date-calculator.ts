// src/date-calculator.ts

export function calculateDaysBetweenDates(date1: Date, date2: Date): number {
  // NOTE: この関数はシンプルさのため、Dateオブジェクトを直接扱っています。
  // より複雑な日付操作が必要なドメインでは、date-fnsのようなライブラリで抽象化する設計も有効です。

  if (!date1 || !date2 || isNaN(date1.getTime()) || isNaN(date2.getTime())) {
    throw new Error('Invalid Date object provided.');
  }

  const ONE_DAY_IN_MILLISECONDS = 1000 * 60 * 60 * 24;

  const utcDate1 = Date.UTC(date1.getFullYear(), date1.getMonth(), date1.getDate());
  const utcDate2 = Date.UTC(date2.getFullYear(), date2.getMonth(), date2.getDate());

  const diffMilliseconds = Math.abs(utcDate1 - utcDate2);
  return Math.floor(diffMilliseconds / ONE_DAY_IN_MILLISECONDS);
}
