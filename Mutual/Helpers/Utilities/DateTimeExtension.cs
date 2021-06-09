using System;
using System.Threading;

namespace Mutual.Helpers.Utilities
{
	public static class DateTimeExtensions
	{
		//// Последнее время дня
		//public static DateTime EndOfDay(this DateTime date)
		//{
		//    return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 999, date.Kind);
		//}
		//// Начало дня
		//public static DateTime BeginningOfDay(this DateTime date)
		//{
		//    return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0, date.Kind);
		//}


		///// <summary>
		///// Returns the same date (same Day, Month, Hour, Minute, Second etc) in the next calendar year.
		///// If that day does not exist in next year in same month, number of missing days is added to the last day in same month next year.
		///// </summary>
		//public static DateTime NextYear(this DateTime start)
		//{
		//    var nextYear = start.Year + 1;

		//    var numberOfDaysInSameMonthNextYear = DateTime.DaysInMonth(nextYear, start.Month);

		//    if (numberOfDaysInSameMonthNextYear < start.Day)
		//    {
		//        var differenceInDays = start.Day - numberOfDaysInSameMonthNextYear;
		//        var dateTime = new DateTime(nextYear, start.Month, numberOfDaysInSameMonthNextYear, start.Hour, start.Minute, start.Second, start.Millisecond, start.Kind);
		//        return dateTime + differenceInDays.Days();
		//    }
		//    return new DateTime(nextYear, start.Month, start.Day, start.Hour, start.Minute, start.Second, start.Millisecond, start.Kind);
		//}

		///// <summary>
		///// Returns the same date (same Day, Month, Hour, Minute, Second etc) in the previous calendar year.
		///// If that day does not exist in previous year in same month, number of missing days is added to the last day in same month previous year.
		///// </summary>
		//public static DateTime PreviousYear(this DateTime start)
		//{
		//    var previousYear = start.Year - 1;
		//    var numberOfDaysInSameMonthPreviousYear = DateTime.DaysInMonth(previousYear, start.Month);

		//    if (numberOfDaysInSameMonthPreviousYear < start.Day)
		//    {
		//        var differenceInDays = start.Day - numberOfDaysInSameMonthPreviousYear;
		//        var dateTime = new DateTime(previousYear, start.Month, numberOfDaysInSameMonthPreviousYear, start.Hour, start.Minute, start.Second, start.Millisecond, start.Kind);
		//        return dateTime + differenceInDays.Days();
		//    }
		//    return new DateTime(previousYear, start.Month, start.Day, start.Hour, start.Minute, start.Second, start.Millisecond, start.Kind);
		//}

		public static DateTime FirstDayOfQuarter(this DateTime current)
		{
			var currentQuarter = (current.Month - 1) / 3 + 1;
			return new DateTime(current.Year, 3 * currentQuarter - 2, 1);
		}

		public static DateTime LastDayOfQuarter(this DateTime current)
		{
			return FirstDayOfQuarter(current).AddMonths(3).AddDays(-1);
		}

		public static DateTime FirstDayOfMonth(this DateTime current)
		{
			return current.AddDays(-current.Day + 1);
		}

		public static DateTime LastDayOfMonth(this DateTime current)
		{
			return FirstDayOfMonth(current).AddMonths(1).AddDays(-1);
		}

		public static DateTime FirstDayOfYear(this DateTime current)
		{
			return new DateTime(current.Year, 1, 1);
		}

		public static DateTime LastDayOfYear(this DateTime current)
		{
			return new DateTime(current.Year, 12, 31);
		}
	}
}