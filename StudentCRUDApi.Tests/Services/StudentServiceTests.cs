using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using StudentCRUD.Api.DTOs;
using StudentCRUD.Api.Models;
using StudentCRUD.Api.Repositories;
using StudentCRUD.Api.Services;

namespace StudentCRUDApi.Tests.Services
{
    [TestFixture]
    public class StudentServiceTests
    {
        private Mock<IStudentRepository> _repo = default!;
        private StudentService _svc = default!;

        [SetUp]
        public void Setup()
        {
            _repo = new Mock<IStudentRepository>(MockBehavior.Strict);
            _svc = new StudentService(_repo.Object);
        }

        // ---- GetAllAsync ----

        [Test]
        public async Task GetAll_Defaults()
        {
            var items = new List<Student>
            {
                new Student { Id = 1, Name = "John", Email = "john@example.com", Gender = "Male" },
                new Student { Id = 2, Name = "Alice", Email = "alice@example.com", Gender = "Female" }
            };

            _repo.Setup(r => r.GetAllAsync(null, null, null)).ReturnsAsync(items);
            _repo.Setup(r => r.GetCountAsync(null)).ReturnsAsync(10);

            var res = await _svc.GetAllAsync(null);

            Assert.That(res.TotalCount, Is.EqualTo(10));
            Assert.That(res.Page, Is.EqualTo(1));
            Assert.That(res.PageSize, Is.EqualTo(items.Count));
            Assert.That(res.Items.Select(x => x.Id), Is.EqualTo(items.Select(x => x.Id)));

            _repo.Verify(r => r.GetAllAsync(null, null, null), Times.Once);
            _repo.Verify(r => r.GetCountAsync(null), Times.Once);
        }

        [Test]
        public async Task GetAll_Params()
        {
            var items = Enumerable.Range(1, 5)
                .Select(i => new Student { Id = i, Name = $"John {i}", Email = $"john{i}@example.com", Gender = "Male" })
                .ToList();

            _repo.Setup(r => r.GetAllAsync("John", 2, 5)).ReturnsAsync(items);
            _repo.Setup(r => r.GetCountAsync("John")).ReturnsAsync(13);

            var res = await _svc.GetAllAsync("John", 2, 5);

            Assert.That(res.Page, Is.EqualTo(2));
            Assert.That(res.PageSize, Is.EqualTo(5));
            Assert.That(res.TotalCount, Is.EqualTo(13));
            Assert.That(res.Items.Count, Is.EqualTo(5));
            Assert.That(res.Items[0].Name, Is.EqualTo("John 1"));

            _repo.Verify(r => r.GetAllAsync("John", 2, 5), Times.Once);
            _repo.Verify(r => r.GetCountAsync("John"), Times.Once);
        }

        // ---- GetAsync ----

        [Test]
        public async Task Get_Found()
        {
            var s = new Student { Id = 5, Name = "Alice", Email = "alice@example.com", Gender = "Female" };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(s);

            var dto = await _svc.GetAsync(5);

            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Id, Is.EqualTo(5));
            Assert.That(dto.Name, Is.EqualTo("Alice"));

            _repo.Verify(r => r.GetByIdAsync(5), Times.Once);
        }

        [Test]
        public async Task Get_NotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

            var dto = await _svc.GetAsync(999);

