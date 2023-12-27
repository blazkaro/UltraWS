using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;
using UltraWS.Extensions;
using UltraWS.IntegrationTests.Fixtures.Base;

namespace UltraWS.IntegrationTests.Fixtures;

public class WsHubEndpoint_WithAuthorizationFixture : WsHubEndpointFixtureBase
{
    protected override TestServer Server { get; init; }
    public override Uri WsHubUri { get; protected init; }

    public WsHubEndpoint_WithAuthorizationFixture()
    {
        Server = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(Environment.CurrentDirectory);

                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication()
                        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cfg =>
                        {
                            cfg.Events.OnRedirectToLogin = (ctx) =>
                            {
                                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                return Task.CompletedTask;
                            };
                        });

                    services.AddAuthorization();

                    services.AddUltraWs()
                        .AddWsHub<TestWsHub>();
                });

                builder.Configure(app =>
                {
                    app.UseRouting();

                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.UseWebSockets();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/authenticate", async (HttpContext context, [FromQuery] string clientId) =>
                        {
                            var claimsIdentity = new ClaimsIdentity(new List<Claim>
                            {
                                new(ClaimTypes.NameIdentifier, clientId)
                            }, CookieAuthenticationDefaults.AuthenticationScheme);

                            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                            return Results.Ok();
                        });

                        endpoints.MapUltraWs<TestWsHub>("")
                            .RequireAuthorization(policy =>
                            {
                                policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
                                policy.RequireClaim(ClaimTypes.NameIdentifier);
                                policy.RequireAuthenticatedUser();
                            });
                    });
                });
            }).Server;

        WsHubUri = Server.BaseAddress;
    }

    public async Task<StringValues> GetAuthenticationCookie(string clientId)
    {
        var uriBuilder = new UriBuilder(WsHubUri);
        uriBuilder.Path = "/authenticate";
        uriBuilder.Query = $"?clientId={clientId}";

        var response = await GetHttpClient().GetAsync(uriBuilder.Uri, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        return response.Headers.GetValues(HeaderNames.SetCookie).SingleOrDefault();
    }
}
