using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.ActiveDirectory;
using Owin;
using System.IdentityModel.Tokens;

namespace TodoListService
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseWindowsAzureActiveDirectoryBearerAuthentication(
                new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    Tenant = ConfigurationManager.AppSettings["ida:Tenant"],
					//We set SaveSigninToken to true, to be able to retrieve the bearer token using the code: ClaimsPrincipal.Current.Identities.First().BootstrapContext
					//This flag is mandatory for this flow to work
					TokenValidationParameters = new TokenValidationParameters{ SaveSigninToken = true, ValidAudience= ConfigurationManager.AppSettings["ida:Audience"] }
                });
        }
    }
}
