using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StudentCRUD.Api.Data;
using StudentCRUD.Api.Models;
using StudentCRUD.Api.Repositories;

namespace StudentCRUD.Api.Tests.Repositories
{
    [TestFixture]
    public class StudentRepositoryTests
    {
        private static ApplicationDbContext NewCtx()
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(opts);
        }

        private static void Seed(ApplicationDbContext ctx, IEnumerable<Student> items)
        {
            ctx.Students.AddRange(items);
            ctx.SaveChanges();
        }

        [Test]
        public async Task GetAll_Filter()
        {
            using var ctx = NewCtx();
            Seed(ctx, new[]
            {
                new Student { Name = "Alice",  Email = "alice@x.com",  Gender = "Female" },
                new Student { Name = "Bob",    Email = "bob@x.com",    Gender = "Male"   },
                new Student { Name = "John",   Email = "john@x.com",   Gender = "Male"   },
                new Student { Name = "Johnny", Email = "johnny@x.com", Gender = "Male"   },
            });

            var repo = new StudentRepository(ctx);

            var res = await repo.GetAllAsync("john");
            Assert.That(res.Count, Is.EqualTo(2));
            Assert.That(res.Select(s => s.Name), Is.EqualTo(new[] { "John", "Johnny" })); // ordered by Name
        }

        [Test]
        public async Task GetAll_Page()
        {
            using var ctx = NewCtx();
            Seed(ctx, new[]
            {
                new Student { Name = "Alice",  Email = "alice@x.com",  Gender = "Female" },
                new Student { Name = "Bob",    Email = "bob@x.com",    Gender = "Male"   },
                new Student { Name = "John",   Email = "john@x.com",   Gender = "Male"   },
                new Student { Name = "Johnny", Email = "johnny@x.com", Gender = "Male"   },
                new Student { Name = "Zoe",    Email = "zoe@x.com",    Gender = "Female" },
            });

            var repo = new StudentRepository(ctx);

            var res = await repo.GetAllAsync(name: null, page: 2, pageSize: 2);
            Assert.That(res.Select(s => s.Name), Is.EqualTo(new[] { "John", "Johnny" })); // order: Alice, Bob, John, Johnny, Zoe
        }

        [Test]
        public async Task GetAll_Order()
        {
            using var ctx = NewCtx();
            Seed(ctx, new[]
            {
                new Student { Name = "Zoe",    Email = "zoe@x.com",    Gender = "Female" },
                new Student { Name = "Alice",  Email = "alice@x.com",  Gender = "Female" },
                new Student { Name = "Bob",    Email = "bob@x.com",    Gender = "Male"   },
            });

            var repo = new StudentRepository(ctx);

            var res = await repo.GetAllAsync(null);
            Assert.That(res.Select(s => s.Name), Is.EqualTo(new[] { "Alice", "Bob", "Zoe" }));
        }

        [Test]
        public async Task Count_Filter()
        {
            using var ctx = NewCtx();
            Seed(ctx, new[]
            {
                new Student { Name = "John",   Email = "john@x.com",   Gender = "Male"   },
                new Student { Name = "Johnny", Email = "johnny@x.com", Gender = "Male"   },
                new Student { Name = "Alice",  Email = "alice@x.com",  Gender = "Female" },
            });

            var repo = new StudentRepository(ctx);

            var c1 = await repo.GetCountAsync("john");
            var c2 = await repo.GetCountAsync(null);

            Assert.That(c1, Is.EqualTo(2));
            Assert.That(c2, Is.EqualTo(3));
        }

        [Test]
        public async Task Get_Id()
        {
            using var ctx = NewCtx();
            var s = new Student { Name = "Alice", Email = "alice@x.com", Gender = "Female" };
            Seed(ctx, new[] { s });

            var repo = new StudentRepository(ctx);
            var found = await repo.GetByIdAsync(s.Id);

            Assert.That(found, Is.Not.Null);
            Assert.That(found!.Name, Is.EqualTo("Alice"));
        }

        [Test]
        public async Task Get_Id_NotFound()
        {
            using var ctx = NewCtx();
            var repo = new StudentRepository(ctx);

            var found = await repo.GetByIdAsync(999);
            Assert.That(found, Is.Null);
        }

