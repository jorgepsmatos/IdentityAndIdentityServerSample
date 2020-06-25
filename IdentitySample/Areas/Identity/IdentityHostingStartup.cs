using System;
using IdentitySample.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(IdentitySample.Areas.Identity.IdentityHostingStartup))]

namespace IdentitySample.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddDbContext<IdentitySampleContext>(options =>
                    options.UseSqlite(
                        context.Configuration.GetConnectionString("IdentitySampleContextConnection")));

                services.AddAuthentication(o =>
                    {
                        o.DefaultScheme = IdentityConstants.ApplicationScheme;
                        o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                    })
                    .AddOpenIdConnect("oidc", options =>
                    {
                        options.Authority = "https://localhost:5001";
                        options.RequireHttpsMetadata = false;

                        options.ClientId = "mvc";
                        options.ClientSecret = "secret";
                        options.ResponseType = "id_token";

                        options.SaveTokens = true;

                        options.Scope.Add("offline_access");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");
                    })
                    .AddIdentityCookies();

                services
                    .AddIdentityCore<IdentityUser>(o =>
                    {
                        o.Stores.MaxLengthForKeys = 128;
                        o.SignIn.RequireConfirmedAccount = true;
                        o.Password.RequireDigit = false;
                        o.Password.RequireLowercase = false;
                        o.Password.RequireNonAlphanumeric = false;
                        o.Password.RequireUppercase = false;
                        o.Password.RequiredLength = 6;
                        o.Password.RequiredUniqueChars = 1;
                    })
                    .AddDefaultUI()
                    .AddEntityFrameworkStores<IdentitySampleContext>()
                    .AddDefaultTokenProviders();
            });
        }
    }
}