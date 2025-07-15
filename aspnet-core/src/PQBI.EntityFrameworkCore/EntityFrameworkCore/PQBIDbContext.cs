using PQBI.Groups;
using PQBI.TrendWidgetConfigurations;
using PQBI.DefaultValues;
using PQBI.BarChartWidgetConfigurations;
using PQBI.TableWidgetConfigurations;
using PQBI.DashboardCustomization;
using PQBI.CustomParameters;
using System.Collections.Generic;
using System.Text.Json;
using Abp.Zero.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PQBI.Authorization.Delegation;
using PQBI.Authorization.Roles;
using PQBI.Authorization.Users;
using PQBI.Chat;
using PQBI.Configuration;
using PQBI.Editions;
using PQBI.ExtraProperties;
using PQBI.Friendships;
using PQBI.MultiTenancy;
using PQBI.MultiTenancy.Accounting;
using PQBI.MultiTenancy.Payments;
using PQBI.OpenIddict.Applications;
using PQBI.OpenIddict.Authorizations;
using PQBI.OpenIddict.Scopes;
using PQBI.OpenIddict.Tokens;
using PQBI.Storage;
using Microsoft.Extensions.Options;
using Abp.Localization;

namespace PQBI.EntityFrameworkCore
{
    public class PQBIDbContext : AbpZeroDbContext<Tenant, Role, User, PQBIDbContext>, IOpenIddictDbContext
    {
        public virtual DbSet<Group> Groups { get; set; }

        public virtual DbSet<TrendWidgetConfiguration> TrendWidgetConfigurations { get; set; }

        public virtual DbSet<DefaultValue> DefaultValues { get; set; }

        public virtual DbSet<BarChartWidgetConfiguration> BarChartWidgetConfigurations { get; set; }

        public virtual DbSet<TableWidgetConfiguration> TableWidgetConfigurations { get; set; }

        public virtual DbSet<WidgetConfiguration> WidgetConfigurations { get; set; }

        public virtual DbSet<CustomParameter> CustomParameters { get; set; }

        private readonly PqbiConfig _config;

        /* Define an IDbSet for each entity of the application */

        public virtual DbSet<OpenIddictApplication> Applications { get; }

        public virtual DbSet<OpenIddictAuthorization> Authorizations { get; }

        public virtual DbSet<OpenIddictScope> Scopes { get; }

        public virtual DbSet<OpenIddictToken> Tokens { get; }

        public virtual DbSet<BinaryObject> BinaryObjects { get; set; }

        public virtual DbSet<Friendship> Friendships { get; set; }

        public virtual DbSet<ChatMessage> ChatMessages { get; set; }

        public virtual DbSet<SubscribableEdition> SubscribableEditions { get; set; }

        public virtual DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }

        public virtual DbSet<SubscriptionPaymentProduct> SubscriptionPaymentProducts { get; set; }

        public virtual DbSet<Invoice> Invoices { get; set; }

        public virtual DbSet<UserDelegation> UserDelegations { get; set; }

        public virtual DbSet<RecentPassword> RecentPasswords { get; set; }

