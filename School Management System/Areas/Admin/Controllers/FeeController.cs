

namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
    public class FeeController : Controller
    {
        private readonly IRepository<Fee> _feeRepo;
        private readonly IRepository<Student> _studentRepo;

        public FeeController(IRepository<Fee> feeRepo, IRepository<Student> studentRepo)
        {
            _feeRepo = feeRepo;
            _studentRepo = studentRepo;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 8,
            string search = "",
            CancellationToken cancellationToken = default)
        {
            var feesData = await _feeRepo.GetAsync(
                includeProperties: q => q
                    .Include(f => f.Student)
                    .Include(f => f.Student.ClassEnrollments)
                        .ThenInclude(ce => ce.Class),
                tracked: false,
                cancellationToken: cancellationToken);

            // ✅ FILTER
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                feesData = feesData
                    .Where(f =>
                        (f.Student.FirstName + " " + f.Student.LastName).ToLower().Contains(search) ||
                        f.Student.Email.ToLower().Contains(search))
                    .ToList();
            }

            // ✅ PAGINATION
            var totalCount = feesData.Count();

            var fees = feesData
                .OrderByDescending(f => f.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ✅ MAPPING
            var feeVMs = fees.Select(f => new FeeVM
            {
                Id = f.Id,
                Amount = f.Amount,
                Paid = f.Paid,
                DueDate = f.DueDate,
                StudentName = $"{f.Student.FirstName} {f.Student.LastName}",

                ClassName = f.Student.ClassEnrollments != null && f.Student.ClassEnrollments.Any(ce => ce.Status)
                    ? $"{f.Student.ClassEnrollments.First(ce => ce.Status).Class.Name} - {f.Student.ClassEnrollments.First(ce => ce.Status).Class.Section}"
                    : "غير مسجل",

                PaymentStatus = f.Paid ? "مدفوع" : "غير مدفوع",
                DaysOverdue = f.Paid ? 0 : (int)(DateTime.Today - f.DueDate).TotalDays
            }).ToList();

            // ✅ VIEWBAG (زي الاتنين بالظبط)
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Search = search;

            return View(feeVMs);
        }

        private async Task LoadStudents(FeeVM vm, CancellationToken cancellationToken)
        {
            var students = (await _studentRepo.GetAsync(tracked: false, cancellationToken: cancellationToken)).ToList();

            vm.Students = students.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.FirstName} {s.LastName}"
            }).ToList();
        }

        // ================= CREATE GET =================
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var vm = new FeeVM { DueDate = DateTime.Today.AddDays(30) };
            await LoadStudents(vm, cancellationToken);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeeVM vm, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await LoadStudents(vm, cancellationToken);
                return View(vm);
            }

            // Check if fee exists for same student same month
            var monthKey = new { vm.StudentId, Month = vm.DueDate.Month, Year = vm.DueDate.Year };
            var exists = await _feeRepo.GetOneAsync(f =>
                f.StudentId == vm.StudentId &&
                f.DueDate.Month == monthKey.Month &&
                f.DueDate.Year == monthKey.Year,
                cancellationToken: cancellationToken);

            if (exists != null)
            {
                ModelState.AddModelError("DueDate", "رسوم لهذا الطالب في نفس الشهر موجودة مسبقاً");
                await LoadStudents(vm, cancellationToken);
                return View(vm);
            }

            var fee = new Fee
            {
                Amount = vm.Amount,
                Paid = vm.Paid,
                DueDate = vm.DueDate,
                StudentId = vm.StudentId
            };

            await _feeRepo.CreateAsync(fee, cancellationToken);
            await _feeRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم إضافة الرسوم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT GET =================
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var fee = await _feeRepo.GetOneAsync(
                f => f.Id == id,
                includeProperties: q => q.Include(f => f.Student),
                tracked: true,
                cancellationToken: cancellationToken);

            if (fee == null) return NotFound();

            var vm = new FeeVM
            {
                Id = fee.Id,
                Amount = fee.Amount,
                Paid = fee.Paid,
                DueDate = fee.DueDate,
                StudentId = fee.StudentId
            };

            await LoadStudents(vm, cancellationToken);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FeeVM vm, CancellationToken cancellationToken)
        {
            if (id != vm.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadStudents(vm, cancellationToken);
                return View(vm);
            }

            var fee = await _feeRepo.GetOneAsync(
                f => f.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (fee == null) return NotFound();

            fee.Amount = vm.Amount;
            fee.Paid = vm.Paid;
            fee.DueDate = vm.DueDate;
            fee.StudentId = vm.StudentId;

            _feeRepo.Update(fee);
            await _feeRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم تعديل الرسوم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var fee = await _feeRepo.GetOneAsync(
                f => f.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (fee == null)
                return Json(new { success = false, message = "الرسوم غير موجودة" });

            try
            {
                _feeRepo.Delete(fee);
                await _feeRepo.CommitAsync(cancellationToken);
                TempData["success"] = "تم حذف الرسوم بنجاح";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في الحذف: " + ex.Message });
            }
        }

        // ================= STUDENT FEES =================
        public async Task<IActionResult> StudentFees(int studentId, CancellationToken cancellationToken)
        {
            var student = await _studentRepo.GetOneAsync(
                s => s.Id == studentId,
                includeProperties: q => q.Include(s => s.ClassEnrollments)
                                       .ThenInclude(ce => ce.Class),
                tracked: false,
                cancellationToken: cancellationToken);

            if (student == null) return NotFound();

            // ✅ بدون orderBy parameter - استخدم OrderBy في الذاكرة
            var fees = await _feeRepo.GetAsync(
                f => f.StudentId == studentId,
                includeProperties: q => q.Include(f => f.Student),
                tracked: false,
                cancellationToken: cancellationToken);

            // ✅ OrderBy في الذاكرة
            var orderedFees = fees.OrderByDescending(f => f.DueDate).ToList();

            var totalDue = orderedFees.Where(f => !f.Paid).Sum(f => f.Amount);
            var totalPaid = orderedFees.Where(f => f.Paid).Sum(f => f.Amount);

            var vm = new StudentFeesVM
            {
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                // ✅ الحل الآمن للـ ClassName
                ClassName = student.ClassEnrollments != null && student.ClassEnrollments.Any(ce => ce.Status)
                    ? $"{student.ClassEnrollments.First(ce => ce.Status).Class.Name} - {student.ClassEnrollments.First(ce => ce.Status).Class.Section}"
                    : "",
                Fees = orderedFees.Select(f => new FeeVM
                {
                    Id = f.Id,
                    Amount = f.Amount,
                    Paid = f.Paid,
                    DueDate = f.DueDate,
                    PaymentStatus = f.Paid ? "مدفوع" : "غير مدفوع"
                }).ToList(),
                TotalDue = totalDue,
                TotalPaid = totalPaid,
                TotalOutstanding = totalDue
            };

            return View(vm);
        }


    }
}
