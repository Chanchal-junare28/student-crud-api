using Microsoft.EntityFrameworkCore;
using StudentCRUD.Api.Data;
using StudentCRUD.Api.Models;

namespace StudentCRUD.Api.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly ApplicationDbContext _db;

        public StudentRepository(ApplicationDbContext db) => _db = db;

        public async Task<IReadOnlyList<Student>> GetAllAsync(string? name, int? page = null, int? pageSize = null)
        {
            IQueryable<Student> q = _db.Students.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var term = name.Trim().ToLower();
                q = q.Where(s => s.Name.ToLower().Contains(term));
            }

            q = q.OrderBy(s => s.Name);

            if (page.HasValue && pageSize.HasValue && page > 0 && pageSize > 0)
            {
                q = q.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }

            return await q.ToListAsync();
        }

        public async Task<int> GetCountAsync(string? name)
        {
            IQueryable<Student> q = _db.Students.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(name))
            {
                var term = name.Trim().ToLower();
                q = q.Where(s => s.Name.ToLower().Contains(term));
            }
            return await q.CountAsync();
        }

        public Task<Student?> GetByIdAsync(int id)
            => _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

        public Task<bool> ExistsByIdAsync(int id)
            => _db.Students.AnyAsync(s => s.Id == id);

        public Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
        {
            var normalized = email.Trim().ToLower();
            return _db.Students.AnyAsync(s =>
                s.Email.ToLower() == normalized &&
                (!excludeId.HasValue || s.Id != excludeId.Value));
        }

        public async Task<Student> AddAsync(Student entity)
        {
            _db.Students.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        // public async Task UpdateAsync(Student entity)
        // {
        //     _db.Entry(entity).State = EntityState.Modified;
        //     await _db.SaveChangesAsync();
        // }

        public async Task UpdateAsync(Student entity)
        {
            // Detach any already tracked instance with same key to avoid duplicate tracking
            var local = _db.Students.Local.FirstOrDefault(e => e.Id == entity.Id);
            if (local != null)
            {
                _db.Entry(local).State = EntityState.Detached;
            }

            // Attach and mark as modified
            _db.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;

            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var s = await _db.Students.FindAsync(id);
            if (s is null) return false;
            _db.Students.Remove(s);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteAllAsync()
        {
            var all = await _db.Students.ToListAsync();
            _db.Students.RemoveRange(all);
            return await _db.SaveChangesAsync();
        }
    }
}