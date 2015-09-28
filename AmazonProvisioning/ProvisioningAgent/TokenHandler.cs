//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens;
    using System.Linq;
    using System.Security.Claims;

    public class TokenHandler : JwtSecurityTokenHandler
    {
        private const string ArgumentNameSecurityToken = "securityToken";
        private const string ArgumentNameValidationParameters = "validationParameters";

        private const string AuthorizedClientApplicationIdentifier = "00000014-0000-0000-c000-000000000000";

        private const string ClaimTypeClientApplicationIdentifier = "appid";

        private static readonly Lazy<IReadOnlyCollection<string>> AuthorizedClientApplicationIdentifiers =
            new Lazy<IReadOnlyCollection<string>>(
                () =>
                    new string[]
                    {
                        TokenHandler.AuthorizedClientApplicationIdentifier
                    });

        private static bool ClaimsApplicationIdentifier(Claim claim)
        {
            if (null == claim)
            {
                return false;
            }

            bool result =
                string.Equals(
                    TokenHandler.ClaimTypeClientApplicationIdentifier,
                    claim.Type,
                    StringComparison.OrdinalIgnoreCase);
            return result;
        }

        private static bool ClaimsAuthorizedClient(Claim claim, TokenValidationParameters tokenValidationParameters)
        {
            if (null == claim)
            {
                return false;
            }

            if (!TokenHandler.ClaimFromValidIssuer(claim, tokenValidationParameters))
            {
                return false;
            }

            if (!TokenHandler.ClaimsApplicationIdentifier(claim))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                return false;
            }

            bool result =
                TokenHandler
                .AuthorizedClientApplicationIdentifiers
                .Value
                .Any(
                    (string item) =>
                        string.Equals(item, claim.Value, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private static bool ClaimFromValidIssuer(Claim claim, TokenValidationParameters tokenValidationParameters)
        {
            if (null == claim)
            {
                return false;
            }

            if (null == tokenValidationParameters)
            {
                return false;
            }

            if (null == tokenValidationParameters.ValidIssuers)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(claim.Issuer) && string.IsNullOrWhiteSpace(claim.OriginalIssuer))
            {
                return false;
            }

            bool result =
                tokenValidationParameters
                .ValidIssuers
                .Any(
                    (string item) =>
                            string.Equals(item, claim.Issuer, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(item, claim.OriginalIssuer, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private static bool IsAuthorizedClient(ClaimsPrincipal principal, TokenValidationParameters tokenValidationParameters)
        {
            if (null == principal)
            {
                return false;
            }

            if (null == principal.Claims)
            {
                return false;
            }

            bool result =
                principal
                .Claims
                .Any(
                    (Claim item) =>
                        TokenHandler.ClaimsAuthorizedClient(item, tokenValidationParameters));
            return result;
        }

        public override ClaimsPrincipal ValidateToken(
            string securityToken,
            TokenValidationParameters validationParameters,
            out SecurityToken validatedToken)
        {
            if (null == securityToken)
            {
                throw new ArgumentNullException(TokenHandler.ArgumentNameSecurityToken);
            }

            if (null == validationParameters)
            {
                throw new ArgumentNullException(TokenHandler.ArgumentNameValidationParameters);
            }

            ClaimsPrincipal result = base.ValidateToken(securityToken, validationParameters, out validatedToken);
            if (!TokenHandler.IsAuthorizedClient(result, validationParameters))
            {
                throw new SecurityTokenValidationException();
            }

            return result;
        }
    }
}
