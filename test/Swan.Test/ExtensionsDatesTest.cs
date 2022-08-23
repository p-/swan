﻿namespace Swan.Test;

using NUnit.Framework;
using Swan.Extensions;
using System;
using System.Collections.Generic;

[TestFixture]
public class ToSortableDate
{
    [TestCase("2016-01-01", "00:00:00", 2016, 1, 1, 0, 0, 0)]
    [TestCase("2016-10-10", "10:10:10", 2016, 10, 10, 10, 10, 10)]
    public void ExtensionsDates_ReturnsEquals(
        string expectedDate,
        string expectedTime,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        var input = new DateTime(year, month, day, hour, minute, second);
        Assert.AreEqual(expectedDate, input.ToSortableDate());
        Assert.AreEqual($"{expectedDate} {expectedTime}", input.ToSortableDateTime());

        Assert.AreEqual(input, $"{expectedDate} {expectedTime}".ToDateTime());
    }
}

[TestFixture]
public class ToDateTime
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void InvalidArguments_ThrowsArgumentNullException(string date) => Assert.Throws<ArgumentNullException>(() => date.ToDateTime());

    [TestCase("2017 10 26")]
    [TestCase("2017-10")]
    [TestCase("2017-10-26 15:35")]
    public void DatesNotParsable_ThrowsException(string date) => Assert.Throws<ArgumentException>(() => date.ToDateTime());
}

[TestFixture]
public class DateRange
{
    [Test]
    public void GivingTwoDates_ReturnsEqualSequenceRangeOfDates()
    {
        var startDate = new DateTime(2017, 1, 1);
        var endDate = new DateTime(2017, 1, 3);

        var rangeActual = startDate.DateRange(endDate);

        var rangeExpected = new List<DateTime>
        {
            new(2017, 1, 1, 0, 0, 0),
            new(2017, 1, 2, 0, 0, 0),
            new(2017, 1, 3, 0, 0, 0),
        };

        CollectionAssert.AreEqual(rangeExpected, rangeActual);
    }
}

[TestFixture]
public class ToUnixEpochDate
{
    [Test]
    public void GivingADate_ConvertItIntoTicks()
    {
        var date = new DateTime(2017, 10, 27).ToUniversalTime().Date;

        Assert.AreEqual(1509062400, date.ToUnixTimeSeconds());
    }
}

[TestFixture]
public class CompareDates
{
    private readonly DateTime _date = new(2002, 7, 3, 12, 0, 0, 200);

    [Test]
    public void WithFullDateTimes_ReturnsDateTimeSpan()
    {
        var result = _date.GetDateTimeSpan(new(1969, 8, 15, 5, 7, 10, 100));

        Assert.That(result.Years, Is.EqualTo(32));
        Assert.That(result.Months, Is.EqualTo(10));
        Assert.That(result.Days, Is.EqualTo(18));
        Assert.That(result.Hours, Is.EqualTo(6));
        Assert.That(result.Minutes, Is.EqualTo(52));
        Assert.That(result.Seconds, Is.EqualTo(50));
        Assert.That(result.Milliseconds, Is.EqualTo(100));
    }

    [Test]
    public void WithPartialDateTimes_ReturnsDateTimeSpan()
    {
        var result = new DateTime(1969, 8, 15).GetDateTimeSpan(_date);

        Assert.AreEqual(result.Years, 32);
        Assert.AreEqual(result.Months, 10);
        Assert.AreEqual(result.Days, 18);
        Assert.AreEqual(result.Minutes, 0);
        Assert.AreEqual(result.Seconds, 0);
        Assert.AreEqual(result.Milliseconds, 200);
    }
}
