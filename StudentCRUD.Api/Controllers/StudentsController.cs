using Microsoft.AspNetCore.Mvc;
using StudentCRUD.Api.DTOs;
using StudentCRUD.Api.Services;

namespace StudentCRUD.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _service;
        public StudentsController(IStudentService service) => _service = service;

        // GET /api/students?name=John&page=1&pageSize=20
        [HttpGet]
        public async Task<ActionResult<PagedResult<StudentReadDto>>> GetAll(
            [FromQuery] string? name,
            [FromQuery] int? page,
            [FromQuery] int? pageSize)
        {
            var result = await _service.GetAllAsync(name, page, pageSize);
            return Ok(result);
        }

        // GET /api/students/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<StudentReadDto>> Get(int id)
        {
            var s = await _service.GetAsync(id);
            return s is null ? NotFound() : Ok(s);
        }

        // POST /api/students
        [HttpPost]
        public async Task<ActionResult<StudentReadDto>> Create([FromBody] StudentCreateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var (ok, error, created) = await _service.CreateAsync(dto);
            if (!ok) return Conflict(new { message = error }); // 409 for uniqueness
            return CreatedAtAction(nameof(Get), new { id = created!.Id }, created);
        }

        // PUT /api/students/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] StudentUpdateDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var (ok, error) = await _service.UpdateAsync(id, dto);
            if (!ok)
            {
                return error == "Student not found."
                    ? NotFound(new { message = error })
                    : Conflict(new { message = error });
            }
            return Ok(new { message = "Student updated successfully!" });
        }

        // DELETE /api/students/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (ok, error) = await _service.DeleteAsync(id);
            return ok ? Ok(new { message = "Student deleted successfully!" })
                      : NotFound(new { message = error });
        }

        // DELETE /api/students
        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            var count = await _service.DeleteAllAsync();
            return Ok(new { message = $"Deleted {count} students." });
        }
    }
}
