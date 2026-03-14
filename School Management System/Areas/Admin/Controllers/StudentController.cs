
namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Admin")]
    public class StudentController : Controller
    {
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Class> _classRepo;
        private readonly IRepository<ClassEnrollment> _enrollmentRepo;

        public StudentController(
            IRepository<Student> studentRepo,
            IRepository<Class> classRepo,
            IRepository<ClassEnrollment> enrollmentRepo)
        {
            _studentRepo = studentRepo;
            _classRepo = classRepo;
            _enrollmentRepo = enrollmentRepo;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var students = await _studentRepo.GetAsync(
                includeProperties: source => source
                    .Include(s => s.ClassEnrollments)
                        .ThenInclude(ce => ce.Class)
                    .Include(s => s.Attendances)
                    .Include(s => s.ExamResults)
                        .ThenInclude(er => er.Exam),
                tracked: false,
                cancellationToken: cancellationToken);

            var studentVMs = students.Select(s => new StudentVM
            {
                Id = s.Id,
                FirstName = s.FirstName ?? "",
                LastName = s.LastName ?? "",
                FullName = $"{s.FirstName ?? ""} {s.LastName ?? ""}".Trim(),
                Email = s.Email ?? "",
                Phone = s.Phone ?? "",
                ClassName = s.ClassEnrollments != null && s.ClassEnrollments.Any(ce => ce.Status)
                    ? $"{s.ClassEnrollments.First(ce => ce.Status).Class.Name} - {s.ClassEnrollments.First(ce => ce.Status).Class.Section}"
                    : "غير مسجل"
            }).ToList();

            return View(studentVMs);
        }


        private async Task LoadClasses(CancellationToken ct)
        {
            ViewBag.Classes = (await _classRepo.GetAsync(tracked: false, cancellationToken: ct)).ToList();
        }

        // ================= ADD GET =================
        public async Task<IActionResult> Add(CancellationToken ct)
        {
            await LoadClasses(ct);
            return View(new AddStudentVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddStudentVM model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadClasses(ct);
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "كلمة السر غير متطابقة");
                await LoadClasses(ct);
                return View(model);
            }

            // Check email exists
            var emailExists = await _studentRepo.GetOneAsync(s => s.Email == model.Email, cancellationToken: ct);
            if (emailExists != null)
            {
                ModelState.AddModelError("Email", "الإيميل موجود مسبقاً");
                await LoadClasses(ct);
                return View(model);
            }

            if (model.ClassId <= 0)
            {
                ModelState.AddModelError("ClassId", "يرجى اختيار صف");
                await LoadClasses(ct);
                return View(model);
            }

            var student = new Student
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                BD = model.BD,
                Email = model.Email,
                Password = model.Password,
                Phone = model.Phone
            };

            // Save student first
            await _studentRepo.CreateAsync(student, ct);
            await _studentRepo.CommitAsync(ct);

            // Save enrollment
            var enrollment = new ClassEnrollment
            {
                StudentId = student.Id,
                ClassId = model.ClassId,
                EnrollmentDate = DateTime.Now,
                Status = true
            };

            await _enrollmentRepo.CreateAsync(enrollment, ct);
            await _enrollmentRepo.CommitAsync(ct);

            TempData["success"] = "تم إضافة الطالب بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT GET =================
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var student = await _studentRepo.GetOneAsync(
                s => s.Id == id,
                includeProperties: q => q.Include(s => s.ClassEnrollments),
                tracked: true,
                cancellationToken: cancellationToken);

            if (student == null) return NotFound();

            var model = new AddStudentVM
            {
                Id = student.Id,
                FirstName = student.FirstName ?? "",
                LastName = student.LastName ?? "",
                Email = student.Email ?? "",
                Phone = student.Phone,
                BD = student.BD,
                ClassId = student.ClassEnrollments?.FirstOrDefault(e => e.Status)?.ClassId ?? 0
            };

            await LoadClasses(cancellationToken);
            return View(model);
        }

        // ================= EDIT POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AddStudentVM model, CancellationToken cancellationToken)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadClasses(cancellationToken);
                return View(model);
            }

            // Check email conflict
            var emailConflict = await _studentRepo.GetOneAsync(
                s => s.Email == model.Email && s.Id != id,
                cancellationToken: cancellationToken);

            if (emailConflict != null)
            {
                ModelState.AddModelError("Email", "الإيميل مستخدم من طالب آخر");
                await LoadClasses(cancellationToken);
                return View(model);
            }

            var student = await _studentRepo.GetOneAsync(
                s => s.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (student == null) return NotFound();

            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            student.Email = model.Email;
            student.Phone = model.Phone;
            student.BD = model.BD;

            // Handle enrollment update
            var activeEnrollment = student.ClassEnrollments?.FirstOrDefault(e => e.Status);

            if (activeEnrollment != null)
            {
                if (model.ClassId > 0 && activeEnrollment.ClassId != model.ClassId)
                {
                    activeEnrollment.ClassId = model.ClassId;
                    _enrollmentRepo.Update(activeEnrollment);
                }
            }
            else if (model.ClassId > 0)
            {
                await _enrollmentRepo.CreateAsync(new ClassEnrollment
                {
                    StudentId = id,
                    ClassId = model.ClassId,
                    EnrollmentDate = DateTime.Now,
                    Status = true
                }, cancellationToken);
            }

            _studentRepo.Update(student);
            await _studentRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم تعديل بيانات الطالب بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var student = await _studentRepo.GetOneAsync(
                s => s.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (student == null)
                return Json(new { success = false, message = "الطالب غير موجود" });

            try
            {
                // Delete enrollments first
                var enrollments = await _enrollmentRepo.GetAsync(
                    e => e.StudentId == id,
                    tracked: true,
                    cancellationToken: cancellationToken);

                foreach (var e in enrollments)
                    _enrollmentRepo.Delete(e);
                await _enrollmentRepo.CommitAsync(cancellationToken);

                _studentRepo.Delete(student);
                await _studentRepo.CommitAsync(cancellationToken);

                TempData["success"] = "تم حذف الطالب بنجاح";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في الحذف: " + ex.Message });
            }
        }

        // ================= DETAILS =================
        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var student = await _studentRepo.GetOneAsync(
                s => s.Id == id,
                includeProperties: q => q
                    .Include(s => s.ClassEnrollments)
                    .ThenInclude(e => e.Class)
                    .Include(s => s.Attendances)
                    .Include(s => s.ExamResults)
                    .ThenInclude(er => er.Exam),
                tracked: false,
                cancellationToken: cancellationToken);

            if (student == null) return NotFound();

            var vm = new StudentDetailsVM
            {
                Id = student.Id,
                FirstName = student.FirstName ?? "",
                LastName = student.LastName ?? "",
                Email = student.Email ?? "",
                Phone = student.Phone,
                BirthDate = student.BD,
                Classes = student.ClassEnrollments?
                    .Where(e => e.Status)
                    .Select(e => $"{e.Class.Name} - {e.Class.Section}")
                    .ToList() ?? new List<string>(),
                AttendanceCount = student.Attendances?.Count() ?? 0,
                ExamResultsCount = student.ExamResults?.Count() ?? 0
            };

            return View(vm);
        }
    }
}
