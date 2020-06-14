using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace IdentitySample.Areas.Identity.Pages.Account
{
    public class AccountLinking : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IEmailSender _emailSender;

        public AccountLinking(SignInManager<IdentityUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty] public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData] public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required] [EmailAddress] public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
            
            [Required]
            [HiddenInput]
            public string ProviderKey { get; set; }
        }

        public async Task OnGetAsync(string email, string providerKey, string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            // await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = null; //(await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ViewData["ProviderKey"] = providerKey;
            ViewData["Email"] = email;

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // var info = await _signInManager.GetExternalLoginInfoAsync();
                //
                // if (info == null)
                // {
                //     ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                //     return Page();
                // }

                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(
                    "oidc",
                    Input.ProviderKey,
                    "OpenIdConnect"
                    ));
                    
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, false);
                    // _logger.LogInformation("{Name} logged in with {LoginProvider} provider.",
                    //     info.Principal.Identity.Name, info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }

                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new {ReturnUrl = returnUrl, RememberMe = false});
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}