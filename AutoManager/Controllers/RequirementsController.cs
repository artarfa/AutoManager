using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoManager.Models;
using AutoManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

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
        public IActionResult Create(Requirement requirement)
        {
            // Retrieve all existing requirements.
            var existingRequirements = _context.Requirements.ToList();
            foreach (var existing in existingRequirements)
            {
                // Compute cosine similarity using the Python AI model.
                double similarity = GetSimilarityUsingPython(requirement.Description, existing.Description);
                // Using a threshold of 0.9 for similarity check.
                if (similarity >= 0.9)
                {
                    return BadRequest($"New requirement is too similar to ID: {existing.Id}. Similarity: {similarity:F2}");
                }
            }
            
            if (requirement.Id == 0)
            {
                _context.Requirements.Add(requirement);
            }
            _context.SaveChanges();
            return new JsonResult(Ok(requirement));
        }
        
        // Compare the text descriptions of two requirements.
        [HttpGet("compare/{id1}/{id2}")]
        public async Task<IActionResult> CompareDescriptions(int id1, int id2)
        {
            // Retrieve both requirements from the database
            var req1 = await _context.Requirements.FindAsync(id1);
            var req2 = await _context.Requirements.FindAsync(id2);

            if (req1 == null || req2 == null)
            {
                return NotFound("One or both requirements not found");
            }

            double similarity = GetSimilarityUsingPython(req1.Description, req2.Description);
            return Ok(new { similarity });
        }
        
        // Update an existing requirement.
        [HttpPut]
        public IActionResult Update(Requirement requirement)
        {
            var existingRequirements = _context.Requirements.AsNoTracking().ToList();
            foreach (var existing in existingRequirements)
            {
                // Skip comparing with itself.
                if (existing.Id == requirement.Id)
                    continue;

                double similarity = GetSimilarityUsingPython(requirement.Description, existing.Description);
                if (similarity >= 0.9)
                {
                    return BadRequest($"Updated requirement is too similar to ID: {existing.Id}. Similarity: {similarity:F2}");
                }
            }
            
            _context.Requirements.Update(requirement);
            _context.SaveChanges();
            return Ok(requirement);
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

        // Remove a requirement.
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
        
        private double GetSimilarityUsingPython(string text1, string text2)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python3", 
                    Arguments = $"ai_similarity.py \"{EscapeArgument(text1)}\" \"{EscapeArgument(text2)}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        // Log error details for debugging.
                        Console.Error.WriteLine("Python error: " + error);
                    }

                    // Use InvariantCulture to parse the output (which uses a dot as decimal separator)
                    if (double.TryParse(output.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double similarity))
                    {
                        return similarity;
                    }
                    else
                    {
                        Console.Error.WriteLine("Failed to parse similarity from output: " + output);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception details.
                Console.Error.WriteLine("Exception in GetSimilarityUsingPython: " + ex.Message);
            }
            return 0.0;
        }
        // Helper to escape quotes in the text arguments.
        private string EscapeArgument(string arg)
        {
            return arg.Replace("\"", "\\\"");
        }
    }
}