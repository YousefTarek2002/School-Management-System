using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using School.Models;
using School.Models.VM;
using System.ComponentModel.DataAnnotations;

namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Admin")]
    public class ExamController : Controller
    {
        private readonly IRepository<Exam> _examRepo;
        private readonly IRepository<Class> _classRepo;
        private readonly IRepository<Subject> _subjectRepo;
        private readonly IWebHostEnvironment _env;

        public ExamController(
            IRepository<Exam> examRepo,
            IRepository<Class> classRepo,
            IRepository<Subject> subjectRepo,
            IWebHostEnvironment env)
        {
            _examRepo = examRepo;
            _classRepo = classRepo;
            _subjectRepo = subjectRepo;
            _env = env;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var exams = await _examRepo.GetAsync(
                includeProperties: q => q
                    .Include(e => e.Subject)
                    .Include(e => e.Class)
                    .Include(e => e.ExamResults)
                    .ThenInclude(er => er.Student),
                tracked: false,
                cancellationToken: cancellationToken);

            var examVMs = exams.Select(e => new ExamVM
            {
                Id = e.Id,
                ExamName = e.ExamName ?? "",
                SubjectName = e.Subject?.Name ?? "",
                ClassName = e.Class?.Name ?? "",
                ExamDate = e.ExamDate,
                ExamTime = e.ExamTime,
                StudentsCount = e.ExamResults?.Count ?? 0,
                ExistingTimeTablePath = e.TimeTablePath
            }).ToList();

            return View(examVMs);
        }

        // ================= CREATE GET =================
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var vm = new ExamVM();
            await LoadData(vm, cancellationToken);
            return View(vm);
        }

        // ================= CREATE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamVM vm, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await LoadData(vm, cancellationToken);
                return View(vm);
            }

            // Unique combination check (بدون Section)
            var exists = await _examRepo.GetOneAsync(e =>
                e.ExamName == vm.ExamName &&
                e.ClassId == vm.ClassId &&
                e.SubjectId == vm.SubjectId &&
                e.ExamDate.Date == vm.ExamDate.Date,
                cancellationToken: cancellationToken);

            if (exists != null)
            {
                ModelState.AddModelError("", "امتحان بنفس التفاصيل موجود مسبقاً");
                await LoadData(vm, cancellationToken);
                return View(vm);
            }

            string? filePath = await SaveTimeTableFile(vm.TimeTableFile);

            var exam = new Exam
            {
                ExamName = vm.ExamName,
                SubjectId = vm.SubjectId,
                ClassId = vm.ClassId,
                ExamDate = vm.ExamDate,
                ExamTime = vm.ExamTime,
                TimeTablePath = filePath
            };

            await _examRepo.CreateAsync(exam, cancellationToken);
            await _examRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم إنشاء الامتحان بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT GET =================
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var exam = await _examRepo.GetOneAsync(
                e => e.Id == id,
                includeProperties: q => q
                    .Include(e => e.Subject)
                    .Include(e => e.Class),
                tracked: false,
                cancellationToken: cancellationToken);

            if (exam == null) return NotFound();

            var vm = new ExamVM
            {
                Id = exam.Id,
                ExamName = exam.ExamName ?? "",
                SubjectId = exam.SubjectId,
                ClassId = exam.ClassId,
                ExamDate = exam.ExamDate,
                ExamTime = exam.ExamTime,
                ExistingTimeTablePath = exam.TimeTablePath
            };

            await LoadData(vm, cancellationToken);
            return View(vm);
        }

        // ================= EDIT POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExamVM vm, CancellationToken cancellationToken)
        {
            if (id != vm.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadData(vm, cancellationToken);
                return View(vm);
            }

            var exam = await _examRepo.GetOneAsync(
                e => e.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (exam == null) return NotFound();

            // Check conflict (exclude current exam) - بدون Section
            var conflict = await _examRepo.GetOneAsync(e =>
                e.ExamName == vm.ExamName &&
                e.ClassId == vm.ClassId &&
                e.SubjectId == vm.SubjectId &&
                e.ExamDate.Date == vm.ExamDate.Date &&
                e.Id != id,
                cancellationToken: cancellationToken);

            if (conflict != null)
            {
                ModelState.AddModelError("", "امتحان بنفس التفاصيل موجود مسبقاً");
                await LoadData(vm, cancellationToken);
                return View(vm);
            }

            // Handle file upload
            if (vm.TimeTableFile != null)
            {
                var newFilePath = await SaveTimeTableFile(vm.TimeTableFile);
                if (!string.IsNullOrEmpty(vm.ExistingTimeTablePath))
                {
                    DeleteOldFile(vm.ExistingTimeTablePath);
                }
                exam.TimeTablePath = newFilePath;
            }

            exam.ExamName = vm.ExamName;
            exam.SubjectId = vm.SubjectId;
            exam.ClassId = vm.ClassId;
            exam.ExamDate = vm.ExamDate;
            exam.ExamTime = vm.ExamTime;

            _examRepo.Update(exam);
            await _examRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم تعديل الامتحان بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var exam = await _examRepo.GetOneAsync(
                e => e.Id == id,
                includeProperties: q => q.Include(e => e.ExamResults),
                tracked: true,
                cancellationToken: cancellationToken);

            if (exam == null)
                return Json(new { success = false, message = "الامتحان غير موجود" });

            if (exam.ExamResults?.Any() == true)
                return Json(new { success = false, message = "لا يمكن الحذف - يوجد نتائج امتحان" });

            // Delete file if exists
            if (!string.IsNullOrEmpty(exam.TimeTablePath))
            {
                DeleteOldFile(exam.TimeTablePath);
            }

            _examRepo.Delete(exam);
            await _examRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم حذف الامتحان بنجاح";
            return Json(new { success = true });
        }

        // ================= PRIVATE METHODS =================
        private async Task LoadData(ExamVM vm, CancellationToken cancellationToken)
        {
            // Subjects
            var subjects = await _subjectRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
            vm.Subjects = subjects.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name ?? ""
            }).ToList();

            // Classes
            var classes = await _classRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
            vm.Classes = classes.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name ?? ""
            }).ToList();
        }

        private async Task<string?> SaveTimeTableFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var folder = Path.Combine(_env.WebRootPath, "uploads/exams");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var fullPath = Path.Combine(folder, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/uploads/exams/" + fileName;
        }

        private void DeleteOldFile(string filePath)
        {
            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
