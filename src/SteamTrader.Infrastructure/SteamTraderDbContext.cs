using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamTrader.Domain.Repository;

namespace SteamTrader.Infrastructure
{
    public class SteamTraderDbContext : DbContext, ISteamTraderRepo, IUnitOfWork
    {
        public SteamTraderDbContext() : base() { }
        public SteamTraderDbContext(DbContextOptions options) : base(options) { }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("User ID=postgres;Password=root;Server=localhost;Port=5432;Database=sell_spasibo;Integrated Security=true;");
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
