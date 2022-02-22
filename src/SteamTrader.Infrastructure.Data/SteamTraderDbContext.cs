using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamTrader.Domain.Entities;
using SteamTrader.Domain.Repository;
using SteamTrader.Domain.Steam.ItemNames;

namespace SteamTrader.Infrastructure.Data
{
    public class SteamTraderDbContext : DbContext
    {
        public DbSet<TradeOfferEntity> TradeOffers { get; protected set; }
        public DbSet<TradeTaskEntity> TradeTask { get; protected set; }

        public DbSet<SteamItemNameEntity> SteamItemNames { get; protected set; }
        public SteamTraderDbContext() : base() { }
        public SteamTraderDbContext(DbContextOptions options) : base(options) { }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TradeOfferEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<TradeOfferEntity>().HasIndex(x => x.DateTime);
            modelBuilder.Entity<TradeOfferEntity>().HasIndex(x => x.From);
            modelBuilder.Entity<TradeOfferEntity>().HasIndex(x => x.To);
            modelBuilder.Entity<TradeOfferEntity>().HasIndex(x => x.Margin);

            modelBuilder.Entity<SteamItemNameEntity>().HasIndex(x => x.ItemId);
            modelBuilder.Entity<SteamItemNameEntity>().HasIndex(x => x.MarketplaceHashName);

            modelBuilder.Entity<TradeTaskEntity>().HasIndex(x => x.TradeStage);

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("User ID=postgres;Password=root;Server=localhost;Port=5432;Database=steamtrader;Integrated Security=true;");
            }
            base.OnConfiguring(optionsBuilder);
        }

        #region Sealed
        public override int SaveChanges()
        {
            throw new NotSupportedException();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
