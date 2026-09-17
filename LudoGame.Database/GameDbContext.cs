using Microsoft.EntityFrameworkCore;
using LudoGame.Database.Entities;

namespace LudoGame.Database
{
    public class GameDbContext : DbContext
    {
        public DbSet<GameStateRecord> GameStates { get; set; } = null!;

        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
        {
        }
        
        // Parameterless constructor for EF Design tools (migrations)
        public GameDbContext() 
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=ludo_game.db");
            }
        }
    }
}
