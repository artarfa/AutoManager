using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;         // For TokenValidationParameters, SecurityKey
using System.IdentityModel.Tokens.Jwt;       // For JwtSecurityToken, JwtSecurityTokenHandler
using System.Security.Claims;                // For Claim, ClaimTypes
using System.Text;                           // For Encoding
using AutoManager.Models;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]


public class AccountController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;


    public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }
    


    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterModel model)
    {
        var user = new IdentityUser{ UserName = model.Email, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            
            await _userManager.AddToRoleAsync(user, "User");
            return Ok("User registered successfully");
        }
        return BadRequest("Registration failed");
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return Unauthorized("Invalid login attempt. User not found.");
        }
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (!result.Succeeded)
        {
            return Unauthorized("Invalid login attempt. Incorrect password.");
        }
    
        var tokenString = await GenerateJwtToken(user);
        return Ok(new { token = tokenString });

    }
    
    // Assigning roles
    //[Authorize (Roles = "Admin")]
    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRoleToUser([FromBody] RoleModel roleModel)
    {
        var user = await _userManager.FindByEmailAsync(roleModel.Email);
        if (user == null)
        {
            return NotFound("User not found");
        }
        

        var result = await _userManager.AddToRoleAsync(user, roleModel.Role);
        
        if (result.Succeeded)
        {
            return Ok($"Role '{roleModel.Role}' assigned to user '{roleModel.Email}' successfully.");
        }
        
        var errors = result.Errors.Select(e => e.Description);
        return BadRequest(errors);
        
    }
    
    

    // returns registered users 
    [HttpGet]    
    public async Task<ActionResult> GetAllUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var userList = users.Select(u => new { u.Email, Roles = _userManager.GetRolesAsync(u).Result });
        
        
        return Ok(userList);
        
    }

    private async Task<string> GenerateJwtToken(IdentityUser user)
    {
        var JwtSettings = _configuration.GetSection("Jwt");
        var issuer = JwtSettings["Issuer"];
        var audience = JwtSettings["Audience"];
        var secretkey = JwtSettings["Key"];

        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        { 
            authClaims.Add(new Claim(ClaimTypes.Role, role)); 
        }
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretkey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            expires: DateTime.Now.AddHours(2),
            claims: authClaims,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
}