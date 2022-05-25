using DurableAppV4Net6Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.IO;

[assembly: FunctionsStartup(typeof(Startup))]

namespace DurableAppV4Net6Test
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;
            var services = builder.Services;

            // Configuring MS Identity Web is NOT working this way
            // IDW10503: Cannot determine the cloud Instance. The provided authentication scheme was ''. Microsoft.Identity.Web inferred 'OpenIdConnect' as the authentication scheme.
            // Available authentication schemes are 'Bearer,WebJobsAuthLevel,ArmToken'. See https://aka.ms/id-web/authSchemes. 

            //var authBuilder = new AuthenticationBuilder(services);
            //authBuilder.AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"))
            //    .EnableTokenAcquisitionToCallDownstreamApi()
            //    .AddDownstreamWebApi("SharePoint", configuration)
            //    .AddMicrosoftGraphAppOnly(authProvider => new GraphServiceClient(authProvider))
            //    .AddInMemoryTokenCaches();

            // Configuring it this way is working, but only when the Function is using Anonymous Auth
            // (however, if you use the TokenAcquisition inside any Durable Activity, it works fine)
            services
                .AddAuthentication(sharedOptions =>
                {
                    sharedOptions.DefaultScheme = Microsoft.Identity.Web.Constants.Bearer;
                    sharedOptions.DefaultChallengeScheme = Microsoft.Identity.Web.Constants.Bearer;
                })
                .AddMicrosoftIdentityWebApi(configuration)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddDownstreamWebApi("SharePoint", configuration)
                .AddMicrosoftGraphAppOnly(authProvider => new GraphServiceClient(authProvider))
                .AddInMemoryTokenCaches();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "local.settings.json"), true, true)
                .AddEnvironmentVariables();
        }
    }
}
