using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using AutoManager.Models;
using Microsoft.EntityFrameworkCore;


[ApiController]
[Route("api/[controller]")]


public class AccountController: ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (!result.Succeeded)
        {
            return Unauthorized("Invalid login attempt");
        }

        return Ok("Login successful");

    }

    // returns registered users 
    [HttpGet]    
    public async Task<ActionResult> GetAllUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        return Ok(users);
        
    }
    
    
}