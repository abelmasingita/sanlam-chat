using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is missing. " +
                    "Add it to appsettings.Development.json or set the " +
                    "ConnectionStrings__DefaultConnection environment variable.");

            services.AddDbContext<ChatDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IMessageRepository, MessageRepository>();

            return services;
        }
    }
}
