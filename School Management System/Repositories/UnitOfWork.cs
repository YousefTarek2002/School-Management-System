using School.Data;
using School.Models;
using School.Repositories.IRepositories;

namespace School.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IRepository<Book> Book { get; private set; }
        public IRepository<Student> Student { get; private set; }
        public IRepository<Teacher> Teacher { get; private set; }
        public IRepository<Class> Class { get; private set; }
        public IRepository<ClassEnrollment> ClassEnrollment { get; private set; }
        public IRepository<BookIssue> BookIssue { get; private set; }
        public IRepository<ExamResult> ExamResult { get; private set; }
        public IRepository<Exam> Exam { get; private set; }
        public IRepository<Subject> Subject { get; private set; }
        public IRepository<Section> Section { get; private set; }
        public IRepository<SubjectTeacher> SubjectTeacher { get; private set; }
        public IRepository<Fee> Fee { get; private set; }



        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            Book = new Repository<Book>(_context);
            Student = new Repository<Student>(_context);
            Teacher = new Repository<Teacher>(_context);
            Class = new Repository<Class>(_context);
            ClassEnrollment = new Repository<ClassEnrollment>(_context);
            BookIssue = new Repository<BookIssue>(_context);
            ExamResult = new Repository<ExamResult>(_context);
            Exam = new Repository<Exam>(_context);
            Subject = new Repository<Subject>(_context);
            Section = new Repository<Section>(_context);
            SubjectTeacher = new Repository<SubjectTeacher>(_context);
            Fee = new Repository<Fee>(_context);
        }

        public int Save()
        {
            return _context.SaveChanges();
        }
    }
}
