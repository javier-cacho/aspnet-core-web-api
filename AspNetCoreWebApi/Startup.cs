using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore; 
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreWebApi.Options;
using AspNetCoreWebApi.DAL;
using AspNetCoreWebApi.Services;
using AspNetCoreWebApi.Models;

namespace AspNetCoreWebApi
{
    public class Startup
    {
        #region Authentication Properties
        private const string SecretKey = "in_a_real_app_you_need_a_trusted_source_for_this";
        private readonly SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));
        #endregion

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                ;
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddDbContext<ProductsContext>(opt => opt.UseInMemoryDatabase());

            // Make authentication compulsory 
            services.AddMvc(config => config.Filters.Add
                (
                    new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()))
                );

            // Use policy auth.
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy",
                                  policy => policy.RequireClaim("Role", "Admin"));

                options.AddPolicy("UserPolicy",
                                  policy => policy.RequireClaim("Role", "User"));
            });

            // Get options from app settings
            var jwtAppSettingOptions = Configuration.GetSection(nameof(Options.JwtIssuerOptions));

            // Configure JwtIssuerOptions
            services.Configure<JwtIssuerOptions>(options =>
            {
                options.Issuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                options.Audience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(this.signingKey, SecurityAlgorithms.HmacSha256);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseCors("CorsPolicy");

            var jwtAppSettingOptions = Configuration.GetSection(nameof(JwtIssuerOptions));
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)],

                ValidateAudience = true,
                ValidAudience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)],

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = this.signingKey,

                RequireExpirationTime = true,
                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };

            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = tokenValidationParameters
            });


            var context = app.ApplicationServices.GetService<ProductsContext>();
            InitProductsContext(context);

            app.UseMvc();
        }
    

    private static void InitProductsContext(ProductsContext productsContext)
    {
        
            var products = new List<Product>
                {
                    new Product {Id=new Guid(), Name="Orange", Available="Available", Description="A fruit with same name and color", Price="1.00"},
                    new Product {Id=new Guid(), Name="Banana", Available="Available", Description="Yellow fruit; monkeys love it", Price="2.03"},
                    new Product {Id=new Guid(), Name="Watermelon", Available="Not Available", Description="Keeps  the winter cold during the summer", Price="3.05"},
                    new Product {Id=new Guid(), Name="Apple", Available="Available", Description="A fruit rather than a mobile phone", Price="4.75"}
                };

            productsContext.AddRange(products);
            productsContext.SaveChanges();

    }
    }
}
