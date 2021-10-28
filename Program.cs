using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace efcore_extensions_bulk_savechanges_concurrencytoken_poc
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await using var context = new MyDbContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            var session = new Session
            {
                Description = "foobar",
                ModifiedOn = DateTimeOffset.UtcNow
            };

            context.Set<Session>().Add(session);
            await context.SaveChangesAsync();

            session.AddEntry(new Entry());

            // 'Unable to cast object of type 'System.DateTime' to type 'System.DateTimeOffset'.
            await context.BulkSaveChangesAsync();
        }
    }

    public class Session
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
        public IList<Entry> Entries { get; set; } = new List<Entry>();

        public void AddEntry(Entry entry)
        {
            Description = $"{Guid.NewGuid()}";
            Entries.Add(entry);
        }
    }

    public class Entry
    {
        public Guid Id { get; set; }
    }

    public class MyDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=yolo;Username=postgres;Password=postgres");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Session>().HasKey(_ => _.Id);
            modelBuilder.Entity<Session>().Property(_ => _.ModifiedOn).IsConcurrencyToken();
            modelBuilder.Entity<Session>().HasMany(_ => _.Entries);
            modelBuilder.Entity<Entry>().HasKey(_ => _.Id);
        }
    }
}
