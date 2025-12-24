using StudentCRUD.Api.Models;

namespace StudentCRUD.Api.Repositories
{
    public interface IStudentRepository
    {
        Task<IReadOnlyList<Student>> GetAllAsync(string? name, int? page = null, int? pageSize = null);
        Task<int> GetCountAsync(string? name);
        Task<Student?> GetByIdAsync(int id);
        Task<bool> ExistsByIdAsync(int id);
        Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
        Task<Student> AddAsync(Student entity);
        Task UpdateAsync(Student entity);
        Task<bool> DeleteAsync(int id);
        Task<int> DeleteAllAsync();
    }
}