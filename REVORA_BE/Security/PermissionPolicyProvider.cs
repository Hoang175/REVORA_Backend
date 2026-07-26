using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace REVORA_BE.Security
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) 
            : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Strict fallback check: if the policy name exists as a statically registered policy, return it immediately
            var policy = await base.GetPolicyAsync(policyName);
            if (policy != null)
            {
                return policy;
            }

            // Granular permission check: Revora permission strings follow 'domain.action[.modifier]' notation (containing a '.')
            // If it doesn't match this structure, we do not attempt to generate it on-the-fly and return null.
            if (!string.IsNullOrEmpty(policyName) && policyName.Contains('.'))
            {
                return new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(policyName))
                    .Build();
            }

            return null;
        }
    }
}
