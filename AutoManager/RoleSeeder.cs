namespace AutoManager;
using Microsoft.AspNetCore.Identity;


public static class RoleSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        if (!await roleManager.RoleExistsAsync("Analyst"))
        {
            await roleManager.CreateAsync(new IdentityRole("Analyst"));
        }

        if (!await roleManager.RoleExistsAsync("Engineer"))
        {
            await roleManager.CreateAsync(new IdentityRole("Engineer"));
        }
    }
}