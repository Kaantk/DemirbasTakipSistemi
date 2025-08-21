using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Azure;
using Business.DependecyResolvers.Autofac;
using Core.Utilities.Results;
using Core.Utilities.Security.Encyption;
using Core.Utilities.Security.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Autofac configuration
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            // Autofac module registration
            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                containerBuilder.RegisterModule<AutofacBusinessModule>();
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddCors(options => options.AddPolicy("AllowOrigin", builder => builder.WithOrigins("https://localhost:7105")));

            var tokenOptions = builder.Configuration.GetSection("TokenOptions").Get<TokenOptions>();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = tokenOptions.Issuer,
                        ValidAudience = tokenOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.SecurityKey)),
                        RoleClaimType = ClaimTypes.Role
                    };

                    options.Events = new JwtBearerEvents
                    {
                        // Yetkisiz (401)
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/auth/login"); // Yetkisiz erişim durumunda kullanıcıyı login sayfasına yönlendir
                            return Task.CompletedTask;
                        },

                        // Forbidden (403)
                        OnForbidden = context =>
                        {
                            context.Response.Redirect("/home/notfound");
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Kullanıcı 30 dakika boyunca istek yapmazsa session silinir
                options.Cookie.HttpOnly = true; // JS ile session'ın cookie bilgilerine erişilmesini engeller
                options.Cookie.IsEssential = true; // Session'ı kullanabilmek için bu ayarın gerekli olduğunu belirtir
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseCors(builder => builder.WithOrigins("https://localhost:7105").AllowAnyHeader());
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.Use(async (context, next) =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization") &&
                    context.Request.Cookies.ContainsKey("UserToken"))
                {
                    // Cookie'den JSON string'i al
                    var cookieValue = context.Request.Cookies["UserToken"];

                    // JSON'dan AccessToken nesnesini deserialize et
                    var tokenData = System.Text.Json.JsonSerializer.Deserialize<AccessToken>(cookieValue);

                    if (tokenData != null)
                    {
                        // Sadece token string'i header'a ekle
                        context.Request.Headers.Add("Authorization", "Bearer " + tokenData.Token);
                    }
                }

                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStatusCodePagesWithRedirects("home/notfound");

            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=home}/{action=index}/{id?}");

            app.Run();
        }
    }
}
