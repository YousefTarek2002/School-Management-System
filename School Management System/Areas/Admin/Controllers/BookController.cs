namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
    public class BookController : Controller

    {
        private readonly IRepository<Book> _bookRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<BookIssue> _bookIssueRepo;

        public BookController(
            IRepository<Book> bookRepo,
            IRepository<Student> studentRepo,
            IRepository<BookIssue> bookIssueRepo)
        {
            _bookRepo = bookRepo;
            _studentRepo = studentRepo;
            _bookIssueRepo = bookIssueRepo;
        }


        // ================= INDEX =================
        public async Task<IActionResult> Index(int page = 1, int pageSize = 8, string search = "", CancellationToken ct = default)
        {
            var booksData = await _bookRepo.GetAsync(
                includeProperties: q => q.Include(b => b.BookIssues),
                tracked: false,
                cancellationToken: ct);

            // ✅ FILTER
            if (!string.IsNullOrWhiteSpace(search))
            {
                booksData = booksData
                    .Where(b => b.Title.ToLower().Contains(search.ToLower()) ||
                               b.Author.ToLower().Contains(search.ToLower()))
                    .ToList();
            }

            // ✅ PAGINATION
            var totalCount = booksData.Count();
            var books = booksData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Search = search;

            return View(books);
        }

        [HttpGet] public IActionResult Create() => View();
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book, CancellationToken cancellation)
        {
            if (!ModelState.IsValid) return View(book);

            book.CopiesAvailable = book.TotalCopies;
            await _bookRepo.CreateAsync(book);
            await _bookRepo.CommitAsync(cancellation);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellation)
        {
            var book = await _bookRepo.GetOneAsync(b => b.Id == id, tracked: true);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book, CancellationToken cancellationToken)
        {
            if (id != book.Id) return NotFound();
            if (!ModelState.IsValid) return View(book);
            _bookRepo.Update(book);
            await _bookRepo.CommitAsync(cancellationToken);
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var book = await _bookRepo.GetOneAsync(b => b.Id == id, tracked: true, cancellationToken: cancellationToken);
            if (book == null)
                return Json(new { success = false, message = "الكتاب غير موجود" });

            try
            {
                // Delete BookIssues first (زي SubjectTeachers)
                var bookIssues = await _bookIssueRepo.GetAsync(
                    bi => bi.BookId == id,
                    tracked: true,
                    cancellationToken: cancellationToken);

                foreach (var bi in bookIssues)
                    _bookIssueRepo.Delete(bi);
                await _bookIssueRepo.CommitAsync(cancellationToken);

                _bookRepo.Delete(book);
                await _bookRepo.CommitAsync(cancellationToken);

                TempData["success"] = "تم حذف الكتاب بنجاح";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في الحذف: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> IssueBook(int bookId, CancellationToken cancellationToken)
        {
            var book = await _bookRepo.GetOneAsync(b => b.Id == bookId);
            if (book == null || book.CopiesAvailable <= 0)
                return NotFound();

            ViewBag.Book = book;
            ViewBag.Students = await _studentRepo.GetAsync(tracked: false, cancellationToken: cancellationToken);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueBook(int bookId, int studentId, CancellationToken cancellationToken)
        {
            if (bookId <= 0 || studentId <= 0) return BadRequest();

            var book = await _bookRepo.GetOneAsync(b => b.Id == bookId, tracked: true);
            var student = await _studentRepo.GetOneAsync(s => s.Id == studentId, tracked: false);

            if (book == null || student == null || book.CopiesAvailable <= 0)
                return NotFound();

            // تحقق من عدم وجود book issue نشط للطالب
            var existingIssue = await _bookIssueRepo.GetOneAsync(bi =>
                bi.StudentId == studentId && bi.BookId == bookId && bi.ReturnDate > DateTime.Now);

            if (existingIssue != null)
            {
                TempData["error"] = "الطالب لديه نسخة نشطة من هذا الكتاب";
                return RedirectToAction(nameof(Index));
            }

            var bookIssue = new BookIssue
            {
                IssueDate = DateTime.Now,
                ReturnDate = DateTime.Now.AddDays(14),
                StudentId = studentId,
                BookId = bookId
            };

            await _bookIssueRepo.CreateAsync(bookIssue);
            book.CopiesAvailable--;
            _bookRepo.Update(book);

            await _bookRepo.CommitAsync(cancellationToken);  // ✅ واحد SaveChanges

            TempData["success"] = $"تم إعارة الكتاب '{book.Title}' لـ {student.FirstName} {student.LastName} بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var book = await _bookRepo.GetOneAsync(
                b => b.Id == id,
                includeProperties: q => q
                    .Include(b => b.BookIssues)
                    .ThenInclude(bi => bi.Student),
                tracked: false,
                cancellationToken: ct);

            if (book == null)
                return NotFound();

            var vm = new BookDetailsVM
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                TotalCopies = book.TotalCopies,
                CopiesAvailable = book.CopiesAvailable,

                Issues = book.BookIssues.Select(bi => new BookIssueVM
                {
                    StudentName = bi.Student.FirstName + " " + bi.Student.LastName,
                    Email = bi.Student.Email,
                    IssueDate = bi.IssueDate,
                    ReturnDate = bi.ReturnDate
                }).OrderByDescending(x => x.IssueDate).ToList()
            };

            return View(vm);
        }
    }
}
