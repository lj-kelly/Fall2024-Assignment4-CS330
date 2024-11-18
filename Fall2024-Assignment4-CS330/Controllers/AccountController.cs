using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Fall2024_Assignment4_CS330.Models;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Fall2024_Assignment4_CS330.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; // Pass returnUrl to the view
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Your account is not allowed to sign in. Please confirm your email or contact support.");
                return View(model);
            }
            else if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Your account is locked. Please try again later.");
                return View(model);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email};
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add DisplayName as a claim
                    await _userManager.AddClaimAsync(user, new Claim("DisplayName", model.DisplayName));

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: /Account/Delete
        [HttpGet]
        public IActionResult Delete()
        {
            return View();
        }

        // POST: /Account/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get the current user's ID
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    await _signInManager.SignOutAsync(); // Sign out the user
                    return RedirectToAction("Index", "Home"); // Redirect to the home page
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "User not found.");
            }

            return View();
        }

        // GET: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/Manage
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Retrieve DisplayName and Photo (if available)
            var displayNameClaim = await _userManager.GetClaimsAsync(user);
            var displayName = displayNameClaim.FirstOrDefault(c => c.Type == "DisplayName")?.Value;

            var model = new ManageAccountVM
            {
                DisplayName = displayName,
                Email = user.Email,
                // Add the photo if it exists
                Photo = user.Photo != null ? new FormFile(new MemoryStream(user.Photo), 0, user.Photo.Length, "photo", "photo") : null
            };

            return View(model);
        }

        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageAccountVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Handle the DisplayName claim
            var currentClaims = await _userManager.GetClaimsAsync(user);
            var displayNameClaim = currentClaims.FirstOrDefault(c => c.Type == "DisplayName");

            if (displayNameClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, displayNameClaim);
            }

            await _userManager.AddClaimAsync(user, new Claim("DisplayName", model.DisplayName));

            // Handle the photo upload
            if (model.Photo != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await model.Photo.CopyToAsync(memoryStream);
                    user.Photo = memoryStream.ToArray();  // Convert the photo to a byte array
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> IncrementWins()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.GamesWon++;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction("Manage"); // or wherever you want to redirect
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("Error"); // or appropriate error view
        }
    }
}

