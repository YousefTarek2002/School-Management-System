
namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
    public class ExamResultController : Controller
    {
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Exam> _examRepo;
        private readonly IRepository<ExamResult> _resultRepo;
        private readonly IRepository<ClassEnrollment> _enrollmentRepo;

        public ExamResultController(
            IRepository<Student> studentRepo,
            IRepository<Exam> examRepo,
            IRepository<ExamResult> resultRepo,
            IRepository<ClassEnrollment> enrollmentRepo)
        {
            _studentRepo = studentRepo;
            _examRepo = examRepo;
            _resultRepo = resultRepo;
            _enrollmentRepo = enrollmentRepo;
        }

        // ================= INDEX - Exams With Results =================
        public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 8,
        string search = "",
        CancellationToken cancellationToken = default)
        {
            var examsData = await _examRepo.GetAsync(
            includeProperties: q => q
            .Include(e => e.ExamResults)
            .ThenInclude(er => er.Student)
            .Include(e => e.Class)
            .Include(e => e.Subject),
            tracked: false,
            cancellationToken: cancellationToken);
 
// ================= FILTER =================
if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                examsData = examsData.Where(e =>
                    (e.ExamName ?? "").ToLower().Contains(search) ||
                    (e.Subject != null && e.Subject.Name.ToLower().Contains(search)) ||
                    (e.Class != null &&
                     ($"{e.Class.Name} {e.Class.Section}").ToLower().Contains(search))
                ).ToList();
            }

            // ================= PAGINATION =================
            var totalCount = examsData.Count();

            var exams = examsData
                .OrderByDescending(e => e.Id) // ممكن تغيرها لـ Date لو عندك
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ================= MAPPING =================
            var examVMs = exams.Select(e => new ExamResultIndexVM
            {
                ExamId = e.Id,
                ExamName = e.ExamName ?? "",
                ClassName = e.Class != null
                    ? $"{e.Class.Name} - {e.Class.Section}"
                    : "No Class",
                SubjectName = e.Subject?.Name ?? "No Subject",
                TotalStudents = e.ExamResults?.Count ?? 0,
                HasResults = e.ExamResults != null && e.ExamResults.Any()
            }).ToList();

            // ================= VIEWBAG =================
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Search = search;

            return View(examVMs); 
}


        // ================= ADD RESULTS GET =================
        public async Task<IActionResult> AddResults(int examId, CancellationToken cancellationToken)
        {
            var exam = await _examRepo.GetOneAsync(
                e => e.Id == examId,
                includeProperties: q => q.Include(e => e.Class)
                                       .Include(e => e.Subject),
                tracked: false,
                cancellationToken: cancellationToken);

            if (exam == null)
                return NotFound("الامتحان غير موجود");

            // الطلاب المسجلين في نفس الفصل والشعبة
            var enrollments = await _enrollmentRepo.GetAsync(
                e => e.ClassId == exam.ClassId && e.Status,
                includeProperties: q => q.Include(e => e.Student),
                tracked: false,
                cancellationToken: cancellationToken);

            var vm = new ExamResultVM
            {
                ExamId = exam.Id,
                ExamName = exam.ExamName ?? "",
                ClassName = $"{exam.Class?.Name} - {exam.Class?.Section}",
                SubjectName = exam.Subject?.Name ?? "",
                Students = enrollments.Select(e => new StudentResultVM
                {
                    StudentId = e.Student.Id,
                    StudentName = $"{e.Student.FirstName} {e.Student.LastName}",
                    // جلب الدرجة الحالية إن وجدت
                    CurrentGrade = 0
                }).ToList()
            };

            // إضافة الدرجات الحالية للطلاب
            var existingResults = await _resultRepo.GetAsync(
                r => r.ExamId == examId,
                cancellationToken: cancellationToken);

            foreach (var student in vm.Students)
            {
                var existingResult = existingResults.FirstOrDefault(r => r.StudentId == student.StudentId);
                if (existingResult != null)
                {
                    student.CurrentGrade = existingResult.Grade;
                }
            }

            return View(vm);
        }

        // ================= ADD RESULTS POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddResults(ExamResultVM vm, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var processedCount = 0;
            var errors = new List<string>();

            foreach (var student in vm.Students)
            {
                if (student.Grade < 0 || student.Grade > 100)
                {
                    errors.Add($"درجة {student.StudentName} غير صحيحة");
                    continue;
                }

                var exists = await _resultRepo.GetOneAsync(
                    r => r.StudentId == student.StudentId && r.ExamId == vm.ExamId,
                    cancellationToken: cancellationToken);

                if (exists == null)
                {
                    // إنشاء نتيجة جديدة
                    await _resultRepo.CreateAsync(new ExamResult
                    {
                        ExamId = vm.ExamId,
                        StudentId = student.StudentId,
                        Grade = student.Grade
                    }, cancellationToken);
                }
                else
                {
                    // تحديث الدرجة الموجودة
                    exists.Grade = student.Grade;
                    _resultRepo.Update(exists);
                }
                processedCount++;
            }

            await _resultRepo.CommitAsync(cancellationToken);

            if (errors.Any())
            {
                TempData["error"] = string.Join(", ", errors);
            }

            TempData["success"] = $"تم حفظ/تحديث نتائج {processedCount} طالب بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= SHOW RESULTS FOR EXAM =================
        public async Task<IActionResult> ViewResults(int examId, CancellationToken cancellationToken)
        {
            var exam = await _examRepo.GetOneAsync(
                e => e.Id == examId,
                includeProperties: q => q.Include(e => e.Class)
                                       .Include(e => e.Subject)
                                       .Include(e => e.ExamResults)
                                           .ThenInclude(er => er.Student),
                tracked: false,
                cancellationToken: cancellationToken);

            if (exam == null)
                return NotFound("الامتحان غير موجود");

            var results = exam.ExamResults?
                .OrderByDescending(r => r.Grade)
                .Select(r => new ExamResultDisplayVM
                {
                    StudentId = r.StudentId,
                    StudentName = $"{r.Student.FirstName} {r.Student.LastName}",
                    Grade = r.Grade,
                    Rank = 0 // سيتم حسابه لاحقاً
                }).ToList() ?? new List<ExamResultDisplayVM>();

            // حساب الترتيب
            for (int i = 0; i < results.Count; i++)
            {
                results[i].Rank = i + 1;
            }

            var vm = new ExamResultsViewVM
            {
                ExamId = exam.Id,
                ExamName = exam.ExamName ?? "",
                ClassName = $"{exam.Class?.Name} - {exam.Class?.Section}",
                SubjectName = exam.Subject?.Name ?? "",
                TotalStudents = results.Count,
                HighestGrade = results.Any() ? results.Max(r => r.Grade) : 0,
                AverageGrade = results.Any() ? (decimal)Math.Round(results.Average(r => r.Grade), 1) : 0,  // ✅ explicit cast
                Results = results
            };

            return View(vm);
        }

        // ================= DELETE RESULT =================
        [HttpPost]
        public async Task<IActionResult> DeleteResult(int examId, int studentId, CancellationToken cancellationToken)
        {
            var result = await _resultRepo.GetOneAsync(
                r => r.ExamId == examId && r.StudentId == studentId,
                tracked: true,
                cancellationToken: cancellationToken);

            if (result == null)
                return Json(new { success = false, message = "النتيجة غير موجودة" });

            try
            {
                _resultRepo.Delete(result);
                await _resultRepo.CommitAsync(cancellationToken);
                return Json(new { success = true, message = "تم حذف النتيجة بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في الحذف: " + ex.Message });
            }
        }
    }
}
