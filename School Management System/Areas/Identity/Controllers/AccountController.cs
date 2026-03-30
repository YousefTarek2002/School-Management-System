using Microsoft.AspNetCore.Identity.UI.Services;

namespace School.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IRepository<ApplicationUserOTP> _applicationUserOTPRepository;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IRepository<ApplicationUserOTP> applicationUserOTPRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _applicationUserOTPRepository = applicationUserOTPRepository;
        }

        // ===================== LOGOUT =====================
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["success-notification"] = "Logout Successfully";
            return RedirectToAction("Login");
        }

        // ===================== REGISTER =====================
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                FirstName = model.Name?.Split(' ').FirstOrDefault() ?? "",
                LastName = model.Name?.Split(' ').Skip(1).Any() == true ? string.Join(" ", model.Name.Split(' ').Skip(1)) : "",
                Email = model.Email,
                UserName = model.UserName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account",
                new { area = "Identity", id = user.Id, token }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm Your Email",
                $"<h1>Please confirm your email by clicking <a href='{link}'>here</a></h1>");

            await _userManager.AddToRoleAsync(user, SD.ADMIN_ROLE);
            TempData["success-notification"] = "Registration successful, check your email!";
            return RedirectToAction("Login");
        }

        // ===================== CONFIRM EMAIL =====================
        public async Task<IActionResult> ConfirmEmail(string id, string token)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error-notification"] = "User not found";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                TempData["error-notification"] = "Invalid or expired token";
            else
                TempData["success-notification"] = "Email confirmed successfully";

            return RedirectToAction("Login");
        }

        // ===================== LOGIN =====================
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.UserNameOREmail)
                       ?? await _userManager.FindByEmailAsync(model.UserNameOREmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username/email or password");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    ModelState.AddModelError(string.Empty, "Too many attempts, try again after 5 minutes");
                else if (result.IsNotAllowed)
                    ModelState.AddModelError(string.Empty, "Please confirm your email first");
                else
                    ModelState.AddModelError(string.Empty, "Invalid username/email or password");

                return View(model);
            }

            TempData["success-notification"] = "Login successful";
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        // ===================== RESEND EMAIL CONFIRMATION =====================
        public IActionResult ResendEmailConfirmation() => View();

        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.UserNameOREmail)
                       ?? await _userManager.FindByEmailAsync(model.UserNameOREmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username/email");
                return View(model);
            }

            if (user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Email already confirmed");
                return View(model);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = Url.Action(nameof(ConfirmEmail), "Account",
                new { area = "Identity", id = user.Id, token }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Resend Email Confirmation",
                $"<h1>Please confirm your email by clicking <a href='{link}'>here</a></h1>");

            TempData["success-notification"] = "Email sent successfully";
            return RedirectToAction("Login");
        }

        // ===================== FORGOT PASSWORD =====================
        public IActionResult ForgetPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.UserNameOREmail)
                       ?? await _userManager.FindByEmailAsync(model.UserNameOREmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username/email");
                return View(model);
            }

            var otp = new Random().Next(1000, 9999).ToString();

            var userOTPs = await _applicationUserOTPRepository.GetAsync(e => e.ApplicationUserId == user.Id);
            var totalCount = userOTPs.Count(e => (DateTime.UtcNow - e.CreateAt).TotalHours < 24);

            if (totalCount > 5)
            {
                ModelState.AddModelError(string.Empty, "Too many attempts, please try later");
                return View(model);
            }

            await _applicationUserOTPRepository.CreateAsync(new()
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationUserId = user.Id,
                OTP = otp,
                IsValid = true,
                CreateAt = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddMinutes(30)
            }, cancellationToken);

            await _applicationUserOTPRepository.CommitAsync(cancellationToken);

            await _emailSender.SendEmailAsync(user.Email, "Forgot Password OTP",
                $"<h1>Use this OTP: {otp} to validate your account. Do not share it.</h1>");

            TempData["success-notification"] = "OTP sent to your email";
            TempData["From-ForgetPassword"] = Guid.NewGuid().ToString();
            return RedirectToAction("ValidateOTP", new { userId = user.Id });
        }

        // ===================== VALIDATE OTP =====================
        public IActionResult ValidateOTP(string userId)
        {
            if (TempData["From-ForgetPassword"] is null)
                return NotFound();

            return View(new ValidateOTP { UserId = userId });
        }

        [HttpPost]
        public async Task<IActionResult> ValidateOTP(ValidateOTP model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var validOTP = (await _applicationUserOTPRepository.GetAsync(
                e => e.ApplicationUserId == model.UserId && e.IsValid && e.ValidTo > DateTime.UtcNow))
                .OrderByDescending(e => e.CreateAt)
                .FirstOrDefault();

            if (validOTP == null || validOTP.OTP != model.OTP)
            {
                TempData["error-notification"] = "Invalid OTP";
                return RedirectToAction(nameof(ValidateOTP), new { userId = model.UserId });
            }

            TempData["From-ValidateOTP"] = Guid.NewGuid().ToString();
            return RedirectToAction("NewPassword", new { userId = model.UserId });
        }

        // ===================== NEW PASSWORD =====================
        public IActionResult NewPassword(string userId)
        {
            if (TempData["From-ValidateOTP"] is null)
                return NotFound();

            return View(new NewPasswordVM { UserId = userId });
        }

        [HttpPost]
        public async Task<IActionResult> NewPassword(NewPasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["error-notification"] = "User not found";
                return RedirectToAction(nameof(NewPassword), new { userId = model.UserId });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, model.Password);

            TempData["success-notification"] = "Password changed successfully";
            return RedirectToAction("Login");
        }
    }
}