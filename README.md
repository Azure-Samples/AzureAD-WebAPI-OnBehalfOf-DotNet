---
services: active-directory
platforms: dotnet
author: jmprieur
level: 400
client: .NET Framework 4.5 Console, JavaScript SPA
service: ASP.NET Web API
endpoint: AAD V1
---
# Calling a downstream web API from a web API using Azure AD

![Build badge](https://identitydivision.visualstudio.com/_apis/public/build/definitions/a7934fdd-dcde-4492-a406-7fad6ac00e17/487/badge)

> There's a newer version of this sample! Check it out: https://github.com/azure-samples/ms-identity-dotnet-desktop-aspnetcore-webapi
>
> This newer sample takes advantage of the Microsoft identity platform (formerly Azure AD v2.0).
>
> While still in public preview, every component is supported in production environments.

## About this sample

### Overview

In this sample, the native client and a simple JavaScript single page application:

1. Acquire a token to act On Behalf Of the user.
2. Call a web API (`TodoListService`)
3. Which itself calls another downstream Web API (The Microsoft Graph)

The TodoListService uses a database to:

- Store the todo list
- Illustrate [token cache serialization](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Token-cache-serialization) in a service

   ![Topology](./ReadmeFiles/Topology.png)

### Scenario. How the sample uses MSAL.NET (and MSAL.js)

- `TodoListClient` uses  MSAL.NET to acquire a token for the user in order to call **TodoListService** Web API. For more information about how to acquire tokens interactively, see [Acquiring tokens interactively Public client application flows](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Acquiring-tokens-interactively---Public-client-application-flows).
- `TodoListSPA`, the single page application, uses [MSAL.js](https://github.com/AzureAD/microsoft-authentication-library-for-js) to acquire the access token to call **TodoListService** Web API.
- Then `TodoListService` also uses MSAL.NET  to get a token to act on behalf of the user to call the Microsoft Graph. For details, see [Service to service calls on behalf of the user](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Service-to-service-calls-on-behalf-of-the-user). It then decorates the todolist item entered by the user, with the First name and the Last name of the user. Below is a screen copy of what happens when the user named *automation service account* entered "item1" in the textbox.

  ![Todo list client](./ReadmeFiles/TodoListClient.png)

Both flows use the OAuth 2.0 protocol to obtain the tokens. For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

## How to run this sample

To run this sample, you'll need:

- [Visual Studio 2017](https://aka.ms/vsdownload)
- An Internet connection
- An Azure Active Directory (Azure AD) tenant. For more information on how to get an Azure AD tenant, see [How to get an Azure AD tenant](https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/)
- A user account in your Azure AD tenant. This sample will not work with a Microsoft account (formerly Windows Live account). Therefore, if you signed in to the [Azure portal](https://portal.azure.com) with a Microsoft account and have never created a user account in your directory before, you need to do that now.

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/Azure-Samples/active-directory-dotnet-webapi-onbehalfof.git`

> Given that the name of the sample is pretty long, and so are the name of the referenced NuGet packages, you might want to clone it in a folder close to the root of your hard drive, to avoid file size limitations on Windows.

### Step 2:  Register the sample with your Azure Active Directory tenant

There are three projects in this sample. Each needs to be separately registered in your Azure AD tenant. To register these projects, you can:

- either follow the steps in the paragraphs below ([Step 2](#step-2--register-the-sample-with-your-azure-active-directory-tenant) and [Step 3](#step-3--configure-the-sample-to-use-your-azure-ad-tenant))
- or use PowerShell scripts that:
  - **automatically** create for you the Azure AD applications and related objects (passwords, permissions, dependencies)
  - modify the Visual Studio projects' configuration files.

If you want to use this automation, read the instructions in [App Creation Scripts](./AppCreationScripts/AppCreationScripts.md)

#### First step: choose the Azure AD tenant where you want to create your applications

As a first step you'll need to:

1. Sign in to the [Azure portal](https://portal.azure.com).
1. On the top bar, click on your account, and then on **Switch Directory**. 
1. Once the *Directory + subscription* pane opens, choose the Active Directory tenant where you wish to register your application, from the *Favorites* or *All Directories* list.
1. Click on **All services** in the left-hand nav, and choose **Azure Active Directory**.

> In the next steps, you might need the tenant name (or directory name) or the tenant ID (or directory ID). These are presented in the **Properties**
of the Azure Active Directory window respectively as *Name* and *Directory ID*

#### Register the service app (TodoListService-OBO)

1. In the  **Azure Active Directory** pane, click on **App registrations** and choose **New application registration**.
1. Enter a friendly name for the application, for example 'TodoListService-OBO' and select 'Web app / API' as the *Application Type*.
1. For the *sign-on URL*, enter the base URL for the sample, which is by default `https://localhost:44321/`.
1. Click on **Create** to create the application.
1. In the succeeding page, Find the *Application ID* value and copy it to the clipboard. You'll need it to configure the Visual Studio configuration file for this project.
1. Then click on **Settings**, and choose **Properties**.
1. For the App ID URI, replace the guid in the generated URI 'https://\<your_tenant_name\>/\<guid\>', with the name of your service, for example, 'https://\<your_tenant_name\>/TodoListService-OBO' (replacing `<your_tenant_name>` with the name of your Azure AD tenant)
1. From the Settings menu, choose **Keys** and add a new entry in the Password section:

   - Type a key description (of instance `app secret`),
   - Select a key duration of either **In 1 year**, **In 2 years**, or **Never Expires**.
   - When you save this page, the key value will be displayed, copy, and save the value in a safe location.
   - You'll need this key later to configure the project in Visual Studio. This key value will not be displayed again, nor retrievable by any other means,
     so record it as soon as it is visible from the Azure portal.
1. Configure Permissions for your application. To that extent, in the Settings menu, choose the 'Required permissions' section and then,
   click on **Add**, then **Select an API**, and type `Microsoft Graph` in the textbox. Then, click on  **Select Permissions** and select **Sign in and read user profile**.

#### Register the client app (TodoListClient-OBO)

1. In the  **Azure Active Directory** pane, click on **App registrations** and choose **New application registration**.
1. Enter a friendly name for the application, for example 'TodoListClient-OBO'.
1. For the *Redirect URI*, select type `Public client (mobile & desktop)` and enter `urn:ietf:wg:oauth:2.0:oob` (this is for the desktop client).
1. Add another *Redirect URI*, select type `Web` and enter `https://localhost:44377` (this is for the SPA client).
1. Click on **Register** to create the application.
1. In the succeeding page, Find the *Application ID* value and copy it to the clipboard. You'll need it to configure the Visual Studio configuration file for this project.
1. Go to **Authentication tab**, select `Access tokens` and `ID tokens` under **Implicit grant**. Then select `Yes` under **Default client type**.
1. Enable the OAuth 2 implicit grant for your application by choosing **Manifest** at the top of the application's page. Open the inline manifest editor.
   Search for the ``oauth2AllowImplicitFlow`` property. You will find that it is set to ``false``; change it to ``true`` and click on **Save** to save the manifest.
1. Then click on **Settings**, and choose **Properties**.
1. Configure Permissions for your application. To that extent, in the Settings menu, choose the 'Required permissions' section and then,
   click on **Add**, then **Select an API**, and type `TodoListService-OBO` in the textbox. Then, click on  **Select Permissions** and select **Access 'TodoListService-OBO'**.

#### Configure known client applications for service (TodoListService-OBO)

For the middle tier web API (`TodoListService-OBO`) to be able to call the downstream web APIs, the user must grant the middle tier permission to do so in the form of consent.
However, since the middle tier has no interactive UI of its own, you need to explicitly bind the client app registration in Azure AD, with the registration for the web API.
This binding merges the consent required by both the client & middle tier into a single dialog, which will be presented to the user by the client.
You can do so by adding the "Client ID" of the client app, to the manifest of the web API in the `knownClientApplications` property. Here's how:

1. In the [Azure portal](https://portal.azure.com), navigate to your `TodoListService-OBO` app registration, and open the manifest editor by clicking on **Manifest**.
1. In the manifest, locate the `knownClientApplications` array property, and add the
   Client ID of the client application (`TodoListClient-OBO`) as an element.
   After you're done, your code should look like the following snippet with as many GUIDs as you have clients:
   `"knownClientApplications": ["94da0930-763f-45c7-8d26-04d5938baab2"]`
1. Save the TodoListService manifest by clicking the **Save** button.

### Step 3:  Configure the sample to use your Azure AD tenant

In the steps below, ClientID is the same as Application ID or AppId.

Open the solution in Visual Studio to configure the projects

#### Configure the service project

1. Open the `TodoListService\Web.Config` file
1. Find the app key `ida:Tenant` and replace the existing value with your AAD tenant name.
1. Find the app key `ida:Audience` and replace the existing value with the App ID URI you registered earlier for the TodoListService-OBO app. For instance use `https://<your_tenant_name>/TodoListService-OBO`, where `<your_tenant_name>` is the name of your Azure AD tenant.
1. Find the app key `ida:AppKey` and replace the existing value with the key you saved during the creation of the `TodoListService-OBO` app, in the Azure portal.
1. Find the app key `ida:ClientID` and replace the existing value with the application ID (clientId) of the `TodoListService-OBO` application copied from the Azure portal.

#### Configure the client project

1. Open the `TodoListClient\App.Config` file
1. Find the app key `ida:Tenant` and replace the existing value with your AAD tenant name.
1. Find the app key `ida:ClientId` and replace the existing value with the application ID (clientId) of the `TodoListClient-OBO` application copied from the Azure portal.
1. Find the app key `todo:TodoListScope` and replace the existing value to `https://<your_tenant_name>/TodoListService-OBO/.default`, where `<your_tenant_name>` is the name of your Azure AD tenant.
1. Find the app key `todo:TodoListBaseAddress` and replace the existing value with the base address of the TodoListService-OBO project (by default `https://localhost:44321/`).

#### [Optionally] Configure the TodoListSPA project

If you want to run `TodoListSPA` project as well, you need to update its config:

1. Open the `TodoListSPA\appconfig.js` file
2. In the `config`variable (which is about the Azure AD TodoListSPA configuration):

- find the member named `authority` and replace `<your_tenant_name>` with your AAD tenant name.
- find the member named `clientId` and replace the value with the Client ID for the TodoListSPA application from the Azure portal.
- find the member named `redirectUri` and replace the value with the redirect URI you provided for the TodoListSPA application from the Azure portal, for example, `https://localhost:44377/`.

3. In the `WebApiConfig`variable (which is about configuration of the resource, that is the TodoListService):

   - find the member named `resourceId` and replace the value with the  App ID URI of the TodoListService, for example `https://<your_tenant_name>/TodoListService`.

4. While running the SPA app in the browser, take care to allow popups from this app.

### Step 4: Run the sample

Clean the solution, rebuild the solution, and run it. You might want to go into the solution properties and set both projects, or the three projects, as startup projects, with the service project starting first.

Explore the sample by signing in, adding items to the To Do list, Clearing the cache (which removes the user account), and starting again.  The To Do list service will take the user's access token, received from the client, and use it to get another access token so it can act On Behalf Of the user in the Microsoft Graph API.  This sample caches the user's access token at the To Do list service, so it does not request a new access token on every request. This cache is a database cache.

[Optionally], when you have added a few items with the TodoList Client, login to the todoListSPA with the same credentials as the todoListClient, and observe the id-Token, and the content of the Todo List as stored on the service, but as Json. This will help you understand the information circulating on the network.

## About the code

There are many key points in this sample to make the [**On-Behalf-Of-(OBO) flow**](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow) work properly and in this section we will explain these key points for each project.
Though we have three applications in the solution, you will notice that we only registred two applications in Azure AD. This is because Azure AD now allows multiple types of applications, like in this case, a desktop and a javascript SPA application who share the same app registration in the Azure AD's app registration portal. 

### TodoListClient

This project represents the .NET desktop UI part of the flow, where users would sign-in and interact with the Web API. Also, this project is the consumer of information that is obtained from a downstream WebAPI. The API that this app calls also requests data from another API by using an access token obtained for the signed-in user using the **On-Behalf-Of (OBO) flow** . The first key point to pay attention is the `MainWindow` initialization. Here is the code snippet:

```csharp
private readonly IPublicClientApplication _app;

public MainWindow()
{
   InitializeComponent();
   
   _app = PublicClientApplicationBuilder.Create(clientId)
      .WithAuthority(authority)
      .Build();
       
   TokenCacheHelper.EnableSerialization(_app.UserTokenCache);

   GetTodoList();
}
```

Important things to notice:

- We create an `IPublicClientApplication` using **MSAL Build Pattern** passing the `clientId` and `authority` in the builder. This `IPublicClientApplication` will be responsible of acquiring access tokens or id tokens later in the code.
- `IPublicClientApplication` also has a token cache, that will cache [access tokens](https://docs.microsoft.com/en-us/azure/active-directory/develop/access-tokens) and [refresh tokens](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow#refresh-the-access-token) for the signed-in user. This is done so that the application can fetch access tokens after they have expired without prompting the user to sign-in again.
- Our `UserTokenCache` implementation uses the local file system for caching. Other popular options for caching tokens are `Database` or `Distributed InMemory cache`.

#### SignIn

Then we have the `SignIn` method, where the login screen is presented to the user and they can provide their credentials.

```csharp
private static readonly string[] Scopes = { "https://<yourTenant>.onmicrosoft.com/TodoListService-OBO/.default" };

private async void SignIn(object sender = null, RoutedEventArgs args = null)
{
   AuthenticationResult result = await _app.AcquireTokenInteractive(Scopes)
      .WithAccount(accounts.FirstOrDefault())
      .WithPrompt(Prompt.SelectAccount)
      .ExecuteAsync()
      .ConfigureAwait(false);
}
```

Important things to notice:

- The scope [`.default`](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-permissions-and-consent#the-default-scope) is a built-in scope for every application that refers to the static list of permissions configured on the application registration. In our scenario here, it enables the user to grant consent for permissions for both the Web API and the downstream API (Microsoft Graph). For example, the permissions for the Web API and the downstream API (Microsoft Graph) are listed below:
   - TodoListService-OBO
     - demo_scope
   - Microsoft Graph
     - user.read
- We you use the `.default` scope, the end user is prompted for a combined set of permissions that include both the **TodoListService-OBO** and **Microsoft Graph**.
- We call the [AcquireTokenInteractive](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Acquiring-tokens-interactively) which will present a window to the user to provide their credentials. When it acquires the access token, MSAL also saves this token in its token cache. When any code in the rest of the project tries to acquire an access token for the Web API with the same scope `.default`, MSAL will return the cached token.

#### Add Todo Item

The method to add a new `Todo` is where we consume our **TodoListService-OBO** Web API, that will consume the downstream **Microsoft Graph** using an access token obtained using the **On-Behalf-Of (OBO) flow**.

To check if the user is signed in, we use the method [GetAccountsAsync](https://docs.microsoft.com/en-us/dotnet/api/microsoft.identity.client.clientapplicationbase.getaccountsasync?view=azure-dotnet):

```csharp
var accounts = (await _app.GetAccountsAsync()).ToList();
if (!accounts.Any())
{
   MessageBox.Show("Please sign in first");
   return;
}
```
Now we call the `AcquireTokenSilent` method to get the cached access token we had obtained earlier during our sign-in. With this token, we can then create a HTTP POST request to our Web API attaching it on the header as `Bearer`.

```csharp
AuthenticationResult result = null;
try
{
   result = await _app.AcquireTokenSilent(Scopes, accounts.FirstOrDefault())
      .ExecuteAsync()
      .ConfigureAwait(false);
}
catch (MsalUiRequiredException)
{
   MessageBox.Show("Please re-sign");
}
catch (MsalException ex)
{
   // An unexpected error occurred.
}

HttpClient httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

HttpContent content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("Title", TodoText.Text) });

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
```

Important things to notice:

- After the **SignIn**, the user token will be cached and it can be acquired again by calling [AcquireTokenSilent](https://docs.microsoft.com/en-us/dotnet/api/microsoft.identity.client.iclientapplicationbase.acquiretokensilentasync?view=azure-dotnet).
- `MsalUiRequiredException` will be thrown if there is no token for that user with the specified scope in the cache, or it got expired. This case requires the user to **SignIn** again.
- Attaching the access token in the HTTP header as `Bearer` is mandatory when using [**On-Behalf-Of (OBO) flow**](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow).

### TodoListService

The **TodoListService** is our Web API project that will make a call to the downstream **Microsoft Graph API** using an access token obtained via the [**On-Behalf-Of (OBO) flow**](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow). The client that called **TodoListService**, sends a `Bearer` token on the HTTP header and this token will be used to impersonate the user and acquire a proper token for **Microsoft Graph API**.

The first key point to pay attention in this project is the `Startup` configuration:

```csharp
public void ConfigureAuth(IAppBuilder app)
{
   app.UseWindowsAzureActiveDirectoryBearerAuthentication(
         new WindowsAzureActiveDirectoryBearerAuthenticationOptions
         {
            Tenant = ConfigurationManager.AppSettings["ida:Tenant"],
            TokenValidationParameters = new TokenValidationParameters 
            { 
                SaveSigninToken = true, 
                ValidAudience = ConfigurationManager.AppSettings["ida:Audience"] 
            }
         });
}
```

Important things to notice:

- Notice that we are setting `SaveSigninToken` to `true` on the `TokenValidationParameters`. This is essential to get the `Bearer` token from the HTTP header using the identity bootstrap context: `ClaimsPrincipal.Current.Identities.First().BootstrapContext`

#### Call Graph API On Behalf Of User

The logic to call **Microsoft Graph** on behalf of a user is inside the method `CallGraphAPIOnBehalfOfUser`. In this sample, we are getting the user's first name and last name and adding them in the `Todo's` title.

The one thing that you will notice is that we are using a different type of token cache provider, which cache tokens in a SQL Server database. It is incapsulated in the `MSALPerUserSqlTokenCacheProvider` class.

```csharp
private async Task<UserProfile> CallGraphAPIOnBehalfOfUser()
{
   string[] scopes = { "user.read" };
   UserProfile profile = null;

   try
   {
      var app = ConfidentialClientApplicationBuilder.Create(clientId)
         .WithAuthority(authority)
         .WithClientSecret(appKey)
         .WithRedirectUri(redirectUri)
         .Build();

      MSALPerUserSqlTokenCacheProvider sqlCache = new MSALPerUserSqlTokenCacheProvider(app.UserTokenCache, db, ClaimsPrincipal.Current);

      var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext as System.IdentityModel.Tokens.BootstrapContext;

      UserAssertion userAssertion = new UserAssertion(bootstrapContext.Token, "urn:ietf:params:oauth:grant-type:jwt-bearer");

      AuthenticationResult result = await app.AcquireTokenOnBehalfOf(scopes, userAssertion)
         .ExecuteAsync();

      string accessToken = result.AccessToken;
      if (accessToken == null)
      {
         throw new Exception("Access Token could not be acquired.");
      }

      string requestUrl = String.Format(CultureInfo.InvariantCulture, graphUserUrl, HttpUtility.UrlEncode(tenant));
      HttpClient client = new HttpClient();
      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
      HttpResponseMessage response = await client.SendAsync(request);

      if (response.IsSuccessStatusCode)
      {
         string responseString = await response.Content.ReadAsStringAsync();
         profile = JsonConvert.DeserializeObject<UserProfile>(responseString);
         return (profile);
      }

      throw new Exception("An unexpected error occurred calling the Graph API.");
   }
   catch (Exception ex)
   {
      throw ex;
   }
}
```

Important things to notice:

- We are using the scope `user.read` to get the user's profile on **Microsoft Graph**.
- The `ConfidentialClientApplication` is built using the **Build Patten** introduced on MSAL v3.x, passing the `clientId`, `authority`, `appKey` and `redirectUri` on the builder. All of these values are related to the **TodoListService**. We don't send anything related to the **TodoListClient** application here.
- We hook the `ConfidentialClientApplication` `UserTokenCache` on our `MSALPerUserSqlTokenCacheProvider`, so we can store the cache on the database. Other alternatives for cache storage could be `InMemory` or `Session`.
- We instantiate a `UserAssertion` using the `Bearer` token sent by the client and `urn:ietf:params:oauth:grant-type:jwt-bearer` as assertion type ([read more here](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-on-behalf-of-flow)). This class represents the credential of the user being impersonated.
- The method `AcquireTokenOnBehalfOf` will try to get a token for the impersonated user. If all the validations pass and the impersonated user have consented the requested scope (`user.read` on our sample), an access token will be returned and be used on **Microsoft Graph** request **on behalf on the user**.

### TodoListSPA

The project **TodoListSPA** has the same role as **TodoListClient**. It represents the UI part of the flow, where users can interact with and consumes **TodoListService** Web API. This project is using [MSAL.js](https://github.com/AzureAD/microsoft-authentication-library-for-js) and uses the method `loginPopup` to establish a user context.

The first key point in this project is the `UserAgentApplication` instantiation. Here is the code:

```javascript
var config = {
    popUp: true,
    webApiScope: "https://<yourTenant>/TodoListService-OBO/.default",
    auth: {
       authority: "https://login.microsoftonline.com/<yourTenant>",
       clientId: "<TodoListSPA_ClientId>",
       redirectUri: "http://localhost:16969/",
    },
    cache: {
       cacheLocation: "localStorage",
       storeAuthStateInCookie: true
    }
}

const clientApplication = new Msal.UserAgentApplication(config);
```

Important things to notice:

- We are using the [ConfigurationOptions](https://github.com/AzureAD/microsoft-authentication-library-for-js/wiki/MSAL.js-1.0.0-api-release#configuration-options) introduced on [MSAL.js](https://github.com/AzureAD/microsoft-authentication-library-for-js) to initialize the client application.
- The `config` is found at `appconfig.js` file.
- We are using `localStorage` to store the cache. The other option would be `sessionStorage`.

#### SignIn

The entry point in this project, is the method `displayTodoList()`, triggered when the user clicks on the **Display the todo list** button. This method checks if the user is logged and prompts the login screen in case they are not. The `config` is found at `appconfig.js` file.

```javascript
const loginRequest = {
    scopes: [config.webApiScope],
    prompt: "select_account",
}

function displayTodoList() {
    var account = clientApplication.getAccount();

    if (account) {
       onLogin(null, account);
    }
    else {
       clientApplication.loginPopup(loginRequest).then(function (loginResponse) {
          if (loginResponse.account) {
             onLogin(null, loginResponse.account);
          }
       });
    }
}
```

Important things to notice:

- To find out if the user is logged in, we use the method `getAccount()` which returns an object [Account](https://htmlpreview.github.io/?https://raw.githubusercontent.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-core/docs/classes/_useragentapplication_.useragentapplication.html#getaccount) when the user is logged in.
- To prompt the login screen, we call the method `loginPopup()` passing the `loginRequest` object for configuration. The method returns an [AuthResponse promise](https://github.com/AzureAD/microsoft-authentication-library-for-js/wiki/MSAL.js-1.0.0-api-release#configuration-options). Another option to prompt it is using `loginRedirect()`, however this method requires a slightly different configuration. You can read more about these two methods on [MSAL.js documentation](https://github.com/AzureAD/microsoft-authentication-library-for-js).

#### Calling TodoListService WebAPI

Once the user have logged in, they can call our **TodoListService** Web API sending the `Bearer` token on the HTTP header. Here is the code:

```javascript
const accessTokenRequest = {
    scopes: [config.webApiScope]
}

function acquireAnAccessTokenAndCallTheProtectedService() {
    clientApplication.acquireTokenSilent(accessTokenRequest)
       .then(response => {
          callServiceWithToken(response.accessToken, webApiConfig.resourceBaseAddress + "api/todolist");
       })
       .catch(err => {
          if (err.name === "InteractionRequiredAuthError") {
             clientApplication.acquireTokenPopup(accessTokenRequest).then(
                function (response) {
                   callServiceWithToken(response.accessToken, webApiConfig.resourceBaseAddress + "api/todolist");
                }).catch(function (error) {
                   console.log(error);
                });
          } else {
             showError("acquireToken", err.errorMessage);
          }
       });
}

function callServiceWithToken(token, endpoint) {
    var headers = new Headers();
    var bearer = "Bearer " + token;
    headers.append("Authorization", bearer);
    var options = {
       method: "GET",
       headers: headers
    };

    // Note that fetch API is not available in all browsers
    fetch(endpoint, options)
       .then(function (response) {
          var contentType = response.headers.get("content-type");
          if (response.status === 200 && contentType && contentType.indexOf("application/json") !== -1) {
             // Case where we got the content from the API (as JSon)
             response.json()
                .then(function (data) {
                   // Display response in the page
                })
                .catch(function (error) {
                   showError(endpoint, error);
                });
          } else if (response.status === 403 && contentType && contentType.indexOf("text/plain; charset=utf-8") !== -1) {
             response.text()
                .then(function (data) {
                   var claims = data;
                   clientApplication.acquireTokenPopup(claims).then(function (response) {
                      onAccessToken(null, response.accessToken, null);
                   }).catch(function (error) {
                      console.log(error);
                   });
                })
                .catch(function (error) {
                   showError(endpoint, error);
                });
          } else {
             response.text()
                .then(function (data) {
                   // Display response in the page
                   showError(endpoint, data);
                })
                .catch(function (error) {
                   showError(endpoint, error);
                });
          }
       })
       .catch(function (error) {
          showError(endpoint, error);
       });
}
```

## How to deploy this sample to Azure

This project has two WebApp / Web API projects. To deploy them to Azure Web Sites, you'll need, for each one, to:

- create an Azure Web Site
- publish the Web App / Web APIs to the web site, and
- update its client(s) to call the web site instead of IIS Express.

### Create and Publish the `TodoListService-OBO` to an Azure Web Site

1. Sign in to the [Azure portal](https://portal.azure.com).
2. Click **Create a resource** in the top left-hand corner, select **Web + Mobile** --> **Web App**, select the hosting plan and region, and give your web site a name, for example, `TodoListService-OBO-contoso.azurewebsites.net`.  Click Create Web Site.
3. Once the web site is created, click on it to manage it.  For this set of steps, download the publish profile by clicking **Get publish profile** and save it.  Other deployment mechanisms, such as from source control, can also be used.
4. Switch to Visual Studio and go to the TodoListService project.  Right click on the project in the Solution Explorer and select **Publish**.  Click **Import Profile** on the bottom bar, and import the publish profile that you downloaded earlier.
5. Click on **Settings** and in the `Connection tab`, update the Destination URL so that it is https, for example [https://TodoListService-OBO-contoso.azurewebsites.net](https://TodoListService-OBO-contoso.azurewebsites.net). Click Next.
6. On the Settings tab, make sure `Enable Organizational Authentication` is NOT selected.  Click **Save**. Click on **Publish** on the main screen.
7. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.

### Update the Active Directory tenant application registration for `TodoListService-OBO`

1. Navigate to the [Azure portal](https://portal.azure.com).
2. On the top bar, click on your account and under the **Directory** list, choose the Active Directory tenant containing the `TodoListService-OBO` application.
3. On the applications tab, select the `TodoListService-OBO` application.
4. From the Settings -> Reply URLs menu, update the Sign-On URL, and Reply URL fields to the address of your service, for example [https://TodoListService-OBO-contoso.azurewebsites.net](https://TodoListService-OBO-contoso.azurewebsites.net). Save the configuration.

### Update the `TodoListClient-OBO` to call the `TodoListService-OBO` Running in Azure Web Sites

1. In Visual Studio, go to the `TodoListClient-OBO` project.
2. Open `TodoListClient\App.Config`.  Only one change is needed - update the `todo:TodoListBaseAddress` key value to be the address of the website you published,
   for example, [https://TodoListService-OBO-contoso.azurewebsites.net](https://TodoListService-OBO-contoso.azurewebsites.net).
3. Run the client! If you are trying multiple different client types (for example, .Net, Windows Store, Android, iOS) you can have them all call this one published web API.

### Update the `TodoListSPA-OBO` to call the `TodoListService-OBO` Running in Azure Web Sites

1. In Visual Studio, go to the `TodoListSPA-OBO` project.
2. Open `TodoListSPA\appconfig.js`.  Only one change is needed - update the `todo:TodoListBaseAddress` key value to be the address of the website you published,
   for example, [https://TodoListService-OBO-contoso.azurewebsites.net](https://TodoListService-OBO-contoso.azurewebsites.net).
3. Run the client! If you are trying multiple different client types (for example, .Net, Windows Store, Android, iOS) you can have them all call this one published web API.

### Create and Publish the `TodoListSPA-OBO` to an Azure Web Site

1. Sign in to the [Azure portal](https://portal.azure.com).
2. Click **Create a resource** in the top left-hand corner, select **Web + Mobile** --> **Web App**, select the hosting plan and region, and give your web site a name, for example, `TodoListSPA-OBO-contoso.azurewebsites.net`.  Click Create Web Site.
3. Once the web site is created, click on it to manage it.  For this set of steps, download the publish profile by clicking **Get publish profile** and save it.  Other deployment mechanisms, such as from source control, can also be used.
4. Switch to Visual Studio and go to the TodoListService project.  Right click on the project in the Solution Explorer and select **Publish**.  Click **Import Profile** on the bottom bar, and import the publish profile that you downloaded earlier.
5. Click on **Settings** and in the `Connection tab`, update the Destination URL so that it is https, for example [https://TodoListSPA-OBO-contoso.azurewebsites.net](https://TodoListSPA-OBO-contoso.azurewebsites.net). Click Next.
6. On the Settings tab, make sure `Enable Organizational Authentication` is NOT selected.  Click **Save**. Click on **Publish** on the main screen.
7. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.

### Update the Active Directory tenant application registration for `TodoListSPA-OBO`

1. Navigate to the [Azure portal](https://portal.azure.com).
2. On the top bar, click on your account and under the **Directory** list, choose the Active Directory tenant containing the `TodoListSPA-OBO` application.
3. On the applications tab, select the `TodoListSPA-OBO` application.
4. From the Settings -> Reply URLs menu, update the Sign-On URL, and Reply URL fields to the address of your service, for example [https://TodoListSPA-OBO-contoso.azurewebsites.net](https://TodoListSPA-OBO-contoso.azurewebsites.net). Save the configuration.

## How To Recreate This Sample

First, in Visual Studio 2017 create an empty solution to host the  projects. Then, follow these steps to create each project.

### Creating the TodoListService Project

1. In Visual Studio 2017, create a new `Visual C#` `ASP.NET Web Application (.NET Framework)` project. In the next screen, choose the `Web API` project template.  And while on this screen, click the Change Authentication button, select 'Work or School Accounts', 'Cloud - Single Organization', enter the name of your Azure AD tenant.  You will be prompted to sign in to your Azure AD tenant.  NOTE:  You must sign in with a user that is in the tenant; you cannot, during this step, sign in with a Microsoft account.
2. Add the Active Directory Authentication Library (ADAL) NuGet, Microsoft.IdentityModel.Clients.ActiveDirectory, EntityFramework, and Microsoft.AspNet.WebApi.Cors to the project.
3. Add reference of the `System.IdentityModel` assembly in the project.
4. In the `Models` folder, add a new class called `TodoItem.cs`.  Copy the implementation of TodoItem from this sample into the class.
5. In the `Models` folder, add a new class called `UserProfile.cs`.  Copy the implementation of UserProfile from this sample into the class.
6. Create a new folder named `DAL`.In the `DAL` folder, add a new class called `DbTokenCache.cs`.  Copy the implementation of DbTokenCache from this sample into the class.
7. In the `DAL` folder, add a new class called `TodoListServiceContext.cs`.  Copy the implementation of TodoListServiceContext from this sample into the class.
8. Add a new class named `Extensions` in the project. Replace the implementation with the contents of the file of the same name from the sample.
9. Add a new, empty, Web API 2 Controller called `TodoListController`.
10. Copy the implementation of the TodoListController from this sample into the controller.  Don't forget to add the `[Authorize]` attribute to the class.
11. In `web.config` make sure that the key `ida:AADInstance`, `ida:Tenant`, `ida:ClientID`, and `ida:AppKey` exist, and are populated.  For the global Azure cloud, the value of `ida:AADInstance` is `https://login.onmicrosoft.com/{0}`.
12. In `web.config`, in `<appSettings>`, create keys for `ida:GraphResourceId` and `ida:GraphUserUrl` and set the values accordingly.  For the global Azure AD, the value of `ida:GraphResourceId` is `https://graph.microsoft.com`, and the value of `ida:GraphUserUrl` is `https://graph.microsoft.com/v1.0/me/`.

### Creating the TodoListClient Project

1. In the solution, create a new Windows --> Windows Classic Desktop -> WPF App(.NET Framework)  called TodoListClient.
2. Add the Active Directory Authentication Library (ADAL) NuGet, Microsoft.IdentityModel.Clients.ActiveDirectory to the project.
3. Add  assembly references to `System.Net.Http`, `System.Web.Extensions`, and `System.Configuration`.
4. Add a new class to the project called `TodoItem.cs`.  Copy the code from the sample project file of the same name into this class, completely replacing the code in the file in the new project.
5. Add a new class to the project called `FileCache.cs`.  Copy the code from the sample project file of the same name into this class, completely replacing the code in the file in the new project.
6. Copy the markup from `MainWindow.xaml' in the sample project into the file of the same name in the new project, completely replacing the markup in the file in the new project.
7. Copy the code from `MainWindow.xaml.cs` in the sample project into the file of the same name in the new project, completely replacing the code in the file in the new project.
8. In `app.config` create keys for `ida:AADInstance`, `ida:Tenant`, `ida:ClientId`, `ida:RedirectUri`, `todo:TodoListResourceId`, and `todo:TodoListBaseAddress` and set them accordingly.  For the global Azure cloud, the value of `ida:AADInstance` is `https://login.onmicrosoft.com/{0}`.

Finally, in the properties of the solution itself, set both projects as startup projects.

## Community Help and Support

Use [Stack Overflow](http://stackoverflow.com/questions/tagged/adal) to get support from the community.
Ask your questions on Stack Overflow first and browse existing issues to see if someone has asked your question before.
Make sure that your questions or comments are tagged with [`adal` `dotnet`].

If you find a bug in the sample, please raise the issue on [GitHub Issues](../../issues).

To provide a recommendation, visit the following [User Voice page](https://feedback.azure.com/forums/169401-azure-active-directory).

## Contributing

If you'd like to contribute to this sample, see [CONTRIBUTING.MD](/CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## More information

For more information, see ADAL.NET's conceptual documentation:

- [Recommended pattern to acquire a token](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token#recommended-pattern-to-acquire-a-token)
- [Acquiring tokens interactively in public client applications](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Acquiring-tokens-interactively---Public-client-application-flows)
- [Service to service calls on behalf of the user](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Service-to-service-calls-on-behalf-of-the-user).
- [Customizing Token cache serialization](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Token-cache-serialization)

For more information about how OAuth 2.0 protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).
