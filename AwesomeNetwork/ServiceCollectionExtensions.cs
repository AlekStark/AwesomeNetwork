using AwesomeNetwork.Data;
using AwesomeNetwork.Data.Repository;
using AwesomeNetwork.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AwesomeNetwork
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAwesomeNetwork(this IServiceCollection services,
            WebApplicationBuilder builder)
        {
            var connection = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection));
            builder.Services.AddIdentity<User, IdentityRole>(opts => {
                opts.Password.RequiredLength = 5;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequireLowercase = false;
                opts.Password.RequireUppercase = false;
                opts.Password.RequireDigit = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddScoped<FriendService>();
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddAutoMapper(typeof(Program).Assembly);
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromDays(90));
            return services;
        }
    }
}
