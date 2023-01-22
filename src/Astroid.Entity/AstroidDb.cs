using Astroid.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.EntityFrameworkCore.Extensions;

namespace Astroid.Entity;

public class AstroidDb : DbContext
{
	private DatabaseProvider Provider { get; set; }
	private string ConnectionString { get; set; }

	#region DbSets

	public DbSet<ADAudit> Audits { get; set; }
	public DbSet<ADBot> Bots { get; set; }
	public DbSet<ADUser> Users { get; set; }
	public DbSet<ADExchange> Exchanges { get; set; }
	public DbSet<ADExchangeProvider> ExchangeProviders { get; set; }

	#endregion DbSets

	public AstroidDb(DbContextOptions<AstroidDb> options, IConfiguration config) : base(options)
	{
		var settings = config.Get<AConfAppSettings>() ?? new();

		ConnectionString = settings.Database.ConnectionString!;
		Provider = settings.Database.DatabaseProvider;
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{
			switch (Provider)
			{
				case DatabaseProvider.PostgreSql:
					optionsBuilder.UseNpgsql(ConnectionString, options => { options.CommandTimeout(30); });
					AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
					AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
					break;
				case DatabaseProvider.MySql:
					optionsBuilder.UseMySQL(ConnectionString, options => options.CommandTimeout(30));
					break;
				case DatabaseProvider.Unknown:
				default:
					throw new InvalidDataException(nameof(Provider));
			}
		}

		base.OnConfiguring(optionsBuilder);
	}
}

