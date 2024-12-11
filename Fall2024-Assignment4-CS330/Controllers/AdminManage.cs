using Microsoft.AspNetCore.Mvc;
using Fall2024_Assignment4_CS330.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Fall2024_Assignment4_CS330.Controllers
{
    public class AdminManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminManageController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: AdminManage/Index
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Denied", "Home");
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var userType = claims.FirstOrDefault(c => c.Type == "UserType")?.Value;

            // Check if the user is a "Pro"
            if (userType != "Pro")
            {
                return RedirectToAction("Denied", "Home"); // Redirect to Access Denied page
            }
            // Fetch all users
            var users = _userManager.Users.ToList();

            // Populate the list and sort by usertype then username
            var adminmanageVM = new AdminManageVM
            {
                Users = users
                    .OrderByDescending(u => u.UserType)
                    .ThenByDescending(u => u.UserName) // Secondary sorting for ties
                    .ToList()
            };

            // Pass the model to the view
            return View(adminmanageVM);
        }
        //GET
        [HttpPost]
        public async Task<IActionResult> UpdateType(string targetUserId)
        {
            // Get the ID of the user performing the action
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            if (currentUser == null)
            {
                
                return RedirectToAction("Index", "AdminManage");
            }

            // Verify the current user has 'Pro' UserType
            var currentUserClaims = await _userManager.GetClaimsAsync(currentUser);
            var currentUserTypeClaim = currentUserClaims.FirstOrDefault(c => c.Type == "UserType");

            if (currentUserTypeClaim == null || currentUserTypeClaim.Value != "Pro")
            {
                
                return RedirectToAction("Index", "AdminManage");
            }

            // Get the user to be updated
            var targetUser = await _userManager.FindByIdAsync(targetUserId);

            if (targetUser == null)
            {
                
                return RedirectToAction("Index", "AdminManage");
            }

            // Retrieve and modify the target user's UserType claim
            var targetUserClaims = await _userManager.GetClaimsAsync(targetUser);
            var targetUserTypeClaim = targetUserClaims.FirstOrDefault(c => c.Type == "UserType");

            if (targetUserTypeClaim != null)
            {
                await _userManager.RemoveClaimAsync(targetUser, targetUserTypeClaim);
            }

            var newType = targetUserTypeClaim?.Value == "Standard" ? "Pro" : "Standard";
            await _userManager.AddClaimAsync(targetUser, new Claim("UserType", newType));

            // Update the database if UserType is stored there
            targetUser.UserType = newType; // Assuming UserType is a property of your ApplicationUser class
            var result = await _userManager.UpdateAsync(targetUser);

            if (!result.Succeeded)
            {
               
                return RedirectToAction("Index", "AdminManage");
            }

           
            return RedirectToAction("Index", "AdminManage");
        }

    }
}
