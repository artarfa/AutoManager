using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;         // For TokenValidationParameters, SecurityKey
using System.IdentityModel.Tokens.Jwt;       // For JwtSecurityToken, JwtSecurityTokenHandler
using System.Security.Claims;                // For Claim, ClaimTypes
using System.Text;                           // For Encoding
using AutoManager.Models;


[ApiController]
[Route("api/[controller]")]


public class AccountController: ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterModel model)
    {
        var user = new IdentityUser{ UserName = model.Email, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        return Ok("Registered Successfully");
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

    // returns registered users 
    [HttpGet]    
    public async Task<ActionResult> GetAllUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        return Ok(users);
        
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
        
        // var userRoles = await _userManager.GetRolesAsync(user);
        // foreach (var role in userRoles)
        // {
        //     authClaims.Add(new Claim(ClaimTypes.Role, role));
        // }
        
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