        public PQBIDbContext(DbContextOptions<PQBIDbContext> options, IOptions<PqbiConfig> config)
            : base(options)
        {
            _config = config.Value;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomParameter>(c =>
            {
                c.HasIndex(e => new { e.TenantId });
            });
            modelBuilder.Entity<TrendWidgetConfiguration>(t =>
                       {
                           t.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<CustomParameter>(c =>
                       {
                           c.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<DefaultValue>(d =>
                       {
                           d.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<CustomParameter>(c =>
                                  {
                                      c.HasIndex(e => new { e.TenantId });
                                  });
            modelBuilder.Entity<BarChartWidgetConfiguration>(b =>
                       {
                           b.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<TableWidgetConfiguration>(t =>
                       {
                           t.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<BarChartWidgetConfiguration>(b =>
                       {
                           b.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<TableWidgetConfiguration>(t =>
                       {
                           t.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<CustomParameter>(c =>
                       {
                           c.HasIndex(e => new { e.TenantId });
                       });
            modelBuilder.Entity<BinaryObject>(b => { b.HasIndex(e => new { e.TenantId }); });

            modelBuilder.Entity<SubscriptionPayment>(x =>
            {
                x.Property(u => u.ExtraProperties)
                    .HasConversion(
                        d => JsonSerializer.Serialize(d, new JsonSerializerOptions()
                        {
                            WriteIndented = false
                        }),
                        s => JsonSerializer.Deserialize<ExtraPropertyDictionary>(s, new JsonSerializerOptions()
                        {
                            WriteIndented = false
                        })
                    );
            });

            modelBuilder.Entity<SubscriptionPaymentProduct>(x =>
            {
                x.Property(u => u.ExtraProperties)
                    .HasConversion(
                        d => JsonSerializer.Serialize(d, new JsonSerializerOptions()
                        {
                            WriteIndented = false
                        }),
                        s => JsonSerializer.Deserialize<ExtraPropertyDictionary>(s, new JsonSerializerOptions()
                        {
                            WriteIndented = false
                        })
                    );
            });

            modelBuilder.Entity<ChatMessage>(b =>
            {
                b.HasIndex(e => new { e.TenantId, e.UserId, e.ReadState });
                b.HasIndex(e => new { e.TenantId, e.TargetUserId, e.ReadState });
                b.HasIndex(e => new { e.TargetTenantId, e.TargetUserId, e.ReadState });
                b.HasIndex(e => new { e.TargetTenantId, e.UserId, e.ReadState });
            });

            modelBuilder.Entity<Friendship>(b =>
            {
                b.HasIndex(e => new { e.TenantId, e.UserId });
                b.HasIndex(e => new { e.TenantId, e.FriendUserId });
                b.HasIndex(e => new { e.FriendTenantId, e.UserId });
                b.HasIndex(e => new { e.FriendTenantId, e.FriendUserId });
            });

            modelBuilder.Entity<Tenant>(b =>
            {
                b.HasIndex(e => new { e.SubscriptionEndDateUtc });
                b.HasIndex(e => new { e.CreationTime });
            });

            modelBuilder.Entity<SubscriptionPayment>(b =>
            {
                b.HasIndex(e => new { e.Status, e.CreationTime });
                b.HasIndex(e => new { PaymentId = e.ExternalPaymentId, e.Gateway });
            });

            modelBuilder.Entity<UserDelegation>(b =>
            {
                b.HasIndex(e => new { e.TenantId, e.SourceUserId });
                b.HasIndex(e => new { e.TenantId, e.TargetUserId });
            });

            if (_config.MultiTenancyEnabled == true)
            {
                modelBuilder.Entity<SubscriptionPaymentExtensionData>(b =>
                {
                    b.HasQueryFilter(m => !m.IsDeleted)
                        .HasIndex(e => new { e.SubscriptionPaymentId, e.Key, e.IsDeleted })
                        .IsUnique()
                        .HasFilter("\"IsDeleted\" = FALSE"); // PostgreSQL dialect. For SQL-Server use .HasFilter("[IsDeleted] = 0");
                });

                modelBuilder.Entity<ApplicationLanguageText>()
                  .Property(p => p.Value)
                  .HasMaxLength(100); // any integer that is smaller than 10485760

                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    foreach (var property in entityType.GetProperties())
                    {
                        // max char length value in sqlserver
                        if (property.GetMaxLength() == 67108864)
                            // max char length value in postgresql
                            property.SetMaxLength(10485760);
                    }
                }
            }
            else
            {
                modelBuilder.Entity<SubscriptionPaymentExtensionData>(b =>
                {
                    b.HasQueryFilter(m => !m.IsDeleted)
                        .HasIndex(e => new { e.SubscriptionPaymentId, e.Key, e.IsDeleted })
                        .IsUnique()
                        .HasFilter("[IsDeleted] = 0");
                });
            }

            //modelBuilder.ConfigurePersistedGrantEntity();
            modelBuilder.ConfigureOpenIddict();
        }
    }
}