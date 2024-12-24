//using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AutoManager.Models;



namespace AutoManager.Data
{
    public class ApiContext: DbContext
    {
        public DbSet<Requirement> Requirements { get; set; }
        


        public ApiContext(DbContextOptions<ApiContext> options)
            : base(options)
        {

        }
    }
}
