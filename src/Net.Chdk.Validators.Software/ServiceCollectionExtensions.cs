using Microsoft.Extensions.DependencyInjection;
using Net.Chdk.Model.Software;

namespace Net.Chdk.Validators.Software
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSoftwareValidator(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<IValidator<SoftwareInfo>, SoftwareValidator>();
        }
    }
}
