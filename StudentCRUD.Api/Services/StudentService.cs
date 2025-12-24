using StudentCRUD.Api.DTOs;
using StudentCRUD.Api.Models;
using StudentCRUD.Api.Repositories;

namespace StudentCRUD.Api.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repo;

        public StudentService(IStudentRepository repo) => _repo = repo;

        public async Task<PagedResult<StudentReadDto>> GetAllAsync(string? name, int? page = null, int? pageSize = null)
        {
            var items = await _repo.GetAllAsync(name, page, pageSize);
            var total = await _repo.GetCountAsync(name);

            return new PagedResult<StudentReadDto>
            {
                Items = items.Select(MapToReadDto).ToList(),
                TotalCount = total,
                Page = page ?? 1,
                PageSize = pageSize ?? items.Count
            };
        }

        public async Task<StudentReadDto?> GetAsync(int id)
        {
            var s = await _repo.GetByIdAsync(id);
            return s is null ? null : MapToReadDto(s);
        }

        public async Task<(bool ok, string? error, StudentReadDto? created)> CreateAsync(StudentCreateDto dto)
        {
            // Email uniqueness check
            if (await _repo.ExistsByEmailAsync(dto.Email))
                return (false, "Email already exists.", null);

            var entity = new Student
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim(),
                Gender = dto.Gender.Trim()
            };

            var created = await _repo.AddAsync(entity);
            return (true, null, MapToReadDto(created));
        }

        public async Task<(bool ok, string? error)> UpdateAsync(int id, StudentUpdateDto dto)
        {
            if (!await _repo.ExistsByIdAsync(id))
                return (false, "Student not found.");

            if (await _repo.ExistsByEmailAsync(dto.Email, excludeId: id))
                return (false, "Email already exists for another student.");

            var entity = new Student
            {
                Id = id,
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim(),
                Gender = dto.Gender.Trim()
            };

            await _repo.UpdateAsync(entity);
            return (true, null);
        }

        public async Task<(bool ok, string? error)> DeleteAsync(int id)
        {
            var ok = await _repo.DeleteAsync(id);
            return ok ? (true, null) : (false, "Student not found.");
        }

        public Task<int> DeleteAllAsync() => _repo.DeleteAllAsync();

        private static StudentReadDto MapToReadDto(Student s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Email = s.Email,
            Gender = s.Gender
        };
    }
}