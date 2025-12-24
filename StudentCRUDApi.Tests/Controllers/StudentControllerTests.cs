using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using StudentCRUD.Api.Controllers;
using StudentCRUD.Api.DTOs;
using StudentCRUD.Api.Services;

namespace StudentCRUDApi.Tests.Controllers
{
    [TestFixture]
    public class StudentsControllerTests
    {
        private Mock<IStudentService> _serviceMock = default!;
        private StudentsController _controller = default!;

        [SetUp]
        public void SetUp()
        {
            _serviceMock = new Mock<IStudentService>(MockBehavior.Strict);
            _controller = new StudentsController(_serviceMock.Object);
        }

        private static string? ReadMessageFromAnonymous(ObjectResult obj)
        {
            var prop = obj.Value?.GetType().GetProperty("message");
            return (string?)prop?.GetValue(obj.Value!);
        }

        [Test]
        public async Task GetAll_Should_Return_Ok_With_Result_And_Pass_Query_Params()
        {
            // Arrange
            var expected = new PagedResult<StudentReadDto>
            {
                Items = new List<StudentReadDto>
                {
                    new StudentReadDto { Id = 1, Name = "John", Email = "john@example.com", Gender = "Male" }
                },
                Page = 1,
                PageSize = 20,
                TotalCount = 1
            };

            _serviceMock
                .Setup(s => s.GetAllAsync("John", 1, 20))
                .ReturnsAsync(expected);

            // Act
            var action = await _controller.GetAll(name: "John", page: 1, pageSize: 20);

            // Assert
            Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)action.Result!;
            Assert.That(ok.Value, Is.SameAs(expected));

            _serviceMock.Verify(s => s.GetAllAsync("John", 1, 20), Times.Once);
        }

        [Test]
        public async Task Get_When_Student_Exists_Should_Return_Ok_With_Dto()
        {
            // Arrange
            var dto = new StudentReadDto { Id = 5, Name = "Alice", Email = "alice@example.com", Gender = "Female" };
            _serviceMock.Setup(s => s.GetAsync(5)).ReturnsAsync(dto);

            // Act
            var action = await _controller.Get(5);

            // Assert
            Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)action.Result!;
            Assert.That(ok.Value, Is.SameAs(dto));

