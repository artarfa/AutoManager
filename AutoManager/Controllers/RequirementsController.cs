using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoManager.Models;
using AutoManager.Data;


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

        // Create/Update
        [HttpPost]
        public JsonResult CreateEdit(Requirement requirement)
        {
            if (requirement.Id == 0)
            {
                _context.Requirements.Add(requirement);
            }
            else
            {
                var requirementInDb = _context.Requirements.Find(requirement.Id);
                if (requirementInDb == null)
                    return new JsonResult(NotFound());

                requirementInDb = requirement;
            }
            _context.SaveChanges();
            return new JsonResult(Ok(requirement));
        }



        // Read by ID
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Requirements.Find(id);
            if (result == null)
            {
                return new JsonResult(NotFound());
            }
            return new JsonResult(Ok(result));
        }


        // Remove
        [HttpDelete]
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