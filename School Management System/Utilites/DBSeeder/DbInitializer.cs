 

namespace School.Utilites.DBSeeder
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DbInitializer> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public DbInitializer(
            ApplicationDbContext context,
            ILogger<DbInitializer> logger,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public void Initialize()
        {
            try
            {
                // ================= MIGRATIONS =================
                if (_context.Database.GetPendingMigrations().Any())
                {
                    _context.Database.Migrate();
                }

                // ================= ROLES =================
                CreateRoleIfNotExists(SD.SUPER_ADMIN_ROLE);
                CreateRoleIfNotExists(SD.ADMIN_ROLE);
                CreateRoleIfNotExists(SD.CUSTOMER_ROLE);
                CreateRoleIfNotExists(SD.EMPLOYEE_ROLE);

                // ================= SUPER ADMIN =================
                var user = _userManager.FindByEmailAsync("superadmin@eraasoft.com")
                                       .GetAwaiter().GetResult();

                if (user == null)
                {
                    var newUser = new ApplicationUser
                    {
                        Email = "superadmin@eraasoft.com",
                        UserName = "SuperAdmin",
                        FirstName = "SuperAdmin",
                        EmailConfirmed = true,
                    };

                    var result = _userManager.CreateAsync(newUser, "Admin123#")
                                             .GetAwaiter().GetResult();

                    if (result.Succeeded)
                    {
                        _userManager.AddToRoleAsync(newUser, SD.SUPER_ADMIN_ROLE)
                                    .GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Seeder Error: {ex.Message}");
            }
        }

        // ================= HELPER =================
        private void CreateRoleIfNotExists(string roleName)
        {
            if (!_roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(roleName))
                            .GetAwaiter().GetResult();
            }
        }
    }
}