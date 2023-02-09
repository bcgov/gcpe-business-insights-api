using Gcpe.Hub.BusinessInsights.API.DbContexts;
using Gcpe.Hub.BusinessInsights.API.Services;
using Gcpe.Hub.Data.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;

namespace Gcpe.Hub.BusinessInsights.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        private IConfiguration Configuration { get; }
        private IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var localDbConnectionString = Configuration["ConnectionStrings:DefaultDbContext"];
            var hubDbConnectionString = Configuration["ConnectionStrings:HubDbContext"];

            services.AddDbContext<HubDbContext>(options => options.UseSqlServer(hubDbConnectionString));
            services.AddDbContext<HubBusinessInsightsDbContext>(options => options.UseSqlServer(hubDbConnectionString));
            services.AddDbContext<LocalDbContext>(options =>
            {
                options.UseSqlServer(localDbConnectionString);
                options.EnableSensitiveDataLogging(false);
            });

            services.AddHealthChecks()
                .AddSqlServer(
                    connectionString: localDbConnectionString,
                    name: "localDbConnection"
                )
                .AddSqlServer(
                    connectionString: hubDbConnectionString,
                    name: "hubDbConnection"
            );

            services.AddMicrosoftIdentityWebApiAuthentication(Configuration);

            services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            services.AddScoped<IHubBusinessInsightsRepository, HubBusinessInsightsRepository>();
            services.AddScoped<ILocalDataRepository, LocalDataRepository>();

            services.AddHostedService<TranslationProcessingWorker>();
            services.AddScoped<IScopedProcessingService, ScopedProcessingService>();
            services.AddScoped<IDataSynchronizationService, DataSynchronizationService>();
            services.AddScoped<IAzureDevOpsService, AzureDevOpsService>();
            services.AddScoped<IReportGenerationService, ReportGenerationService>();

            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.WithOrigins("http://localhost:3000")
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            services.AddTransient<ILogger>(s => s.GetRequiredService<ILogger<Program>>());

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gcpe.Hub.BusinessInsights.API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gcpe.Hub.BusinessInsights.API v1"));
            }

            app.UseCors("MyPolicy");

            app.UseHttpsRedirection();
            app.UseFileServer();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health").AllowAnonymous();
            });
        }
    }
}
