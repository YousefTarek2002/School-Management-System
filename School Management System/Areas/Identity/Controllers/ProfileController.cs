namespace School.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ===================== GET PROFILE =====================
        public async Task<IActionResult> Index()
        {
            var user = await GetUserAsync();
            if (user is null) return NotFound();

            var vm = MapToVM(user);
            return View(vm);
        }

        // ===================== UPDATE PROFILE =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ApplicationUserVM model)
        {
            var user = await GetUserAsync();
            if (user is null) return NotFound();

            if (!ModelState.IsValid)
                return View("Index", MapToVM(user));

            // 🔥 تقسيم الاسم
            var names = (model.Name ?? "").Trim().Split(' ');

            user.FirstName = names.Length > 0 ? names[0] : "";
            user.LastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "";

            user.PhoneNumber = model.PhoneNumber;

            // لو عندك Address في DB ضيفه هنا
            // user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["error-notification"] =
                    string.Join(", ", result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(Index));
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["success-notification"] = "Profile updated successfully";
            return RedirectToAction(nameof(Index));
        }

        // ===================== CHANGE PASSWORD =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(ApplicationUserVM model)
        {
            var user = await GetUserAsync();
            if (user is null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                string.IsNullOrWhiteSpace(model.NewPassword))
            {
                TempData["error-notification"] = "Please fill all password fields";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword
            );

            if (!result.Succeeded)
            {
                TempData["error-notification"] =
                    string.Join(", ", result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(Index));
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["success-notification"] = "Password changed successfully";
            return RedirectToAction(nameof(Index));
        }

        // ===================== HELPERS =====================
        private async Task<ApplicationUser?> GetUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        private ApplicationUserVM MapToVM(ApplicationUser user)
        {
            return new ApplicationUserVM
            {
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,

                // لو ضفت Address في DB
                // Address = user.Address
            };
        }
    }
}