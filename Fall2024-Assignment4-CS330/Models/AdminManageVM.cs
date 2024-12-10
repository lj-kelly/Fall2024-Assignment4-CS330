using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace Fall2024_Assignment4_CS330.Models
{
    public class AdminManageVM
    {
        private List<ApplicationUser> _users;

        public List<ApplicationUser> Users
        {
            get => _users.OrderByDescending(u => u.UserType).ToList(); // Sort by W/L ratio
            set => _users = value;
        }
        public async Task<AdminManageVM> GetUserListAsync(UserManager<ApplicationUser> userManager)
        {
            var users = userManager.Users.ToList(); // Retrieve all users
            var da_list = new AdminManageVM
            {
                Users = users // Sorting happens automatically in the property
            };
            return da_list;
        }

    }
}
