using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Hangfire.Sample.Highlighter.Models
{
    public class HighlighterDbContext : DbContext
    {
        public HighlighterDbContext()
            : base("HighlighterDb")
        {
        }

        public DbSet<Snippet> Snippets { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}