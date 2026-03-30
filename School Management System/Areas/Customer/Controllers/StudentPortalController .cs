namespace School.Areas.Customer.Controllers
{
    [Area("Customer")]

    public class StudentPortalController : Controller
    {
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Fee> _feeRepo;
        private readonly IRepository<ExamResult> _examResultRepo;
        private readonly IRepository<ClassEnrollment> _enrollmentRepo;
        private readonly IRepository<BookIssue> _bookRepo;

        public StudentPortalController(
            IRepository<Student> studentRepo,
            IRepository<Fee> feeRepo,
            IRepository<ExamResult> examResultRepo,
            IRepository<ClassEnrollment> enrollmentRepo,
            IRepository<BookIssue> bookRepo)
        {
            _studentRepo = studentRepo;
            _feeRepo = feeRepo;
            _examResultRepo = examResultRepo;
            _enrollmentRepo = enrollmentRepo;
            _bookRepo = bookRepo;
        }

        // ================= 1. الصفوف =================
        public async Task<IActionResult> MyClasses(CancellationToken ct)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return Unauthorized();

            var enrollments = await _enrollmentRepo.GetAsync(
                filter: e => e.StudentId == studentId && e.Status,
                includeProperties: q => q.Include(e => e.Class)
                                       .ThenInclude(c => c.Teacher),
                tracked: false,
                cancellationToken: ct);

            return View(enrollments.OrderBy(e => e.Class.Name).ToList());
        }

        // ================= 2. نتائج الامتحانات =================
        public async Task<IActionResult> MyResults(CancellationToken ct)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return Unauthorized();

            var results = await _examResultRepo.GetAsync(
                filter: r => r.StudentId == studentId,
                includeProperties: q => q
                    .Include(r => r.Exam)
                    .ThenInclude(e => e.Subject)
                    .Include(r => r.Exam)
                    .ThenInclude(e => e.Class)
                    .ThenInclude(c => c.Teacher),
                tracked: false,
                cancellationToken: ct);

            return View(results.OrderByDescending(r => r.Exam.ExamTime).ToList());
        }

        // ================= 3. المصاريف =================
        public async Task<IActionResult> MyFees(CancellationToken ct)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return Unauthorized();

            var fees = await _feeRepo.GetAsync(
                filter: f => f.StudentId == studentId,
                tracked: false,
                cancellationToken: ct);

            var orderedFees = fees.OrderByDescending(f => f.DueDate).ToList();
            ViewBag.TotalDue = orderedFees.Where(f => !f.Paid).Sum(f => f.Amount);

            return View(orderedFees);
        }

        // ================= 4. الكتب المستعارة =================
        public async Task<IActionResult> MyBooks(CancellationToken ct)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return Unauthorized();

            var books = await _bookRepo.GetAsync(
                filter: b => b.StudentId == studentId,
                includeProperties: q => q.Include(b => b.Book),
                tracked: false,
                cancellationToken: ct);

            return View(books.OrderByDescending(b => b.IssueDate).ToList());
        }

        // ================= 5. المعلومات الشخصية =================
        public async Task<IActionResult> Profile(CancellationToken ct)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return Unauthorized();

            var student = await _studentRepo.GetOneAsync(
                filter: s => s.Id == studentId,
                includeProperties: q => q.Include(s => s.ClassEnrollments)
                                       .ThenInclude(ce => ce.Class),
                tracked: false,
                cancellationToken: ct);

            return View(student);
        }

        // ================= Helper Method =================
        private int GetCurrentStudentId()
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            return int.TryParse(studentIdClaim, out int id) ? id : 0;
        }
    }
}
