namespace School.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null) return NotFound();

            // ✅ Manual Mapping (أفضل من Mapster هنا)
            var userVM = new ApplicationUserVM()
            {
                Name = user.FirstName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(userVM);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ApplicationUserVM userVM)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null) return NotFound();

            // ✅ تعديل البيانات صح
            user.FirstName = userVM.Name;
            user.PhoneNumber = userVM.PhoneNumber;

            // ⚠️ متغيرش Email كدا
            // user.Email = userVM.Email;

            await _userManager.UpdateAsync(user);

            TempData["success-notification"] = "Profile Updated Successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePassword(ApplicationUserVM userVM)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null) return NotFound();

            if (string.IsNullOrEmpty(userVM.CurrentPassword) || string.IsNullOrEmpty(userVM.NewPassword))
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, userVM.CurrentPassword, userVM.NewPassword);

            if (!result.Succeeded)
                TempData["error-notification"] = string.Join(", ", result.Errors.Select(e => e.Description));
            else
                TempData["success-notification"] = "Password Updated Successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}