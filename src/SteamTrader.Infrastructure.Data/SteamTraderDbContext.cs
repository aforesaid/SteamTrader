using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamTrader.Domain.Entities;
using SteamTrader.Domain.Repository;

namespace SteamTrader.Infrastructure.Data
{
    public class SteamTraderDbContext : DbContext, IUnitOfWork
    {
        public DbSet<TradeOfferEntity> TradeOffers { get; protected set; }
        public SteamTraderDbContext() : base() { }
        public SteamTraderDbContext(DbContextOptions options) : base(options) { }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TradeOfferEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<TradeOfferEntity>().HasIndex(x => x.DateTime);
            modelBuilder.Entity<TradeOfferEntity>().HasIndex(x => x.From);
            modelBuilder.Entity<TradeOfferEntity>().HasIndex(x => x.To);
            modelBuilder.Entity<TradeOfferEntity>().HasIndex(x => x.Margin);
            
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
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
