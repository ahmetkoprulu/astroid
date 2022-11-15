using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Astroid.Entity
{
	public class AstroidDb : DbContext
	{
		public static DatabaseProvider DefaultProvider { get; set; }

		#region DbSets

		#endregion DbSets

		private AstroidDb() { }

		public AstroidDb(DbContextOptions<AstroidDb> options) : base(options) { }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				var connectionString = "";
				switch (DefaultProvider)
				{
					case DatabaseProvider.PostgreSql:
						optionsBuilder
							.UseNpgsql(connectionString, options =>
							{
								options.CommandTimeout(30);
							});
						AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
						AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
						break;
					case DatabaseProvider.MySql:
					case DatabaseProvider.Unknown:
					default:
						throw new ArgumentOutOfRangeException(nameof(DefaultProvider));
						throw new ArgumentOutOfRangeException(nameof(DefaultProvider));
				}
			}

			base.OnConfiguring(optionsBuilder);
		}
	}
}
