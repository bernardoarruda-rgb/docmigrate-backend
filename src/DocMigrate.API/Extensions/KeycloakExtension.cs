using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Common;

namespace DocMigrate.API.Extensions;

public static class KeycloakExtension
{
    public static IServiceCollection AddKeycloakAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddKeycloakWebApiAuthentication(configuration);
        services.AddKeycloakAuthorization(options =>
        {
            options.EnableRolesMapping = RolesClaimTransformationSource.ResourceAccess;
            options.RolesResource = configuration["Keycloak:resource"];
        });

        return services;
    }
}