            _serviceMock.Verify(s => s.GetAsync(5), Times.Once);
        }

        [Test]
        public async Task Get_When_Student_NotFound_Should_Return_404()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetAsync(999)).ReturnsAsync((StudentReadDto?)null);

            // Act
            var action = await _controller.Get(999);

            // Assert
            Assert.That(action.Result, Is.InstanceOf<NotFoundResult>());

            _serviceMock.Verify(s => s.GetAsync(999), Times.Once);
        }

        [Test]
        public async Task Create_When_Service_Returns_Conflict_Should_Return_409_With_Message()
        {
            // Arrange
            var dto = new StudentCreateDto { Name = "John", Email = "john@example.com", Gender = "Male" };
            _serviceMock
                .Setup(s => s.CreateAsync(dto))
                .ReturnsAsync((ok: false, error: "Email already exists.", created: (StudentReadDto?)null));

            // Make ModelState valid (no errors)
            // Act
            var action = await _controller.Create(dto);

            // Assert
            Assert.That(action.Result, Is.InstanceOf<ConflictObjectResult>());
            var conflict = (ConflictObjectResult)action.Result!;
            var msg = ReadMessageFromAnonymous(conflict);
            Assert.That(msg, Is.EqualTo("Email already exists."));

            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Test]
        public async Task Create_When_Success_Should_Return_201_CreatedAtAction_With_Created_Dto()
        {
            // Arrange
            var dto = new StudentCreateDto { Name = "John", Email = "john@example.com", Gender = "Male" };
            var created = new StudentReadDto { Id = 5, Name = "John", Email = "john@example.com", Gender = "Male" };

            _serviceMock
                .Setup(s => s.CreateAsync(dto))
                .ReturnsAsync((ok: true, error: (string?)null, created: created));

            // Act
            var action = await _controller.Create(dto);

            // Assert
            Assert.That(action.Result, Is.InstanceOf<CreatedAtActionResult>());
            var createdResult = (CreatedAtActionResult)action.Result!;
            Assert.That(createdResult.ActionName, Is.EqualTo(nameof(StudentsController.Get)));
            Assert.That(createdResult.RouteValues!["id"], Is.EqualTo(created.Id));
            Assert.That(createdResult.Value, Is.SameAs(created));

            _serviceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Test]
        public async Task Update_When_Student_NotFound_Should_Return_404_With_Message()
        {
            // Arrange
            var dto = new StudentUpdateDto { Name = "John", Email = "john@example.com", Gender = "Male" };
            _serviceMock
                .Setup(s => s.UpdateAsync(42, dto))
                .ReturnsAsync((ok: false, error: "Student not found."));

            // Act
            var action = await _controller.Update(42, dto);

            // Assert
            Assert.That(action, Is.InstanceOf<NotFoundObjectResult>());
            var notFound = (NotFoundObjectResult)action;
            var msg = ReadMessageFromAnonymous(notFound);
            Assert.That(msg, Is.EqualTo("Student not found."));

            _serviceMock.Verify(s => s.UpdateAsync(42, dto), Times.Once);
        }

        [Test]
        public async Task Update_When_Conflict_Should_Return_409_With_Message()
        {
            // Arrange
            var dto = new StudentUpdateDto { Name = "John", Email = "john@example.com", Gender = "Male" };
            _serviceMock
                .Setup(s => s.UpdateAsync(5, dto))
                .ReturnsAsync((ok: false, error: "Email already exists."));

            // Act
            var action = await _controller.Update(5, dto);

            // Assert
            Assert.That(action, Is.InstanceOf<ConflictObjectResult>());
            var conflict = (ConflictObjectResult)action;
            var msg = ReadMessageFromAnonymous(conflict);
            Assert.That(msg, Is.EqualTo("Email already exists."));

            _serviceMock.Verify(s => s.UpdateAsync(5, dto), Times.Once);
        }

        [Test]
        public async Task Update_When_Success_Should_Return_200_Ok_With_Message()
        {
            // Arrange
            var dto = new StudentUpdateDto { Name = "John", Email = "john@example.com", Gender = "Male" };
            _serviceMock
                .Setup(s => s.UpdateAsync(5, dto))
                .ReturnsAsync((ok: true, error: (string?)null));

            // Act
            var action = await _controller.Update(5, dto);

            // Assert
            Assert.That(action, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)action;
            var msg = ReadMessageFromAnonymous(ok);
            Assert.That(msg, Is.EqualTo("Student updated successfully!"));

            _serviceMock.Verify(s => s.UpdateAsync(5, dto), Times.Once);
        }

        [Test]
        public async Task Delete_When_Success_Should_Return_200_Ok_With_Message()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteAsync(7))
                .ReturnsAsync((ok: true, error: (string?)null));

            // Act
            var action = await _controller.Delete(7);

            // Assert
            Assert.That(action, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)action;
            var msg = ReadMessageFromAnonymous(ok);
            Assert.That(msg, Is.EqualTo("Student deleted successfully!"));

            _serviceMock.Verify(s => s.DeleteAsync(7), Times.Once);
        }

        [Test]
        public async Task Delete_When_NotFound_Should_Return_404_With_Message()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteAsync(404))
                .ReturnsAsync((ok: false, error: "Student not found."));

            // Act
            var action = await _controller.Delete(404);

            // Assert
            Assert.That(action, Is.InstanceOf<NotFoundObjectResult>());
            var nf = (NotFoundObjectResult)action;
            var msg = ReadMessageFromAnonymous(nf);
            Assert.That(msg, Is.EqualTo("Student not found."));

            _serviceMock.Verify(s => s.DeleteAsync(404), Times.Once);
        }

        [Test]
        public async Task DeleteAll_Should_Return_200_Ok_With_Count_In_Message()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteAllAsync())
                .ReturnsAsync(3);

            // Act
            var action = await _controller.DeleteAll();

            // Assert
            Assert.That(action, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)action;
            var msg = ReadMessageFromAnonymous(ok);
            Assert.That(msg, Is.EqualTo("Deleted 3 students."));

            _serviceMock.Verify(s => s.DeleteAllAsync(), Times.Once);
        }
    }
}