using System.ComponentModel.DataAnnotations;

namespace StudentCRUD.Api.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required, MinLength(2), MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Gender { get; set; } = string.Empty;
    }
}