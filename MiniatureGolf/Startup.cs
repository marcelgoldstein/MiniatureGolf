using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniatureGolf.DAL;
using MiniatureGolf.Services;
using MiniatureGolf.Settings;

namespace MiniatureGolf;

public class Startup
{
    private readonly IConfiguration configuration;

    public Startup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddRazorPages();
        _ = services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; }); 
        _ = services.AddTelerikBlazor();

        _ = services.AddSingleton<GameService>();

        _ = services.AddDbContext<MiniatureGolfContext>(optionsBuilder =>
        {
            var conStr = configuration.GetConnectionString("MiniatureGolfDb");
            _ = optionsBuilder.UseSqlServer(conStr);
        }, ServiceLifetime.Transient, ServiceLifetime.Transient);

        // Load/Bind custom configuration
        var settingsSection = configuration.GetSection(nameof(AppSettings));
        _ = services.Configure<AppSettings>(settingsSection); // this makes them resolvable through -> IOptions<AppSettings>
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            _ = app.UseDeveloperExceptionPage();
        }
        else
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            _ = app.UseHsts();
        }

        _ = app.UseHttpsRedirection();

        _ = app.UseStaticFiles();

        _ = app.UseRouting();

        _ = app.UseEndpoints(endpoints =>
        {
            _ = endpoints.MapBlazorHub();
            _ = endpoints.MapFallbackToPage("/_Host");
        });

        #region database migration
        using var scope = app.ApplicationServices.CreateScope();
        using var db = scope.ServiceProvider.GetService<MiniatureGolfContext>();
        db.Database.Migrate();
        #endregion database migration
    }
}
