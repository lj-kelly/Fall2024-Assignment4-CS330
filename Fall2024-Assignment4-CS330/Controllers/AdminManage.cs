using Microsoft.AspNetCore.Mvc;
using Fall2024_Assignment4_CS330.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

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
            // Fetch all users
            var users = _userManager.Users.ToList();

            // Populate the list and sort by usertype then username
            var adminmanageVM = new AdminManageVM
            {
                Users = users.OrderByDescending(u =>
                    u.UserType)
                    .ThenByDescending(u => u.UserName) // Secondary sorting for ties
                    .ToList()
            };

            // Pass the model to the view
            return View(adminmanageVM);
        }
    }
}
