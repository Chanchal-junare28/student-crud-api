using StudentCRUD.Api.DTOs;

namespace StudentCRUD.Api.Services
{
    public interface IStudentService
    {
        Task<PagedResult<StudentReadDto>> GetAllAsync(string? name, int? page = null, int? pageSize = null);
        Task<StudentReadDto?> GetAsync(int id);
        Task<(bool ok, string? error, StudentReadDto? created)> CreateAsync(StudentCreateDto dto);
        Task<(bool ok, string? error)> UpdateAsync(int id, StudentUpdateDto dto);
        Task<(bool ok, string? error)> DeleteAsync(int id);
        Task<int> DeleteAllAsync();
    }
}