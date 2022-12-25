using Gcpe.Hub.BusinessInsights.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gcpe.Hub.BusinessInsights.API.DbContexts
{
    public class LocalDbContext : DbContext
    {
        public DbSet<NewsReleaseItem> NewsReleaseItems { get; set; }
        public DbSet<TranslationItem> TranslationItems { get; set; }
        public DbSet<Url> Urls { get; set; }

        public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options) { }
    }
}
