using Microsoft.Identity.Client;
using System;
using System.Security.Claims;

namespace TodoListService.Utils
{
	/// <summary>
	/// Extensions around ClaimsPrincipal.
	/// </summary>
	public static class ClaimsPrincipalExtensions
	{
		/// <summary>
		/// Get the Account identifier for an MSAL.NET account from a ClaimsPrincipal
		/// </summary>
		/// <param name="claimsPrincipal">Claims principal</param>
		/// <returns>A string corresponding to an account identifier as defined in <see cref="AccountId.Identifier"/></returns>
		public static string GetMsalAccountId(this ClaimsPrincipal claimsPrincipal)
		{
			string userObjectId = claimsPrincipal.GetObjectId();
			string tenantId = claimsPrincipal.GetTenantId();

			if (!string.IsNullOrWhiteSpace(userObjectId) && !string.IsNullOrWhiteSpace(tenantId))
			{
				return $"{userObjectId}.{tenantId}";
			}

			return null;
		}

		/// <summary>
		/// Get the unique object ID associated with the claimsPrincipal
		/// </summary>
		/// <param name="claimsPrincipal">Claims principal from which to retrieve the unique object id</param>
		/// <returns>Unique object ID of the identity, or <c>null</c> if it cannot be found</returns>
		public static string GetObjectId(this ClaimsPrincipal claimsPrincipal)
		{
			var objIdclaim = claimsPrincipal.FindFirst(ClaimConstants.ObjectId);

			if (objIdclaim == null)
			{
				objIdclaim = claimsPrincipal.FindFirst(ClaimConstants.OidKey);
			}

			return objIdclaim != null ? objIdclaim.Value : string.Empty;
		}

		/// <summary>
		/// Tenant ID of the identity
		/// </summary>
		/// <param name="claimsPrincipal">Claims principal from which to retrieve the tenant id</param>
		/// <returns>Tenant ID of the identity, or <c>null</c> if it cannot be found</returns>
		public static string GetTenantId(this ClaimsPrincipal claimsPrincipal)
		{
			var tenantIdclaim = claimsPrincipal.FindFirst(ClaimConstants.TenantId);

			if (tenantIdclaim == null)
			{
				tenantIdclaim = claimsPrincipal.FindFirst(ClaimConstants.TidKey);
			}

			return tenantIdclaim != null ? tenantIdclaim.Value : string.Empty;
		}

		/// <summary>
		/// Gets the domain-hint associated with an identity
		/// </summary>
		/// <param name="claimsPrincipal">Identity for which to compte the domain-hint</param>
		/// <returns>domain-hint for the identity, or <c>null</c> if it cannot be found</returns>
		public static string GetDomainHint(this ClaimsPrincipal claimsPrincipal)
		{
			// Tenant for MSA accounts
			const string msaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

			var tenantId = GetTenantId(claimsPrincipal);

			string domainHint = string.IsNullOrWhiteSpace(tenantId) ? null 
				: tenantId.Equals(msaTenantId, StringComparison.OrdinalIgnoreCase) ? "consumers" : "organizations";

			return domainHint;
		}

		/// <summary>
		/// Builds a ClaimsPrincipal from an IAccount
		/// </summary>
		/// <param name="account">The IAccount instance.</param>
		/// <returns>A ClaimsPrincipal built from IAccount</returns>
		public static ClaimsPrincipal ToClaimsPrincipal(this IAccount account)
		{
			if (account != null)
			{
				var identity = new ClaimsIdentity();
				identity.AddClaim(new Claim(ClaimConstants.ObjectId, account.HomeAccountId.ObjectId));
				identity.AddClaim(new Claim(ClaimConstants.TenantId, account.HomeAccountId.TenantId));
				identity.AddClaim(new Claim(ClaimTypes.Upn, account.Username));
				return new ClaimsPrincipal(identity);
			}

			return null;
		}
	}
}