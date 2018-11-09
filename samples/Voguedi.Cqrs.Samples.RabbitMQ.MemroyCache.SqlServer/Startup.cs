using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer.Stores;

namespace Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer
{
    public class Startup
    {
        #region Public Properties

        public IConfiguration Configuration { get; }

        #endregion

        #region Ctors

        public Startup(IConfiguration configuration) => Configuration = configuration;

        #endregion

        #region Public Methods

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddVoguedi(s =>
            {
                s.UseRabbitMQ(o =>
                {
                    o.HostName = "localhost";
                    o.ExchangeName = "Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer";
                });
                s.UseMemoryCache(TimeSpan.FromMinutes(5));
                s.UseSqlServer(@"Server=DESKTOP-GQ9I89D\MSSQLSERVER16;Database=Test;User Id=sa;Password=123;");
                s.UseJson();
            });
            services.AddSingleton<INoteStore>(_ => new NoteStore(@"Server=DESKTOP-GQ9I89D\MSSQLSERVER16;Database=Test;User Id=sa;Password=123;"));
            services.AddSwaggerGen(s => s.SwaggerDoc("v1", new Info { Title = "Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer", Version = "v1" }));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(s => s.SwaggerEndpoint("/swagger/v1/swagger.json", "Voguedi.Cqrs.Samples.RabbitMQ.MemroyCache.SqlServer"));
        }

        #endregion
    }
}
