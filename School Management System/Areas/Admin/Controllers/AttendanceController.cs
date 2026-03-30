namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
    public class AttendanceController : Controller
    {
        private readonly IRepository<Attendance> _attRepo;
        private readonly IRepository<Student> _studentRepo;
        private readonly IRepository<Class> _classRepo;

        public AttendanceController(
            IRepository<Attendance> attRepo,
            IRepository<Student> studentRepo,
            IRepository<Class> classRepo)
        {
            _attRepo = attRepo;
            _studentRepo = studentRepo;
            _classRepo = classRepo;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(int page = 1, int pageSize = 8, string search = "", CancellationToken cancellationToken = default)
        {
            var attendancesData = await _attRepo.GetAsync(
                includeProperties: q => q
                    .Include(a => a.Student)
                    .Include(a => a.Class),
                tracked: false,
                cancellationToken: cancellationToken);

            // ✅ FILTER
            if (!string.IsNullOrWhiteSpace(search))
            {
                attendancesData = attendancesData
                    .Where(a => a.Student.FirstName.ToLower().Contains(search.ToLower()) ||
                               a.Student.LastName.ToLower().Contains(search.ToLower()) ||
                               $"{a.Class.Name} {a.Class.Section}".ToLower().Contains(search.ToLower()))
                    .ToList();
            }

            // ✅ PAGINATION
            var totalCount = attendancesData.Count();
            var attendances = attendancesData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var attendanceVMs = attendances.Select(a => new AttendanceVM
            {
                Id = a.Id,
                StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
                ClassName = $"{a.Class.Name} - {a.Class.Section}",
                Date = a.Date.ToDateTime(TimeOnly.MinValue),
                StatusText = a.Status ? "حاضر" : "غائب"
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Search = search;

            return View(attendanceVMs);
        }

        private async Task LoadData(AttendanceVM vm, CancellationToken cancellationToken)
        {
            var students = (await _studentRepo.GetAsync(tracked: false, cancellationToken: cancellationToken)).ToList();
            var classes = (await _classRepo.GetAsync(tracked: false, cancellationToken: cancellationToken)).ToList();

            vm.Students = students.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.FirstName} {s.LastName}"
            }).ToList();

            vm.Classes = classes.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Name} - {c.Section}"
            }).ToList();
        }

        // ================= CREATE GET =================
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var vm = new AttendanceVM { Date = DateTime.Today };
            await LoadData(vm, cancellationToken);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttendanceVM vm, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await LoadData(vm, cancellationToken);
                return View(vm);
            }

            var exists = await _attRepo.GetOneAsync(
                a => a.StudentId == vm.StudentId &&
                     a.ClassId == vm.ClassId &&
                     a.Date == DateOnly.FromDateTime(vm.Date.Date),
                cancellationToken: cancellationToken);

            if (exists != null)
            {
                TempData["error"] = "سجل الحضور موجود مسبقاً لهذا الطالب في هذا الصف والتاريخ";
                await LoadData(vm, cancellationToken);
                return View(vm);
            }

            var attendance = new Attendance
            {
                StudentId = vm.StudentId,
                ClassId = vm.ClassId,
                Date = DateOnly.FromDateTime(vm.Date.Date),
                Status = vm.Status
            };

            await _attRepo.CreateAsync(attendance, cancellationToken);
            await _attRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم حفظ سجل الحضور بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT GET =================
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var attendance = await _attRepo.GetOneAsync(
                a => a.Id == id,
                includeProperties: q => q.Include(a => a.Student).Include(a => a.Class),
                tracked: true,
                cancellationToken: cancellationToken);

            if (attendance == null) return NotFound();

            var vm = new AttendanceVM
            {
                Id = attendance.Id,
                StudentId = attendance.StudentId,
                ClassId = attendance.ClassId,
                Status = attendance.Status,
                Date = attendance.Date.ToDateTime(TimeOnly.MinValue)
            };

            await LoadData(vm, cancellationToken);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AttendanceVM vm, CancellationToken cancellationToken)
        {
            if (id != vm.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadData(vm, cancellationToken);
                return View(vm);
            }

            var attendance = await _attRepo.GetOneAsync(
                a => a.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (attendance == null) return NotFound();

            attendance.StudentId = vm.StudentId;
            attendance.ClassId = vm.ClassId;
            attendance.Status = vm.Status;
            attendance.Date = DateOnly.FromDateTime(vm.Date.Date);

            _attRepo.Update(attendance);
            await _attRepo.CommitAsync(cancellationToken);

            TempData["success"] = "تم تعديل سجل الحضور بنجاح";
            return RedirectToAction(nameof(Index));
        }


        // ================= DELETE (AJAX زي Student) =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var attendance = await _attRepo.GetOneAsync(
                a => a.Id == id,
                tracked: true,
                cancellationToken: cancellationToken);

            if (attendance == null)
                return Json(new { success = false, message = "سجل الحضور غير موجود" });

            try
            {
                _attRepo.Delete(attendance);
                await _attRepo.CommitAsync(cancellationToken);

                TempData["success"] = "تم حذف سجل الحضور بنجاح";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في الحذف: " + ex.Message });
            }
        }
    }
}
