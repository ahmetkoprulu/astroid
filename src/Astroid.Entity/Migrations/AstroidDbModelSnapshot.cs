﻿// <auto-generated />
using System;
using Astroid.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Astroid.Entity.Migrations
{
    [DbContext(typeof(AstroidDb))]
    partial class AstroidDbModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Astroid.Entity.ADAudit", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ActorId")
                        .HasColumnType("uuid");

                    b.Property<string>("CorrelationId")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Data")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("TargetId")
                        .HasColumnType("uuid");

                    b.Property<short>("Type")
                        .HasColumnType("smallint");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.ToTable("Audits");
                });

            modelBuilder.Entity("Astroid.Entity.ADBot", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<Guid>("ExchangeId")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsNotificationEnabled")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsPositionSizeExpandable")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsPyramidingEnabled")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsRemoved")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsStopLossEnabled")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsTakePofitEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Leverage")
                        .HasColumnType("integer");

                    b.Property<string>("LimitSettingsJson")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("LimitSettings");

                    b.Property<Guid?>("ManagedBy")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("ModifiedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<short>("OrderMode")
                        .HasColumnType("smallint");

                    b.Property<short>("OrderType")
                        .HasColumnType("smallint");

                    b.Property<decimal?>("PositionSize")
                        .HasColumnType("numeric");

                    b.Property<short>("PositionSizeType")
                        .HasColumnType("smallint");

                    b.Property<string>("PyramidingSettingsJson")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("PyramidingSettings");

                    b.Property<short>("State")
                        .HasColumnType("smallint");

                    b.Property<string>("StopLossSettingsJson")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("StopLossSettings");

                    b.Property<string>("TakeProfitSettingsJson")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("TakeProfitSettings");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ExchangeId");

                    b.ToTable("Bots");
                });

            modelBuilder.Entity("Astroid.Entity.ADBotManager", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid>("Key")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("PingDate")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.ToTable("BotManagers");
                });

            modelBuilder.Entity("Astroid.Entity.ADExchange", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<bool>("IsRemoved")
                        .HasColumnType("boolean");

                    b.Property<string>("Label")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("ModifiedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("PropertiesJson")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("Properties");

                    b.Property<Guid>("ProviderId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ProviderId");

                    b.ToTable("Exchanges");
                });

            modelBuilder.Entity("Astroid.Entity.ADExchangeProvider", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TargetType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ExchangeProviders");
                });

            modelBuilder.Entity("Astroid.Entity.ADNotification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<short>("Channel")
                        .HasColumnType("smallint");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Error")
                        .HasColumnType("text");

                    b.Property<DateTime>("ExpireDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime>("SentDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<short>("Status")
                        .HasColumnType("smallint");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("To")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("Astroid.Entity.ADOrder", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("BotId")
                        .HasColumnType("uuid");

                    b.Property<bool>("ClosePosition")
                        .HasColumnType("boolean");

                    b.Property<short>("ConditionType")
                        .HasColumnType("smallint");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid>("ExchangeId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("FilledPrice")
                        .HasColumnType("numeric");

                    b.Property<decimal>("FilledQuantity")
                        .HasColumnType("numeric");

                    b.Property<int?>("Leverage")
                        .HasColumnType("integer");

                    b.Property<Guid>("PositionId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("numeric");

                    b.Property<short>("QuantityType")
                        .HasColumnType("smallint");

                    b.Property<Guid?>("RelatedTo")
                        .HasColumnType("uuid");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("TriggerPrice")
                        .HasColumnType("numeric");

                    b.Property<int>("TriggerType")
                        .HasColumnType("integer");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("BotId");

                    b.HasIndex("ExchangeId");

                    b.HasIndex("PositionId");

                    b.HasIndex("UserId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("Astroid.Entity.ADPosition", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("AvgEntryPrice")
                        .HasColumnType("numeric");

                    b.Property<Guid>("BotId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("CurrentQuantity")
                        .HasColumnType("numeric");

                    b.Property<decimal>("EntryPrice")
                        .HasColumnType("numeric");

                    b.Property<Guid>("ExchangeId")
                        .HasColumnType("uuid");

                    b.Property<int>("Leverage")
                        .HasColumnType("integer");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("numeric");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<short>("Type")
                        .HasColumnType("smallint");

                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("BotId");

                    b.HasIndex("ExchangeId");

                    b.HasIndex("UserId");

                    b.ToTable("Positions");
                });

            modelBuilder.Entity("Astroid.Entity.ADUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<short>("ChannelPreference")
                        .HasColumnType("smallint");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsRemoved")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Phone")
                        .HasColumnType("text");

                    b.Property<string>("TelegramId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Astroid.Entity.ADBot", b =>
                {
                    b.HasOne("Astroid.Entity.ADExchange", "Exchange")
                        .WithMany()
                        .HasForeignKey("ExchangeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Exchange");
                });

            modelBuilder.Entity("Astroid.Entity.ADExchange", b =>
                {
                    b.HasOne("Astroid.Entity.ADExchangeProvider", "Provider")
                        .WithMany()
                        .HasForeignKey("ProviderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Provider");
                });

            modelBuilder.Entity("Astroid.Entity.ADNotification", b =>
                {
                    b.HasOne("Astroid.Entity.ADUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Astroid.Entity.ADOrder", b =>
                {
                    b.HasOne("Astroid.Entity.ADBot", "Bot")
                        .WithMany()
                        .HasForeignKey("BotId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Astroid.Entity.ADExchange", "Exchange")
                        .WithMany()
                        .HasForeignKey("ExchangeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Astroid.Entity.ADPosition", "Position")
                        .WithMany("Orders")
                        .HasForeignKey("PositionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Astroid.Entity.ADUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Bot");

                    b.Navigation("Exchange");

                    b.Navigation("Position");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Astroid.Entity.ADPosition", b =>
                {
                    b.HasOne("Astroid.Entity.ADBot", "Bot")
                        .WithMany()
                        .HasForeignKey("BotId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Astroid.Entity.ADExchange", "Exchange")
                        .WithMany()
                        .HasForeignKey("ExchangeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Astroid.Entity.ADUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Bot");

                    b.Navigation("Exchange");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Astroid.Entity.ADPosition", b =>
                {
                    b.Navigation("Orders");
                });
#pragma warning restore 612, 618
        }
    }
}
