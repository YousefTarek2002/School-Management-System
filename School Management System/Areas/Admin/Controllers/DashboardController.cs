

namespace School.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardVM = new DashboardVM
            {
                TotalStudents = await _context.Students.CountAsync(),
                TotalTeachers = await _context.Teachers.CountAsync(),
                TotalClasses = await _context.Classes.CountAsync(),
                TotalExams = await _context.Exams.CountAsync(),
                TotalFeesDue = await _context.Fees.Where(f => !f.Paid).SumAsync(f => f.Amount),
                TotalFeesPaid = await _context.Fees.Where(f => f.Paid).SumAsync(f => f.Amount),
                RecentStudents = await _context.Students
                    .OrderByDescending(s => s.Id)
                    .Take(5)
                    .Select(s => new RecentItemVM
                    {
                        Name = s.FirstName + " " + s.LastName,
                        Date = s.BD 
                    })
                    .ToListAsync(),
                RecentFees = await _context.Fees
                    .OrderByDescending(f => f.DueDate)
                    .Take(5)
                    .Select(f => new RecentItemVM
                    {
                        Name = f.Student.FirstName + " " + f.Student.LastName,
                        Date = f.DueDate
                    })
                    .ToListAsync()
            };

            return View(dashboardVM);
        }
    }
}
