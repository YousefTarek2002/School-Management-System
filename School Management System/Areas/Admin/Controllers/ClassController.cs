using Microsoft.AspNetCore.Authorization;
using SchoolSystem.Models.VM;

namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Admin")]
    public class ClassController : Controller
    {
        private readonly IRepository<Class> _classRepo;
        private readonly IRepository<Teacher> _teacherRepo;
        private readonly IRepository<ClassEnrollment> _enrollmentRepo;
        private readonly IRepository<Student> _studentRepo;

        public ClassController(
            IRepository<Class> classRepo,
            IRepository<Teacher> teacherRepo,
            IRepository<ClassEnrollment> enrollmentRepo,
            IRepository<Student> studentRepo)
        {
            _classRepo = classRepo;
            _teacherRepo = teacherRepo;
            _enrollmentRepo = enrollmentRepo;
            _studentRepo = studentRepo;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var classes = await _classRepo.GetAsync(
                includeProperties: q => q.Include(c => c.ClassEnrollments)
                                       .ThenInclude(e => e.Student)
                                       .Include(c => c.Teacher),
                tracked: false,
                cancellationToken: cancellationToken);

            var classVMs = classes.Select(c => new ClassVM
            {
                Id = c.Id,
                Name = c.Name ?? "",
                Section = c.Section ?? "",
                TeacherName = c.Teacher?.Name ?? "غير محدد",
                StudentsCount = c.ClassEnrollments?.Count(e => e.Status) ?? 0,
                TotalStudents = c.ClassEnrollments?.Count() ?? 0
            }).ToList();

            return View(classVMs);
        }

        private async Task LoadTeachers(CancellationToken ct)
        {
            ViewBag.Teachers = (await _teacherRepo.GetAsync(tracked: false, cancellationToken: ct)).ToList();
        }

        // ================= ADD =================
        [HttpGet]
        public async Task<IActionResult> Add(CancellationToken cancellationToken)
        {
            await LoadTeachers(cancellationToken);
            return View(new AddClassVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddClassVM model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await LoadTeachers(cancellationToken);
                return View(model);
            }

            // Check name + section unique
            var exists = await _classRepo.GetOneAsync(c =>
                c.Name == model.Name && c.Section == model.Section,
                cancellationToken: cancellationToken);

            if (exists != null)
            {
                ModelState.AddModelError("", "الصف والشعبة موجودين مسبقاً");
                await LoadTeachers(cancellationToken);
                return View(model);
            }

            var classObj = new Class
            {
                Name = model.Name,
                Section = model.Section,
                TeacherId = model.TeacherId
            };

            await _classRepo.CreateAsync(classObj, cancellationToken);
            await _classRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم إضافة الصف بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var classObj = await _classRepo.GetOneAsync(
                c => c.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (classObj == null) return NotFound();

            var model = new AddClassVM
            {
                Id = classObj.Id,
                Name = classObj.Name ?? "",
                Section = classObj.Section ?? "",
                TeacherId = classObj.TeacherId
            };

            await LoadTeachers(cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AddClassVM model, CancellationToken cancellationToken)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadTeachers(cancellationToken);
                return View(model);
            }

            // Check name + section conflict
            var conflict = await _classRepo.GetOneAsync(c =>
                c.Name == model.Name &&
                c.Section == model.Section &&
                c.Id != id,
                cancellationToken: cancellationToken);

            if (conflict != null)
            {
                ModelState.AddModelError("", "الصف والشعبة مستخدمين من صف آخر");
                await LoadTeachers(cancellationToken);
                return View(model);
            }

            var classObj = await _classRepo.GetOneAsync(c => c.Id == id, tracked: true, cancellationToken: cancellationToken);
            if (classObj == null) return NotFound();

            classObj.Name = model.Name;
            classObj.Section = model.Section;
            classObj.TeacherId = model.TeacherId;

            _classRepo.Update(classObj);
            await _classRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم تعديل الصف بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var classObj = await _classRepo.GetOneAsync(
                c => c.Id == id,
                includeProperties: q => q.Include(c => c.ClassEnrollments),
                tracked: true,
                cancellationToken: cancellationToken);

            if (classObj == null)
                return Json(new { success = false, message = "الصف غير موجود" });

            var activeEnrollments = classObj.ClassEnrollments?.Count(e => e.Status) ?? 0;
            if (activeEnrollments > 0)
                return Json(new { success = false, message = $"لا يمكن الحذف - يوجد {activeEnrollments} طالب نشط" });

            try
            {
                // Delete enrollments first
                var enrollments = await _enrollmentRepo.GetAsync(
                    e => e.ClassId == id,
                    tracked: true,
                    cancellationToken: cancellationToken);

                foreach (var enrollment in enrollments)
                    _enrollmentRepo.Delete(enrollment);

                await _enrollmentRepo.CommitAsync(cancellationToken);

                _classRepo.Delete(classObj);
                await _classRepo.CommitAsync(cancellationToken);

                TempData["success"] = "تم حذف الصف بنجاح";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في الحذف: " + ex.Message });
            }
        }

        // ================= DETAILS =================
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var classObj = await _classRepo.GetOneAsync(
                c => c.Id == id,
                includeProperties: q => q
                    .Include(c => c.Teacher)
                    .Include(c => c.ClassEnrollments)
                        .ThenInclude(e => e.Student),
                tracked: false,
                cancellationToken: cancellationToken);

            if (classObj == null) return NotFound();

            // ✅ List<Student> مش IEnumerable
            ViewBag.AllStudents = await _studentRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
            return View(classObj);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudentToClass(AddStudentToClassVM model, CancellationToken cancellationToken)
        {
            if (model.StudentId <= 0 || model.ClassId <= 0)
                return BadRequest();

            // Check if already enrolled
            var exists = await _enrollmentRepo.GetOneAsync(
                e => e.StudentId == model.StudentId && e.ClassId == model.ClassId,
                cancellationToken: cancellationToken);

            if (exists != null)
            {
                TempData["error"] = "الطالب مسجل بالفعل في هذا الصف";
                return RedirectToAction("Details", new { id = model.ClassId });
            }

            var enrollment = new ClassEnrollment
            {
                StudentId = model.StudentId,
                ClassId = model.ClassId,
                EnrollmentDate = DateTime.Now,
                Status = true
            };

            await _enrollmentRepo.CreateAsync(enrollment, cancellationToken);
            await _enrollmentRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم إضافة الطالب للصف بنجاح";
            return RedirectToAction("Details", new { id = model.ClassId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveStudent(int id, int classId, CancellationToken cancellationToken)
        {
            var enrollment = await _enrollmentRepo.GetOneAsync(
                e => e.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (enrollment != null)
            {
                _enrollmentRepo.Delete(enrollment);
                await _enrollmentRepo.CommitAsync(cancellationToken);
                TempData["success"] = "تم إزالة الطالب من الصف";
            }

            return RedirectToAction("Details", new { id = classId });
        }
    }
}
