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

public class ExchangeProviderBaseTests
{
	private readonly Mock<AstroidDb> _db;
	private readonly Mock<ExchangeInfoStore> _store;

	public ExchangeProviderBaseTests()
	{
		_db = new Mock<AstroidDb>(new DbContextOptions<AstroidDb>(), new Mock<IConfiguration>().Object);
		_store = new Mock<ExchangeInfoStore>(new Mock<ICacheService>().Object);
	}

	private class TestProvider : ExchangeProviderBase
	{
		public TestProvider(AstroidDb db, ExchangeInfoStore store, ExchangeCalculator calculator) : base(db, store, calculator)
		{

		}

		public override Task<AMProviderResult> ChangeTickersMarginType(List<string> tickers, MarginType type) => throw new NotImplementedException();

		public override Task<AMProviderResult> ExecuteOrder(ADBot bot, AMOrderRequest order) => throw new NotImplementedException();
	}
}
