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
	public DbSet<ADBotManager> BotManagers { get; set; }
	public DbSet<ADExchange> Exchanges { get; set; }
	public DbSet<ADExchangeProvider> ExchangeProviders { get; set; }
	public DbSet<ADNotification> Notifications { get; set; }
	public DbSet<ADOrder> Orders { get; set; }
	public DbSet<ADPosition> Positions { get; set; }
	public DbSet<ADUser> Users { get; set; }

	#endregion DbSets

	public AstroidDb(DbContextOptions<AstroidDb> options, IConfiguration config) : base(options)
	{
		var settings = config.Get<WebConfig>() ?? new();
		var connStringEnvVariable = Environment.GetEnvironmentVariable("ASTROID_DB_CONNECTION_STRING");
		var dbProviderEnvVariable = Environment.GetEnvironmentVariable("ASTROID_DB_PROVIDER");

		ConnectionString = connStringEnvVariable ?? settings.Database.ConnectionString!;
		Provider = dbProviderEnvVariable != null ? Enum.Parse<DatabaseProvider>(dbProviderEnvVariable) : settings.Database.DatabaseProvider;
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

