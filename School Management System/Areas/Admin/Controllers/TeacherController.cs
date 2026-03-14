using Microsoft.AspNetCore.Authorization;

namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Admin")]  
    public class TeacherController : Controller
    {
        private readonly IRepository<Teacher> _teacherRepo;
        private readonly IRepository<Subject> _subjectRepo;
        private readonly IRepository<SubjectTeacher> _subjectTeacherRepo;

        public TeacherController(
            IRepository<Teacher> teacherRepo,
            IRepository<Subject> subjectRepo,
            IRepository<SubjectTeacher> subjectTeacherRepo)
        {
            _teacherRepo = teacherRepo;
            _subjectRepo = subjectRepo;
            _subjectTeacherRepo = subjectTeacherRepo;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var teachers = await _teacherRepo.GetAsync(
                includeProperties: q => q.Include(t => t.SubjectTeachers)  // ✅ الـ type الجديد
                                       .ThenInclude(st => st.Subject),
                tracked: false,
                cancellationToken: cancellationToken);

            var teacherVMs = teachers.Select(t => new TeacherVM
            {
                Id = t.Id,
                Name = t.Name,
                Salary = t.Salary,
                Email = t.Email,
                SubjectName = t.SubjectTeachers?.Select(st => st.Subject.Name)
                    .FirstOrDefault() ?? "غير محدد"
            }).ToList();

            return View(teacherVMs);
        }

        [HttpGet]
        public async Task<IActionResult> Add(CancellationToken cancellationToken)
        {
            ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
            return View(new AddTeacherVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddTeacherVM model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "كلمة السر غير متطابقة");
                ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
                return View(model);
            }

            // ✅ Check email exists
            var emailExists = await _teacherRepo.GetOneAsync(t => t.Email == model.Email, cancellationToken: cancellationToken);
            if (emailExists != null)
            {
                ModelState.AddModelError("Email", "الإيميل موجود مسبقاً");
                ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
                return View(model);
            }

            var teacher = new Teacher
            {
                Name = model.Name,
                Salary = model.Salary,
                Email = model.Email,
                Password = model.Password,  // ✅ هتحتاج hash في service layer
                ConfirmPassword = null  // مش محتاج في الـ DB
            };

            await _teacherRepo.CreateAsync(teacher, cancellationToken);
            await _teacherRepo.CommitAsync(cancellationToken);

            // Add SubjectTeacher if subject selected
            if (model.SubjectId > 0)
            {
                await _subjectTeacherRepo.CreateAsync(new SubjectTeacher
                {
                    TeacherId = teacher.Id,
                    SubjectId = model.SubjectId
                }, cancellationToken);
                await _subjectTeacherRepo.CommitAsync(cancellationToken);
            }

            TempData["success"] = "تم إضافة المعلم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var teacher = await _teacherRepo.GetOneAsync(
                t => t.Id == id,
                includeProperties: q => q.Include(t => t.SubjectTeachers).ThenInclude(st => st.Subject),
                tracked: true,
                cancellationToken: cancellationToken);

            if (teacher == null) return NotFound();

            ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);

            var model = new AddTeacherVM
            {
                Id = teacher.Id,
                Name = teacher.Name,
                Salary = teacher.Salary,
                Email = teacher.Email,
                SubjectId = teacher.SubjectTeachers?.FirstOrDefault()?.SubjectId ?? 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AddTeacherVM model, CancellationToken cancellationToken)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
                return View(model);
            }

            var teacher = await _teacherRepo.GetOneAsync(t => t.Id == id, tracked: true, cancellationToken: cancellationToken);
            if (teacher == null) return NotFound();

            // ✅ Check email conflict (لازم يكون نفس الـ teacher)
            var emailConflict = await _teacherRepo.GetOneAsync(
                t => t.Email == model.Email && t.Id != id,
                cancellationToken: cancellationToken);

            if (emailConflict != null)
            {
                ModelState.AddModelError("Email", "الإيميل مستخدم من معلم آخر");
                ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
                return View(model);
            }

            teacher.Name = model.Name;
            teacher.Salary = model.Salary;
            teacher.Email = model.Email;
            // Password مش بنحدثه هنا - يحتاج action منفصل

            _teacherRepo.Update(teacher);
            await _teacherRepo.CommitAsync(cancellationToken);

            // Update SubjectTeacher relations
            var oldRelations = await _subjectTeacherRepo.GetAsync(
                st => st.TeacherId == id,
                tracked: true,
                cancellationToken: cancellationToken);

            foreach (var relation in oldRelations)
            {
                _subjectTeacherRepo.Delete(relation);
            }
            await _subjectTeacherRepo.CommitAsync(cancellationToken);

            if (model.SubjectId > 0)
            {
                await _subjectTeacherRepo.CreateAsync(new SubjectTeacher
                {
                    TeacherId = id,
                    SubjectId = model.SubjectId
                }, cancellationToken);
                await _subjectTeacherRepo.CommitAsync(cancellationToken);
            }

            TempData["success"] = "تم تعديل المعلم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var teacher = await _teacherRepo.GetOneAsync(t => t.Id == id, tracked: true, cancellationToken: cancellationToken);
            if (teacher == null)
                return Json(new { success = false, message = "المعلم غير موجود" });

            try
            {
                // Delete SubjectTeachers first (Cascade هيشتغل)
                var subjectTeachers = await _subjectTeacherRepo.GetAsync(
                    st => st.TeacherId == id,
                    tracked: true,
                    cancellationToken: cancellationToken);

                foreach (var st in subjectTeachers)
                {
                    _subjectTeacherRepo.Delete(st);
                }
                await _subjectTeacherRepo.CommitAsync(cancellationToken);

                _teacherRepo.Delete(teacher);
                await _teacherRepo.CommitAsync(cancellationToken);

                TempData["success"] = "تم حذف المعلم بنجاح";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في الحذف: " + ex.Message });
            }
        }
    }
}
