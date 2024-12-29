using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoManager.Models;
using AutoManager.Data;
using Microsoft.EntityFrameworkCore;


namespace AutoManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequirementsController : ControllerBase
    {
        private readonly ApiContext _context;
        public RequirementsController(ApiContext context)
        {
            _context = context;
        }
        
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Requirement>>> GetRequirements()
        {
            return await _context.Requirements.ToListAsync();
        }
        
        // Creates a new requirement
        [HttpPost]
        public JsonResult Create(Requirement requirement)
        {
            if (requirement.Id == 0) 
            {
                _context.Requirements.Add(requirement);
            }

            _context.SaveChanges();
            return new JsonResult(Ok(requirement));
        }
        
        // Update
        [HttpPut]
        public JsonResult Update(Requirement requirement)
        {
            _context.Requirements.Update(requirement);
            _context.SaveChanges();
            return new JsonResult(Ok(requirement));
        }
        

        [HttpGet("{id}")]
        public async Task<ActionResult<Requirement>> GetRequirement(int id)
        {
            var requirement = await _context.Requirements.FindAsync(id);
            if (requirement == null)
            {
                return new JsonResult(NotFound());
            }
            return new JsonResult(requirement);
        }


        // Remove
        [HttpDelete("{id}")]
        public JsonResult Delete(int id)
        {
            var result = _context.Requirements.Find(id);
            if (result == null)
            {
                return new JsonResult("Requirement does not exist");
            }
            _context.Requirements.Remove(result);
            _context.SaveChanges();
            return new JsonResult("Requirement has been removed");
        }
    }
}