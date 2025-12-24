using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NUnit.Framework;
using StudentCRUD.Api.Models;

namespace StudentCRUDApi.Tests.Models
{
    [TestFixture]
    public class StudentTests
    {
        private static IList<ValidationResult> Validate(object model)
        {
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        [Test]
        public void Valid_Student_Should_Pass_Validation()
        {
            var student = new Student
            {
                Id = 1,
                Name = "John Doe",
                Email = "john.doe@example.com",
                Gender = "Male"
            };

            var results = Validate(student);

            Assert.That(results, Is.Empty, "Expected no validation errors for a valid student.");
        }

        [TestCase(null, TestName = "Name_Null_Should_Fail_Required")]
        [TestCase("", TestName = "Name_Empty_Should_Fail_Required")]
        public void Name_NullOrEmpty_Should_Fail_Required(string? name)
        {
            var student = new Student
            {
                Name = name!,
                Email = "john.doe@example.com",
                Gender = "Male"
            };

            var results = Validate(student);

            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Name))));
        }

        [Test]
        public void Name_TooShort_Should_Fail_MinLength()
        {
            var student = new Student
            {
                Name = "A", // length 1 < MinLength(2)
                Email = "john.doe@example.com",
                Gender = "Male"
            };

            var results = Validate(student);

            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Name))));
        }

        [Test]
        public void Name_At_MinLength_2_Should_Pass()
        {
            var student = new Student
            {
                Name = "Al",
                Email = "john.doe@example.com",
                Gender = "Male"
            };

            var results = Validate(student);

            Assert.That(results, Is.Empty);
        }

        [Test]
        public void Name_TooLong_Should_Fail_MaxLength()
        {
            var longName = new string('x', 51); // MaxLength(50)
            var student = new Student
            {
                Name = longName,
                Email = "john.doe@example.com",
                Gender = "Male"
            };

            var results = Validate(student);

            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Name))));
        }

        [Test]
        public void Name_At_MaxLength_50_Should_Pass()
        {
            var name = new string('x', 50);
            var student = new Student
            {
                Name = name,
                Email = "john.doe@example.com",
                Gender = "Male"
            };

            var results = Validate(student);

            Assert.That(results, Is.Empty);
        }

        [TestCase(null, TestName = "Email_Null_Should_Fail_Required")]
        [TestCase("", TestName = "Email_Empty_Should_Fail_Required")]
        public void Email_NullOrEmpty_Should_Fail_Required(string? email)
        {
            var student = new Student
            {
                Name = "John Doe",
                Email = email!,
                Gender = "Male"
            };

            var results = Validate(student);

            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Email))));
        }

        [Test]
        public void Email_TooLong_Should_Fail_MaxLength()
        {
            // Length > 100
            var local = new string('a', 60);
            var domain = new string('b', 41);
            var email = $"{local}@{domain}.com"; // > 100 total
            var student = new Student
            {
                Name = "John Doe",
                Email = email,
                Gender = "Male"
            };

            var results = Validate(student);

            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Email))));
        }

        [Test]
        public void Email_At_MaxLength_100_Should_Pass()
        {
            // Construct exactly 100 characters email:
            // local + '@' + domain + ".com" (4)
            var local = new string('a', 46);
            var domain = new string('b', 49);
            var email = $"{local}@{domain}.com"; // 46 + 1 + 49 + 4 = 100

            var student = new Student
            {
                Name = "John Doe",
                Email = email,
                Gender = "Male"
            };

            var results = Validate(student);

            Assert.That(results, Is.Empty);
        }

        [TestCase(null, TestName = "Gender_Null_Should_Fail_Required")]
        [TestCase("", TestName = "Gender_Empty_Should_Fail_Required")]
        public void Gender_NullOrEmpty_Should_Fail_Required(string? gender)
        {
            var student = new Student
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Gender = gender!
            };

            var results = Validate(student);

            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Gender))));
        }

        [Test]
        public void Gender_TooLong_Should_Fail_MaxLength()
        {
            var gender = new string('x', 21); // MaxLength(20)
            var student = new Student
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Gender = gender
            };

            var results = Validate(student);

            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Gender))));
        }

        [Test]
        public void Gender_At_MaxLength_20_Should_Pass()
        {
            var gender = new string('x', 20);
            var student = new Student
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Gender = gender
            };

            var results = Validate(student);

            Assert.That(results, Is.Empty);
        }

        [Test]
        public void Default_Instance_Should_Fail_All_Required()
        {
            var student = new Student(); // Name/Email/Gender = string.Empty by default

            var results = Validate(student);

            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Name))));
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Email))));
            Assert.That(results, Has.Some.Matches<ValidationResult>(r => r.MemberNames.Contains(nameof(Student.Gender))));
        }
    }
}