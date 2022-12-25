using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.Data.Entity;
using Microsoft.EntityFrameworkCore;

namespace Gcpe.Hub.BusinessInsights.API.DbContexts
{
    public class HubBusinessInsightsDbContext : HubDbContext
    {
        public HubBusinessInsightsDbContext(DbContextOptions<HubDbContext> options) : base(options) { }

        public DbSet<NewsReleaseEntity> NewsReleaseEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NewsReleaseEntity>().HasNoKey();

            base.OnModelCreating(modelBuilder);
        }
    }
}