        [Test]
        public async Task Exists_Id()
        {
            using var ctx = NewCtx();
            var s = new Student { Name = "Bob", Email = "bob@x.com", Gender = "Male" };
            Seed(ctx, new[] { s });

            var repo = new StudentRepository(ctx);

            Assert.That(await repo.ExistsByIdAsync(s.Id), Is.True);
            Assert.That(await repo.ExistsByIdAsync(999), Is.False);
        }

        [Test]
        public async Task Exists_Email()
        {
            using var ctx = NewCtx();
            var s = new Student { Name = "John", Email = "john@x.com", Gender = "Male" };
            Seed(ctx, new[] { s });

            var repo = new StudentRepository(ctx);

            // case/space-insensitive
            Assert.That(await repo.ExistsByEmailAsync("  JOHN@x.com  ", null), Is.True);
        }

        [Test]
        public async Task Exists_Email_Exclude()
        {
            using var ctx = NewCtx();
            var s = new Student { Name = "John", Email = "john@x.com", Gender = "Male" };
            Seed(ctx, new[] { s });

            var repo = new StudentRepository(ctx);

            // exclude same id => should ignore the existing one
            Assert.That(await repo.ExistsByEmailAsync("john@x.com", s.Id), Is.False);

            // another student with same email -> true
            var t = new Student { Name = "Jane", Email = "john@x.com", Gender = "Female" };
            Seed(ctx, new[] { t });

            Assert.That(await repo.ExistsByEmailAsync("john@x.com", s.Id), Is.True);
        }

        [Test]
        public async Task Add()
        {
            using var ctx = NewCtx();
            var repo = new StudentRepository(ctx);

            var created = await repo.AddAsync(new Student
            {
                Name = "New",
                Email = "new@x.com",
                Gender = "Male"
            });

            Assert.That(created.Id, Is.GreaterThan(0));
            var all = await ctx.Students.AsNoTracking().ToListAsync();
            Assert.That(all.Count, Is.EqualTo(1));
            Assert.That(all[0].Name, Is.EqualTo("New"));
        }

        [Test]
        public async Task Update()
        {
            using var ctx = NewCtx();
            var s = new Student { Name = "Old", Email = "old@x.com", Gender = "Male" };
            Seed(ctx, new[] { s });

            var repo = new StudentRepository(ctx);

            await repo.UpdateAsync(new Student
            {
                Id = s.Id,
                Name = "New",
                Email = "new@x.com",
                Gender = "Female"
            });

            var found = await ctx.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == s.Id);
            Assert.That(found!.Name, Is.EqualTo("New"));
            Assert.That(found.Email, Is.EqualTo("new@x.com"));
            Assert.That(found.Gender, Is.EqualTo("Female"));
        }

        [Test]
        public async Task Del_Ok()
        {
            using var ctx = NewCtx();
            var s = new Student { Name = "Del", Email = "del@x.com", Gender = "Male" };
            Seed(ctx, new[] { s });

            var repo = new StudentRepository(ctx);
            var ok = await repo.DeleteAsync(s.Id);

            Assert.That(ok, Is.True);
            Assert.That(await ctx.Students.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task Del_NotFound()
        {
            using var ctx = NewCtx();
            var repo = new StudentRepository(ctx);

            var ok = await repo.DeleteAsync(999);
            Assert.That(ok, Is.False);
        }

        // ---- DeleteAllAsync ----

        [Test]
        public async Task DelAll()
        {
            using var ctx = NewCtx();
            Seed(ctx, new[]
            {
                new Student { Name = "A", Email = "a@x.com", Gender = "Female" },
                new Student { Name = "B", Email = "b@x.com", Gender = "Male" },
                new Student { Name = "C", Email = "c@x.com", Gender = "Female" },
            });

            var repo = new StudentRepository(ctx);
            var count = await repo.DeleteAllAsync();

            Assert.That(count, Is.EqualTo(3));
            Assert.That(await ctx.Students.CountAsync(), Is.EqualTo(0));
        }
    }
}