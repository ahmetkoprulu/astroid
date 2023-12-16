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
		public TestProvider(ExecutionRepository repo, ExchangeInfoStore store, ExchangeCalculator calculator, MetadataMapper mapper) : base(repo, store, calculator, mapper)
		{

		}

		public override Task ChangeLeverage(string ticker, int leverage) => throw new NotImplementedException();
		public override Task ChangeMarginType(string ticker, MarginType marginType, AMProviderResult result) => throw new NotImplementedException();
		public override Task<decimal> GetBalance(string asset) => throw new NotImplementedException();
		public override Task<AMOrderBook> GetOrderBook(AMOrderBook orderBook, string ticker) => throw new NotImplementedException();
		public override Task<IEnumerable<AMExchangePosition>> GetPositions() => throw new NotImplementedException();
		public override Task<AMOrderResult> PlaceDeviatedOrder(string ticker, decimal quantity, decimal price, OrderType oType, PositionType pType) => throw new NotImplementedException();
		public override Task<AMOrderResult> PlaceMarketOrder(string ticker, decimal quantity, OrderType oType, PositionType pType, bool reduceOnly = false) => throw new NotImplementedException();
		public override Task<AMOrderResult> PlaceOboOrder(AMOrderBook orderBook, string ticker, decimal quantity, OrderType oType, PositionType pType, LimitSettings settings) => throw new NotImplementedException();
		public override Task<AMOrderResult> PlaceOrderTillPositionFilled(AMOrderBook orderBook, string ticker, decimal quantity, OrderType oType, PositionType pType, LimitSettings settings) => throw new NotImplementedException();
	}
}
