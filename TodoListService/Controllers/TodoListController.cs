/*
 The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using TodoListService.DAL;

// The following using statements were added for this sample.
using TodoListService.Models;
using TodoListService.Utils;

namespace TodoListService.Controllers
{
	[Authorize]
	[EnableCors(origins: "*", headers: "*", methods: "*")]
	public class TodoListController : ApiController
	{
		//
		// The Client ID is used by the application to uniquely identify itself to Azure AD.
		// The App Key is a credential used by the application to authenticate to Azure AD.
		// The Tenant is the name of the Azure AD tenant in which this application is registered.
		// The AAD Instance is the instance of Azure, for example public Azure or Azure China.
		// The Authority is the sign-in URL of the tenant.
		//
		private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
		private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
		private static string clientId = ConfigurationManager.AppSettings["ida:ClientID"];
		private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
		private static string graphUserUrl = ConfigurationManager.AppSettings["ida:GraphUserUrl"];
		private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];

		private TodoListServiceContext dbContext = new TodoListServiceContext();

		// GET api/todolist
		public IEnumerable<TodoItem> Get()
		{
			// The Scope claim tells you what permissions the client application has in the service.
			// In this case we look for a scope value of user_impersonation, or full access to the service as the user.
			var scopeClaim = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
			if (scopeClaim == null || (!scopeClaim.Value.Contains("user_impersonation")))
			{
				throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The Scope claim does not contain 'user_impersonation' or scope claim not found" });
			}

			// A user's To Do list is keyed off of the NameIdentifier claim, which contains an immutable, unique identifier for the user.
			Claim subject = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier);

			return from todo in dbContext.TodoItems
				   where todo.Owner == subject.Value
				   select todo;
		}

		// POST api/todolist
		public async Task Post(TodoItem todo)
		{
			try
			{
				var scopeClaim = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope");
				if (scopeClaim == null || !scopeClaim.Value.Contains("user_impersonation"))
				{
					throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The Scope claim does not contain 'user_impersonation' or scope claim not found" });
				}

				// Call the Graph API On Behalf Of the user who called the To Do list web API.
				UserProfile profile = await CallGraphAPIOnBehalfOfUser();

				string augmentedTitle = null;
				if (profile != null)
				{
					augmentedTitle = String.Format("{0}, First Name: {1}, Last Name: {2}", todo.Title, profile.GivenName, profile.Surname);
				}
				else
				{
					return;
				}

				if (todo != null && !string.IsNullOrWhiteSpace(todo.Title))
				{
					dbContext.TodoItems.Add(new TodoItem { Title = augmentedTitle, Owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value });
					dbContext.SaveChanges();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private async Task<UserProfile> CallGraphAPIOnBehalfOfUser()
		{
			string[] scopes = { "user.read" };
			UserProfile profile = null;

			// We will use MSAL.NET to get a token to call the API On Behalf Of the current user
			try
			{
				string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

				// Creating a ConfidentialClientApplication using the Build pattern (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-Applications)
				var app = ConfidentialClientApplicationBuilder.Create(clientId)
				   .WithAuthority(authority)
				   .WithClientSecret(appKey)
				   .WithRedirectUri(redirectUri)
				   .Build();

				// Hooking MSALPerUserSqlTokenCacheProvider class on ConfidentialClientApplication's UserTokenCache.
				MSALPerUserSqlTokenCacheProvider sqlCache = new MSALPerUserSqlTokenCacheProvider(app.UserTokenCache, dbContext, ClaimsPrincipal.Current);

				//Grab the Bearer token from the HTTP Header using the identity bootstrap context. This requires SaveSigninToken to be true at Startup.Auth.cs
				var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext as System.IdentityModel.Tokens.BootstrapContext;

				// Creating a UserAssertion based on the Bearer token sent by TodoListClient request.
				//urn:ietf:params:oauth:grant-type:jwt-bearer is the grant_type required when using On Behalf Of flow: https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow
				UserAssertion userAssertion = new UserAssertion(bootstrapContext.Token, "urn:ietf:params:oauth:grant-type:jwt-bearer");

				// Acquiring an AuthenticationResult for the scope user.read, impersonating the user represented by userAssertion, using the OBO flow
				AuthenticationResult result = await app.AcquireTokenOnBehalfOf(scopes, userAssertion)
					.ExecuteAsync();

				string accessToken = result.AccessToken;
				if (accessToken == null)
				{
					throw new Exception("Access Token could not be acquired.");
				}

				// Call the Graph API and retrieve the user's profile.
				string requestUrl = String.Format(CultureInfo.InvariantCulture, graphUserUrl, HttpUtility.UrlEncode(tenant));
				HttpClient client = new HttpClient();
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
				HttpResponseMessage response = await client.SendAsync(request);

				// Return the user's profile.
				if (response.IsSuccessStatusCode)
				{
					string responseString = await response.Content.ReadAsStringAsync();
					profile = JsonConvert.DeserializeObject<UserProfile>(responseString);
					return (profile);
				}

				// An unexpected error occurred calling the Graph API.
				throw new Exception("An unexpected error occurred calling the Graph API.");
			}
			catch (MsalUiRequiredException msalServiceException)
			{
				/*
				* If you used the scope `.default` on the client application, the user would have been prompted to consent for Graph API back there
				* and no incremental consents are required (this exception is not expected). However, if you are using the scope `user_impersonation`,
				* this exception will be thrown at the first time the API tries to access Graph on behalf of the user for an incremental consent.
				* You must then, add the logic to delegate the consent screen to your client application here.
				* This sample doesn't use the incremental consent strategy.
				*/
				throw msalServiceException;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
	}
}