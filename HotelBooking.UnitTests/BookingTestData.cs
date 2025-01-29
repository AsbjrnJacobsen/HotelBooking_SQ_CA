using System;
using System.Collections;
using System.Collections.Generic;

namespace HotelBooking.UnitTests;

public class BookingTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { DateTime.Today.AddDays(2), DateTime.Today.AddDays(5), true }; // Available
        yield return new object[] { DateTime.Today.AddDays(2), DateTime.Today.AddDays(5), false };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}