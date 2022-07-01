using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Swashbuckle
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(
                options => options.AddPolicy(
                    "allow_all",
                    policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
            
            services.AddControllers();

            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Audience = "api1";
                    options.Authority = "http://localhost:5100";
                    options.RequireHttpsMetadata = false;
                });

            services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();
                options.SwaggerDoc("v1", new OpenApiInfo {Title = "Protected API", Version = "v1"});

                var tokenUrl = new Uri("http://localhost:5100/connect/token");
                var scopes = new Dictionary<string, string> { {"api1", "Demo API - full access"} };
                
                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        ClientCredentials = new OpenApiOAuthFlow {TokenUrl = tokenUrl, Scopes = scopes},
                        Password = new OpenApiOAuthFlow { TokenUrl = tokenUrl, Scopes = scopes }
                    }
                });

                options.OperationFilter<AuthorizeCheckOperationFilter>();
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCors("allow_all");
            
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger(options => options.PreSerializeFilters.Add((document, _) =>
            {
                document.Servers = new List<OpenApiServer> { new OpenApiServer { Url = "http://localhost:5000" } };
            }));
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");

                options.OAuthClientId("client_1");
                options.OAuthClientSecret("secret");
                options.OAuthAppName("Demo API - Swagger");
                options.DisplayOperationId();
            });

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }
    }

    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize = context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
                               context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

            if (hasAuthorize)
            {
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme {Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = "oauth2"}}] 
                            = new[] {"api1"}
                    }
                };
            }
        }
    }
}
