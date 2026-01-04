using Iepan_Flaviu_Lab4.Models.History;
using Microsoft.EntityFrameworkCore;

namespace Iepan_Flaviu_Lab4.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
 : base(options)
        {
        }
        public DbSet<PredictionHistory> PredictionHistories { get; set; }
    }
}
