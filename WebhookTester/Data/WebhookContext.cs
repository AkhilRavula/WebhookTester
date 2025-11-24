using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebhookTester.Models;

namespace WebhookTester.Data
{
    public class WebhookContext : DbContext
    {
        public WebhookContext(DbContextOptions<WebhookContext> options)
            : base(options)
        {
        }

        public DbSet<WebhookEndpoint> WebhookEndpoints { get; set; } = null!;
        public DbSet<WebhookRequest> WebhookRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WebhookEndpoint>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<WebhookRequest>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<WebhookRequest>()
                .HasOne(r => r.Webhook)
                .WithMany(w => w.Requests)
                .HasForeignKey(r => r.WebhookId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Indexes for performance
            modelBuilder.Entity<WebhookRequest>()
                .HasIndex(r => r.WebhookId);
                
            modelBuilder.Entity<WebhookRequest>()
                .HasIndex(r => r.ReceivedAt);
        }
    }
}
