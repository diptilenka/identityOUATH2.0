using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;

namespace Api.NSwag
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication("Bearer", options =>
                {
                    options.ApiName = "api1";
                    options.Authority = "http://localhost:5100";
                });

            services.AddOpenApiDocument(options =>
            {
                options.DocumentName = "v1";
                options.Title = "Protected API";
                options.Version = "v1";

                options.AddSecurity("oauth2", new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = "http://localhost:5100/connect/authorize",
                            TokenUrl = "http://localhost:5100/connect/token",
                            Scopes = new Dictionary<string, string> { { "api1", "Demo API - full access" } }
                        }
                    }
                });

                options.OperationProcessors.Add(new OperationSecurityScopeProcessor("oauth2"));
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseOpenApi();
            app.UseSwaggerUi3(options =>
            {
                options.OAuth2Client = new OAuth2ClientSettings
                {
                    ClientId = "bob",
                    ClientSecret = "secret",
                    AppName = "Demo API - Swagger",
                    UsePkceWithAuthorizationCodeGrant = true
                };
            });

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }
    }
}
