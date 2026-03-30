using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School.Models;
using School.Models.VM;

namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
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
        public async Task<IActionResult> Index(int page = 1,int pageSize = 8,string search = "",CancellationToken cancellationToken = default)
        {
            var teachersData = await _teacherRepo.GetAsync(
                includeProperties: q => q.Include(t => t.SubjectTeachers)
                                         .ThenInclude(st => st.Subject),
                tracked: false,
                cancellationToken: cancellationToken);

            // ✅ FILTER
            if (!string.IsNullOrWhiteSpace(search))
            {
                teachersData = teachersData
                    .Where(t => t.Name.ToLower().Contains(search.ToLower()))
                    .ToList();
            }

            // ✅ PAGINATION
            var totalCount = teachersData.Count();

            var teachers = teachersData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var teacherVMs = teachers.Select(t => new TeacherVM
            {
                Id = t.Id,
                Name = t.Name,
                Salary = t.Salary,
                Email = t.Email,
                SubjectName = t.SubjectTeachers?
                    .Select(st => st.Subject.Name)
                    .FirstOrDefault() ?? "غير محدد"
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Search = search;

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
                Password = model.Password,
                ConfirmPassword = null
            };

            await _teacherRepo.CreateAsync(teacher, cancellationToken);
            await _teacherRepo.CommitAsync(cancellationToken);

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
                includeProperties: q => q.Include(t => t.SubjectTeachers),
                tracked: false,
                cancellationToken: cancellationToken);

            if (teacher == null)
                return NotFound();

            var model = new EditTeacherVM
            {
                Id = teacher.Id,
                Name = teacher.Name,
                Salary = teacher.Salary,
                Email = teacher.Email,
                SubjectId = teacher.SubjectTeachers.FirstOrDefault()?.SubjectId
            };

            ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTeacherVM model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
                return View(model);
            }

            var teacher = await _teacherRepo.GetOneAsync(
                t => t.Id == model.Id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (teacher == null)
                return NotFound();

            var emailExists = await _teacherRepo.GetOneAsync(
                t => t.Email == model.Email && t.Id != model.Id,
                cancellationToken: cancellationToken);

            if (emailExists != null)
            {
                ModelState.AddModelError("Email", "الإيميل مستخدم قبل كده");
                ViewBag.Subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
                return View(model);
            }

            teacher.Name = model.Name;
            teacher.Salary = model.Salary;
            teacher.Email = model.Email;

            _teacherRepo.Update(teacher);
            await _teacherRepo.CommitAsync(cancellationToken);

            var oldRelations = await _subjectTeacherRepo.GetAsync(
                st => st.TeacherId == model.Id,
                tracked: true,
                cancellationToken: cancellationToken);

            foreach (var item in oldRelations)
                _subjectTeacherRepo.Delete(item);

            await _subjectTeacherRepo.CommitAsync(cancellationToken);

            if (model.SubjectId.HasValue)
            {
                await _subjectTeacherRepo.CreateAsync(new SubjectTeacher
                {
                    TeacherId = model.Id,
                    SubjectId = model.SubjectId.Value
                }, cancellationToken);

                await _subjectTeacherRepo.CommitAsync(cancellationToken);
            }

            TempData["success"] = "تم التعديل بنجاح";
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
                var subjectTeachers = await _subjectTeacherRepo.GetAsync(
                    st => st.TeacherId == id,
                    tracked: true,
                    cancellationToken: cancellationToken);

                foreach (var st in subjectTeachers)
                    _subjectTeacherRepo.Delete(st);

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

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var teacher = await _teacherRepo.GetOneAsync(
                t => t.Id == id,
                includeProperties: q => q
                    .Include(t => t.SubjectTeachers)
                    .ThenInclude(st => st.Subject),
                tracked: false,
                cancellationToken: cancellationToken);

            if (teacher == null)
                return NotFound();

            var model = new TeacherDetailsVM
            {
                Id = teacher.Id,
                Name = teacher.Name,
                Salary = teacher.Salary,
                Email = teacher.Email,
                Subjects = teacher.SubjectTeachers
                    .Select(st => st.Subject.Name)
                    .ToList()
            };

            return View(model);
        }
    }


}
