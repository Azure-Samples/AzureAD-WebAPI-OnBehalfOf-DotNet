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
using System;
using System.Collections.Generic;
using System.Configuration;

// The following using statements were added for this sample.
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;

namespace TodoListClient
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//
		// The Client ID is used by the application to uniquely identify itself to Azure AD.
		// The Tenant is the name of the Azure AD tenant in which this application is registered.
		// The AAD Instance is the instance of Azure, for example public Azure or Azure China.
		// The Authority is the sign-in URL of the tenant.
		// The Scopes is a list of scopes for the token request. For more information, see https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-permissions-and-consent.
		//
		private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];

		private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
		private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
		private static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
		private static string todoListScope = ConfigurationManager.AppSettings["todo:TodoListScope"];
		private static string todoListBaseAddress = ConfigurationManager.AppSettings["todo:TodoListBaseAddress"];
		private static readonly string[] Scopes = { todoListScope };

		private HttpClient httpClient = new HttpClient();
		private readonly IPublicClientApplication _app;

		// Button strings
		private const string signInString = "Sign In";

		private const string clearCacheString = "Clear Cache";

		public MainWindow()
		{
			InitializeComponent();

			//Create an instance of PublicClientApplication using the Build Patten
			_app = PublicClientApplicationBuilder.Create(clientId)
				.WithAuthority(authority)
				.Build();

			//Hooking our file cache into the UserTokenCache
			TokenCacheHelper.EnableSerialization(_app.UserTokenCache);

			GetTodoList();
		}

		private async void GetTodoList()
		{
			await GetTodoListAsync(SignInButton.Content.ToString() != clearCacheString);
		}

		private async Task GetTodoListAsync(bool isAppStarting)
		{
			AuthenticationResult result = null;
			try
			{
				//Once the user have signed in, the method GetAccountsAsync will return the logged user IAccount.
				//IAccount is required by MSAL to acquire token
				var accounts = (await _app.GetAccountsAsync()).ToList();
				if (!accounts.Any())
				{
					SignInButton.Content = signInString;
					return;
				}

				//Calling MSAL to acquire an access token for the logged user IAccount
				result = await _app.AcquireTokenSilent(Scopes, accounts.FirstOrDefault())
					.ExecuteAsync()
					.ConfigureAwait(false);

				Dispatcher.Invoke(() =>
				{
					SignInButton.Content = clearCacheString;
					SetUserName(result.Account);
				});
			}
			// There is no access token in the cache, so prompt the user to sign-in.
			catch (MsalUiRequiredException)
			{
				if (!isAppStarting)
				{
					MessageBox.Show("Please sign in to view your To-Do list");
					SignInButton.Content = signInString;
				}
			}
			catch (MsalException ex)
			{
				// An unexpected error occurred.
				string message = ex.Message;
				if (ex.InnerException != null)
				{
					message += "Error Code: " + ex.ErrorCode + "Inner Exception : " + ex.InnerException.Message;
				}
				MessageBox.Show(message);

				UserName.Content = Properties.Resources.UserNotSignedIn;
				return;
			}

			// Once the token has been returned by MSAL, add it to the http authorization header, before making the call to access the To Do list service.
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

			// Call the To Do list service endpoint (HTTP GET api/todolist) sending the bearer token on the HTTP header.
			HttpResponseMessage response = await httpClient.GetAsync(todoListBaseAddress + "/api/todolist");

			if (response.IsSuccessStatusCode)
			{
				// Read the response and databind to the GridView to display To Do items.
				string content = await response.Content.ReadAsStringAsync();
				JavaScriptSerializer serializer = new JavaScriptSerializer();
				List<TodoItem> toDoArray = serializer.Deserialize<List<TodoItem>>(content);

				Dispatcher.Invoke(() =>
				{
					TodoList.ItemsSource = toDoArray.Select(t => new { t.Title });
				});
			}
			else
			{
				MessageBox.Show("An error occurred : " + response.ReasonPhrase);
			}

			return;
		}

		private async void RefreshList(object sender, RoutedEventArgs e)
		{
			await GetTodoListAsync(SignInButton.Content.ToString() != clearCacheString).ConfigureAwait(false);
		}

		private async void AddTodoItem(object sender, RoutedEventArgs e)
		{
			await AddTodoItemAsync();
		}

		private async Task AddTodoItemAsync()
		{
			var accounts = (await _app.GetAccountsAsync()).ToList();

			if (!accounts.Any())
			{
				MessageBox.Show("Please sign in first");
				return;
			}

			if (string.IsNullOrEmpty(TodoText.Text))
			{
				MessageBox.Show("Please enter a value for the To Do item name");
				return;
			}

			AuthenticationResult result = null;
			try
			{
				//Calling MSAL to acquire an access token with the scope 'user_impersonation' for the logged user IAccount
				result = await _app.AcquireTokenSilent(Scopes, accounts.FirstOrDefault())
					.ExecuteAsync()
					.ConfigureAwait(false);

				SetUserName(result.Account);
			}
			catch (MsalUiRequiredException)
			{
				MessageBox.Show("Please re-sign");
				SignInButton.Content = signInString;
			}
			catch (MsalException ex)
			{
				// An unexpected error occurred.
				string message = ex.Message;
				if (ex.InnerException != null)
				{
					message += "Error Code: " + ex.ErrorCode + "Inner Exception : " + ex.InnerException.Message;
				}

				Dispatcher.Invoke(() =>
				{
					UserName.Content = Properties.Resources.UserNotSignedIn;
					MessageBox.Show("Unexpected error: " + message);
				});

				return;
			}

			// Once the token has been returned by MSAL, add it to the http authorization header, before making the call to access the To Do service.
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

			// Forms encode Todo item, to POST to the todo list web api.
			HttpContent content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("Title", TodoText.Text) });

			// Call the To Do list service endpoint (HTTP POST /api/todolist), sending the bearer token on the HTTP header.
			HttpResponseMessage response = await httpClient.PostAsync(todoListBaseAddress + "/api/todolist", content);

			if (response.IsSuccessStatusCode)
			{
				TodoText.Text = "";
				GetTodoList();
			}
			else
			{
				MessageBox.Show("An error occurred : " + response.ReasonPhrase);
			}
		}

		private async void SignIn(object sender = null, RoutedEventArgs args = null)
		{
			var accounts = (await _app.GetAccountsAsync()).ToList();

			// If there is already a token in the cache, clear the cache and update the label on the button.
			if (SignInButton.Content.ToString() == clearCacheString)
			{
				TodoList.ItemsSource = string.Empty;

				// clear the cache
				while (accounts.Any())
				{
					await _app.RemoveAsync(accounts.First());
					accounts = (await _app.GetAccountsAsync()).ToList();
				}

				// Also clear cookies from the browser control.
				SignInButton.Content = signInString;
				UserName.Content = Properties.Resources.UserNotSignedIn;
				return;
			}

			AuthenticationResult result = null;
			try
			{
				// Force a sign-in (PromptBehavior.Always), as the MSAL web browser might contain cookies for the current user
				result = await _app.AcquireTokenInteractive(Scopes)
					.WithAccount(accounts.FirstOrDefault())
					.WithPrompt(Prompt.SelectAccount)
					.ExecuteAsync()
					.ConfigureAwait(false);

				Dispatcher.Invoke(() =>
				{
					SignInButton.Content = clearCacheString;
					SetUserName(result.Account);
					GetTodoList();
				});
			}
			catch (MsalException ex)
			{
				if (ex.ErrorCode == "access_denied")
				{
					// The user canceled sign in, take no action.
				}
				else
				{
					// An unexpected error occurred.
					string message = ex.Message;
					if (ex.InnerException != null)
					{
						message += "Error Code: " + ex.ErrorCode + "Inner Exception : " + ex.InnerException.Message;
					}

					MessageBox.Show(message);
				}

				UserName.Content = Properties.Resources.UserNotSignedIn;
			}
		}

		// Set user name to text box
		private void SetUserName(IAccount userInfo)
		{
			string userName = null;

			if (userInfo != null)
			{
				if (userInfo != null)
				{
					userName = userInfo.Username ?? userInfo.HomeAccountId.ObjectId;
				}

				if (userName == null)
					userName = Properties.Resources.UserNotIdentified;

				UserName.Content = userName;
			}
		}
	}
}