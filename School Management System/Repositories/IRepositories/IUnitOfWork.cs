namespace School.Repositories.IRepositories
{
    public interface IUnitOfWork
    {
        IRepository<Book> Book { get; }
        IRepository<Student> Student { get; }
        IRepository<Teacher> Teacher { get; }
        IRepository<Class> Class { get; }
        IRepository<ClassEnrollment> ClassEnrollment { get; }
        IRepository<BookIssue> BookIssue { get; }
        IRepository<ExamResult> ExamResult { get; }
        IRepository<Exam> Exam { get; }
        IRepository<Subject> Subject { get; }
        IRepository<Section> Section { get; }
        IRepository<SubjectTeacher> SubjectTeacher { get; }
        IRepository<Fee> Fee { get; }


        int Save();
    }
}
