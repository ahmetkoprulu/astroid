using Xunit;
using FluentAssertions;
using Moq;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Astroid.Core.Cache;

namespace Astroid.Providers.Base.Tests;

public class ExchangeCalculatorTests
{
	public ExchangeCalculatorTests() { }

	[Theory]
	[MemberData(nameof(GetStandartDeviasionTestData))]
	public void ComputeStandardDeviation_ReturnsCorrectValue(decimal expected, params decimal[] values)
	{
		var calculator = new ExchangeCalculator();

		var actual = calculator.CalculateStandardDeviation(values);

		Math.Round(actual, 3).Should().Be(Math.Round(expected, 3)); // Round to 3 decimal places
	}

	public static IEnumerable<object[]> GetStandartDeviasionTestData()
	{
		yield return new object[] { 1.708, new decimal[] { 1, 2, 3, 4, 5, 6 } };
		// Add more test data as needed
	}
}
