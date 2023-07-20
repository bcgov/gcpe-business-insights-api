using Gcpe.Hub.BusinessInsights.API.Entities;
using Microsoft.EntityFrameworkCore;
using Url = Gcpe.Hub.BusinessInsights.API.Models.Url;

namespace Gcpe.Hub.BusinessInsights.API.DbContexts
{
    public class LocalBusinessInsightsDbContext : DbContext
    {
        public LocalBusinessInsightsDbContext(DbContextOptions options) : base(options) { }

        public DbSet<NewsReleaseItem> NewsReleaseItems { get; set; }
        public DbSet<Url> Urls { get; set; }
    }
}
