namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
    public class SubjectController : Controller
    {
        private readonly IRepository<Subject> _subjectRepo;
        private readonly IRepository<Teacher> _teacherRepo;
        private readonly IRepository<SubjectTeacher> _subjectTeacherRepo;

        public SubjectController(
            IRepository<Subject> subjectRepo,
            IRepository<Teacher> teacherRepo,
            IRepository<SubjectTeacher> subjectTeacherRepo)
        {
            _subjectRepo = subjectRepo;
            _teacherRepo = teacherRepo;
            _subjectTeacherRepo = subjectTeacherRepo;
        }

        // ================= INDEX ==================
        public async Task<IActionResult> Index( int page = 1, int pageSize = 8, string search = "", CancellationToken ct = default)
        {
            var subjectsData = await _subjectRepo.GetAsync(
            includeProperties: q => q.Include(s => s.SubjectTeachers)
            .ThenInclude(st => st.Teacher),
            tracked: false,
            cancellationToken: ct);


            // ✅ FILTER
            if (!string.IsNullOrWhiteSpace(search))
            {
                subjectsData = subjectsData
                    .Where(s => s.Name.ToLower().Contains(search.ToLower()))
                    .ToList();
            }

            // ✅ PAGINATION
            var totalCount = subjectsData.Count();

            var subjects = subjectsData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var subjectVMs = subjects.Select(s => new SubjectVM
            {
                Id = s.Id,
                Name = s.Name ?? "",
                TeacherNames = s.SubjectTeachers?
                    .Select(st => st.Teacher.Name ?? "")
                    .ToList() ?? new List<string>()
            }).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.Search = search;

            return View(subjectVMs);


        }


        // ================= ADD ==================
        [HttpGet]
        public async Task<IActionResult> Add(CancellationToken ct)
        {
            var vm = new SubjectVM
            {
                AllTeachers = (await _teacherRepo.GetAsync(tracked: false, cancellationToken: ct)).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(SubjectVM vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                vm.AllTeachers = (await _teacherRepo.GetAsync(tracked: false, cancellationToken: ct)).ToList();
                return View(vm);
            }

            // Check name exists
            var nameExists = await _subjectRepo.GetOneAsync(s => s.Name == vm.Name, cancellationToken: ct);
            if (nameExists != null)
            {
                ModelState.AddModelError("Name", "المادة موجودة مسبقاً");
                vm.AllTeachers = (await _teacherRepo.GetAsync(tracked: false, cancellationToken: ct)).ToList();
                return View(vm);
            }

            var subject = new Subject { Name = vm.Name };
            await _subjectRepo.CreateAsync(subject, ct);
            await _subjectRepo.CommitAsync(ct);

            // Add SubjectTeacher relations
            if (vm.SelectedTeachers?.Any() == true)
            {
                foreach (var teacherId in vm.SelectedTeachers)
                {
                    await _subjectTeacherRepo.CreateAsync(new SubjectTeacher
                    {
                        SubjectId = subject.Id,
                        TeacherId = teacherId
                    }, ct);
                }
                await _subjectTeacherRepo.CommitAsync(ct);
            }

            TempData["success"] = "تم إضافة المادة بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT ==================
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var subject = await _subjectRepo.GetOneAsync(
                s => s.Id == id,
                includeProperties: q => q.Include(s => s.SubjectTeachers).ThenInclude(st => st.Teacher),
                tracked: true,
                cancellationToken: ct);

            if (subject == null) return NotFound();

            var vm = new SubjectVM
            {
                Id = subject.Id,
                Name = subject.Name ?? "",
                SelectedTeachers = subject.SubjectTeachers?.Select(x => x.TeacherId).ToList() ?? new List<int>(),
                AllTeachers = (await _teacherRepo.GetAsync(tracked: false, cancellationToken: ct)).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SubjectVM vm, CancellationToken ct)
        {
            if (id != vm.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                vm.AllTeachers = (await _teacherRepo.GetAsync(tracked: false, cancellationToken: ct)).ToList();
                return View(vm);
            }

            // Check name conflict
            var nameConflict = await _subjectRepo.GetOneAsync(
                s => s.Name == vm.Name && s.Id != id, cancellationToken: ct);

            if (nameConflict != null)
            {
                ModelState.AddModelError("Name", "اسم المادة مستخدم من مادة أخرى");
                vm.AllTeachers = (await _teacherRepo.GetAsync(tracked: false, cancellationToken: ct)).ToList();
                return View(vm);
            }

            var subject = await _subjectRepo.GetOneAsync(s => s.Id == id, tracked: true, cancellationToken: ct);
            if (subject == null) return NotFound();

            subject.Name = vm.Name;
            _subjectRepo.Update(subject);
            await _subjectRepo.CommitAsync(ct);

            // Delete old relations
            var oldRelations = await _subjectTeacherRepo.GetAsync(
                st => st.SubjectId == id,
                tracked: true,
                cancellationToken: ct);

            foreach (var relation in oldRelations)
                _subjectTeacherRepo.Delete(relation);
            await _subjectTeacherRepo.CommitAsync(ct);

            // Add new relations
            if (vm.SelectedTeachers?.Any() == true)
            {
                foreach (var teacherId in vm.SelectedTeachers)
                {
                    await _subjectTeacherRepo.CreateAsync(new SubjectTeacher
                    {
                        SubjectId = id,
                        TeacherId = teacherId
                    }, ct);
                }
                await _subjectTeacherRepo.CommitAsync(ct);
            }

            TempData["success"] = "تم تعديل المادة بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE ==================
        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var subject = await _subjectRepo.GetOneAsync(s => s.Id == id, tracked: true, cancellationToken: ct);
            if (subject == null)
                return Json(new { success = false, message = "المادة غير موجودة" });

            try
            {
                // Delete SubjectTeachers first
                var relations = await _subjectTeacherRepo.GetAsync(st => st.SubjectId == id, tracked: true, cancellationToken: ct);
                foreach (var r in relations)
                    _subjectTeacherRepo.Delete(r);
                await _subjectTeacherRepo.CommitAsync(ct);

                _subjectRepo.Delete(subject);
                await _subjectRepo.CommitAsync(ct);

                TempData["success"] = "تم حذف المادة بنجاح";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في الحذف: " + ex.Message });
            }
        }
    }
}
