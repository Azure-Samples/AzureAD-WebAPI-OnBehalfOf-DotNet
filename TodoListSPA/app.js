'use strict';

config.callback = loggedin;
var authenticationContext = new AuthenticationContext(config);

if (authenticationContext.isCallback(window.location.hash)) {
    authenticationContext.handleWindowCallback();
}

if (!config.popUp) {
    if (!authenticationContext.isCallback(window.location.hash)) {
        var user = authenticationContext.getCachedUser();
        if (user && window.parent === window && !window.opener) {
            // Display the user
            displayUserAndShowSignOutButton(user);

            // Call the protected API to show the content of the todo list
            acquireAnAccessTokenAndCallTheProtectedService();
        }
    }
}


/**
 * Entry point: called when the user clicks on the "Display the todo list" button
 */
function displayTodoList() {
    var user = authenticationContext.getCachedUser();
    if (user) {
        onLogin(null, user);
    }
    else {
        showProgress("Getting user's identity");
        authenticationContext.login();
    }
}

/**
 * Entry point: called when the user clicks on the "Logout" button
 */
function signOut() {
    authenticationContext.logOut();
}

/**
 * Callback, triggered after authentication.login() got called and the login interaction
 * with the user happened.
 * @param {string} errorDescription - Description of the error (as returned from the STS)
 * @param {string} idToken - Id token in case of success
 * @param {string} error - Error code
 */
function loggedin(errorDescription, idToken, error) {
    if (error) {
        showError(errorDescription, error);
    }
    else {
        if (config.popUp) {
            authenticationContext.getUser(onLogin);
        }
        // The other case (redirect) is processed in the section guarded by the 
        // authenticationContext.isCallback(window.location.hash) condition (see above)
    }
}

/**
 * Function called when the user is logged-in. It displays the information about the
 * user, and calls the protected Web API to display the list of to do items
 * @param {string} error - Error from the STS
 * @param {any} user - Data about the logged-in user
 */
function onLogin(error, user) {
    if (user) {
        // Show the information about the user
        displayUserAndShowSignOutButton(user);

        // Call the protected API to show the content of the todo list
        acquireAnAccessTokenAndCallTheProtectedService();
    }
    if (error) {
        showError("login", error);
    }
}

/**
 * Displays information about the user and show the signout button
 * @param {any} user - Data about the logged-in user
 */
function displayUserAndShowSignOutButton(user) {
    // Show the user info.
    var userInfoElement = document.getElementById("userInfo");
    userInfoElement.parentElement.classList.remove("hidden");
    userInfoElement.innerHTML = JSON.stringify(user, null, 4);

    // Show the Sign-Out button
    document.getElementById("signOutButton").classList.remove("hidden");
}

/**
 * Acquire an access token and call the Web API.
 * This tries to acquire token from the cache (acquireToken), and if this fails, this will try to
 * acquire token with one of the UI-ed functions acquireTokenPopup or acquireTokenRedirect
 */
function acquireAnAccessTokenAndCallTheProtectedService() {
    showProgress("acquiring an access token for the Web API");
    authenticationContext.acquireToken(webApiConfig.resourceId, function (errorDesc, token, error) {
        if (error) {
            if (config.popUp) {
                authenticationContext.acquireTokenPopup(webApiConfig.resourceId, null, null, onAccessToken);
            }
            else {
                authenticationContext.acquireTokenRedirect(webApiConfig.resourceId, null, null);
            }
        }
        else {
            onAccessToken(errorDesc, token, error);
        }
    });
}

/**
 * Function called when the access token is available. This will call the service
 * @param {string} errorDesc - Error message
 * @param {string} token - Access token to use in the Web API
 * @param {string} error - Error code from the STS
 */
function onAccessToken(errorDesc, token, error) {
    if (error) {
        showError("acquireToken", error);
    }
    if (token) {
        showProgress("calling the Web API with the access token");
        callServiceWithToken(token, webApiConfig.resourceBaseAddress + "api/todolist");
    }
}

/**
 * Calls the service with the access token. Also handles the "claim challenge", that is when the service requires
 * more claims from the users (for instance that the users does two factors authentication)
 * @param {string} token - Access token for the web API
 * @param {string} endpoint - endpoint to the Web API to call
 */
function callServiceWithToken(token, endpoint) {
    // Header won't work in IE, but you could replace it with a call to AJAX JQuery for instance.
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
                        showProgress("");
                        showAPIResponse(data, token);
                    })
                    .catch(function (error) {
                        showError(endpoint, error);
                    });
            } else if (response.status === 403 && contentType && contentType.indexOf("text/plain; charset=utf-8") !== -1) {
                // Claim Challenge: Case where the TotoListService requests that the user presents additional claims, (for instance two factors authentication).
                // We then need another round of UI interaction hence the calls to acquireTokenPopup or acquireTokenRedirect
                response.text()
                    .then(function (data) {
                        // Display response in the page
                        showProgress("Requires additional claims: " + data);
                        var claims = data;
                        if (config.popUp) {
                            authenticationContext.acquireTokenPopup(webApiConfig.resourceId, null, claims, onAccessToken);
                        }
                        else {
                            authenticationContext.acquireTokenRedirect(webApiConfig.resourceId, null, claims);
                        }
                    })
                    .catch(function (error) {
                        showError(endpoint, error);
                    });
            } else {
                // Other cases than 202 with JSon content, or 403 with text content. We suppose this is some Json content.
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

/**
 * Display the response from the Web API (as JSON)
 * @param {any} data - the JSon data
 * @param {any} token - the access token
 */
function showAPIResponse(data, token) {
    var responseElement = document.getElementById("apiResponse");
    responseElement.parentElement.classList.remove("hidden");
    console.log(data);
    responseElement.innerHTML = JSON.stringify(data, null, 4);
}

/**
 * Show an error message in the page
 * @param {any} endpoint - the endpoint used for the error message
 * @param {any} error - the error string
 */
function showError(endpoint, error) {
    var errorElement = document.getElementById("errorMessage");
    console.error(error);
    var formattedError = JSON.stringify(error, null, 4);
    if (formattedError.length < 3) {
        formattedError = error;
    }
    errorElement.innerHTML = "Error calling " + endpoint + ": " + formattedError;
}

/**
 * Show an a text in the page explaining the progress
 * @param {string} text - the text to display
 */
function showProgress(text) {
    var errorElement = document.getElementById("progressMessage");
    errorElement.innerText = text;
}