            Assert.That(dto, Is.Null);
            _repo.Verify(r => r.GetByIdAsync(999), Times.Once);
        }

        // ---- CreateAsync ----

        [Test]
        public async Task Create_EmailExists()
        {
            var dto = new StudentCreateDto { Name = "John", Email = "john@example.com", Gender = "Male" };
            _repo.Setup(r => r.ExistsByEmailAsync("john@example.com", (int?)null)).ReturnsAsync(true);

            var (ok, err, created) = await _svc.CreateAsync(dto);

            Assert.That(ok, Is.False);
            Assert.That(err, Is.EqualTo("Email already exists."));
            Assert.That(created, Is.Null);

            _repo.Verify(r => r.ExistsByEmailAsync("john@example.com", (int?)null), Times.Once);
            _repo.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Create_TrimAndMap()
        {
            var dto = new StudentCreateDto { Name = "  John  ", Email = "  john@example.com  ", Gender = "  Male  " };
            _repo.Setup(r => r.ExistsByEmailAsync("  john@example.com  ", (int?)null)).ReturnsAsync(false);

            _repo
                .Setup(r => r.AddAsync(It.Is<Student>(s =>
                    s.Name == "John" && s.Email == "john@example.com" && s.Gender == "Male")))
                .ReturnsAsync((Student s) => new Student
                {
                    Id = 10,
                    Name = s.Name,
                    Email = s.Email,
                    Gender = s.Gender
                });

            var (ok, err, created) = await _svc.CreateAsync(dto);

            Assert.That(ok, Is.True);
            Assert.That(err, Is.Null);
            Assert.That(created, Is.Not.Null);
            Assert.That(created!.Id, Is.EqualTo(10));
            Assert.That(created.Name, Is.EqualTo("John"));

            _repo.Verify(r => r.ExistsByEmailAsync("  john@example.com  ", (int?)null), Times.Once);
            _repo.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Once);
        }

        // ---- UpdateAsync ----

        [Test]
        public async Task Update_NotFound()
        {
            var dto = new StudentUpdateDto { Name = "John", Email = "john@example.com", Gender = "Male" };
            _repo.Setup(r => r.ExistsByIdAsync(42)).ReturnsAsync(false);

            var (ok, err) = await _svc.UpdateAsync(42, dto);

            Assert.That(ok, Is.False);
            Assert.That(err, Is.EqualTo("Student not found."));

            _repo.Verify(r => r.ExistsByIdAsync(42), Times.Once);
            _repo.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Update_EmailConflict()
        {
            var dto = new StudentUpdateDto { Name = "John", Email = "john@example.com", Gender = "Male" };
            _repo.Setup(r => r.ExistsByIdAsync(5)).ReturnsAsync(true);
            _repo.Setup(r => r.ExistsByEmailAsync("john@example.com", 5)).ReturnsAsync(true);

            var (ok, err) = await _svc.UpdateAsync(5, dto);

            Assert.That(ok, Is.False);
            Assert.That(err, Is.EqualTo("Email already exists for another student."));

            _repo.Verify(r => r.ExistsByIdAsync(5), Times.Once);
            _repo.Verify(r => r.ExistsByEmailAsync("john@example.com", 5), Times.Once);
            _repo.VerifyNoOtherCalls();
        }

        [Test]
        public async Task Update_TrimAndCallRepo()
        {
            var dto = new StudentUpdateDto { Name = "  John  ", Email = "  john@example.com  ", Gender = "  Male  " };
            _repo.Setup(r => r.ExistsByIdAsync(5)).ReturnsAsync(true);
            _repo.Setup(r => r.ExistsByEmailAsync("  john@example.com  ", 5)).ReturnsAsync(false);

            _repo
                .Setup(r => r.UpdateAsync(It.Is<Student>(s =>
                    s.Id == 5 && s.Name == "John" && s.Email == "john@example.com" && s.Gender == "Male")))
                .Returns(Task.CompletedTask);

            var (ok, err) = await _svc.UpdateAsync(5, dto);

            Assert.That(ok, Is.True);
            Assert.That(err, Is.Null);

            _repo.Verify(r => r.ExistsByIdAsync(5), Times.Once);
            _repo.Verify(r => r.ExistsByEmailAsync("  john@example.com  ", 5), Times.Once);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Student>()), Times.Once);
        }

        // ---- DeleteAsync ----

        [Test]
        public async Task Delete_Ok()
        {
            _repo.Setup(r => r.DeleteAsync(7)).ReturnsAsync(true);

            var (ok, err) = await _svc.DeleteAsync(7);

            Assert.That(ok, Is.True);
            Assert.That(err, Is.Null);

            _repo.Verify(r => r.DeleteAsync(7), Times.Once);
        }

        [Test]
        public async Task Delete_NotFound()
        {
            _repo.Setup(r => r.DeleteAsync(404)).ReturnsAsync(false);

            var (ok, err) = await _svc.DeleteAsync(404);

            Assert.That(ok, Is.False);
            Assert.That(err, Is.EqualTo("Student not found."));

            _repo.Verify(r => r.DeleteAsync(404), Times.Once);
        }

        // ---- DeleteAllAsync ----

        [Test]
        public async Task DeleteAll_Count()
        {
            _repo.Setup(r => r.DeleteAllAsync()).ReturnsAsync(3);

            var count = await _svc.DeleteAllAsync();

            Assert.That(count, Is.EqualTo(3));
            _repo.Verify(r => r.DeleteAllAsync(), Times.Once);
        }
    }
}