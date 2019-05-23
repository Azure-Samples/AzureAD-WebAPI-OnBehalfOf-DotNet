// Configuration of the Azure AD Application for this TodoList Single Page application
// Note that changing popUp to false will produce a completely different UX based on redirects instead of popups.
var config = {
	popUp: true,
	webApiScope: "api://[Enter_client_ID_Of_TodoListService-v2_from_Azure_Portal,_e.g._2ec40e65-ba09-4853-bcde-bcb60029e596]/.default",
	auth: {
		authority: "[https://login.microsoftonline.com/<your_tenant_name>]",
		clientId: "[Enter client ID of the TodoListClient as obtained from Azure Portal, e.g. 82692da5-a86f-44c9-9d53-2f88d52b478b]",
		redirectUri: "https://localhost:44377",
	},
	cache: {
		cacheLocation: "localStorage",
		storeAuthStateInCookie: true
	}
}

// Configuration of the Azure AD Application for the WebAPI called by this single page application (TodoListService)
var webApiConfig = {
	resourceId: "[Enter_App_ID_URI_of_TodoListService,_e.g._https://contoso.onmicrosoft.com/TodoListService]",
    resourceBaseAddress: "https://localhost:44321/",
}
