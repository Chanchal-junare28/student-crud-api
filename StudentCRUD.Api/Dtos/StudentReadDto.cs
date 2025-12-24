namespace StudentCRUD.Api.DTOs
{
    public class StudentReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Gender { get; set; } = "";
    }
}
