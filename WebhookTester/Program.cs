using Microsoft.EntityFrameworkCore;
using WebhookTester.Data;
using WebhookTester.Hubs;
using WebhookTester.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Microsoft.AspNetCore.HttpOverrides;

namespace WebhookTester
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure forwarded headers for load balancer
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddHostedService<CleanupService>();
            builder.Services.AddDistributedMemoryCache(); 
            builder.Services.AddHealthChecks();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            
            // AWS S3 Service for webhook storage
            builder.Services.AddAWSService<IAmazonS3>();
            builder.Services.AddSingleton<S3StorageService>();
            
            // SQLite Database (COMMENTED OUT - using S3 instead)
            // builder.Services.AddDbContext<WebhookContext>(options =>
            //     options.UseSqlite(builder.Configuration.GetConnectionString("WebhookContext")));
            // builder.Services.AddDataProtection().PersistKeysToDbContext<WebhookContext>();

            var app = builder.Build();

            // Use forwarded headers from load balancer (MUST be first middleware)
            app.UseForwardedHeaders();

            // SQLite Database Migration (COMMENTED OUT - using S3 instead)
            // using(var scope = app.Services.CreateScope())
            // {
            //     try{
            //         var dbContext = scope.ServiceProvider.GetRequiredService<WebhookContext>();
            //         dbContext.Database.Migrate();
            //     }
            //     catch(Exception ex)
            //     {
            //         var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            //         logger.LogError(ex, "An error occurred while migrating or initializing the database.");
            //     }
            // }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();
            
            app.UseSession();

            app.MapRazorPages();
            app.MapControllers();
            app.MapHub<WebhookHub>("/webhookHub");
            app.MapHealthChecks("/api/health");

            app.Run();
        }
    }
}
