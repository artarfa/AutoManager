using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoManager.Models;
using AutoManager.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace AutoManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequirementsController : ControllerBase
    {
        private readonly ApiContext _context;
        // Reuse a static HttpClient for performance.
        private static readonly HttpClient httpClient = new HttpClient();

        private const double SimilarityThresholdForContradiction = 0.6;
        private const string FlagEmoji = " 🚩";
        public RequirementsController(ApiContext context)
        {
            _context = context;
            // Start the Python service in the background if not already running.
            PythonServiceManager.StartService();
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Requirement>>> GetRequirements()
        {
            return await _context.Requirements.ToListAsync();
        }
        
        // Creates a new requirement.
        [HttpPost]
        public async Task<IActionResult> Create(Requirement requirement)
        {
            // Retrieve all existing requirements.
            var existingRequirements = _context.Requirements.ToList();
            var warnings = new List<string>();
            
            
            foreach (var existing in existingRequirements)
            {
                // Compute cosine similarity using the Python similarity service.
                double similarity = await GetSimilarityUsingPythonAsync(requirement.Description, existing.Description);
                Console.WriteLine($"Computed similarity with requirement ID {existing.Id}: {similarity}");
                // Using a threshold of 0.85 for similarity check.
                if (similarity >= 0.95)
                {
                    warnings.Add($"Requirement similar to ID {existing.Id} (Similarity: {similarity:F2})");
                    requirement.Description = AppendFlag(requirement.Description);
                    existing.Description = AppendFlag(existing.Description);
                    _context.Requirements.Update(existing);
                    //return BadRequest($"Requirement is too similar to ID: {existing.Id}. Similarity: {similarity:F2}");
                }
                
                // First we check if the 2 requirements are atleast somewhat similar.
                if (similarity >= SimilarityThresholdForContradiction)
                {
                    double contradiction = await GetContradictionUsingPythonAsync(requirement.Description, existing.Description);
                    Console.WriteLine($"Computed contradiction with requirement ID {existing.Id}: {contradiction}");
                    // Using a threshold of 0.8 for contradiction check.
                    if (contradiction >= 0.9)
                    {
                        warnings.Add($"Requirement contradicts ID {existing.Id} Contradiction score: {contradiction:F2}");
                        requirement.Description = AppendFlag(requirement.Description);
                        existing.Description = AppendFlag(existing.Description);
                        _context.Requirements.Update(existing);

                        //return BadRequest($"New requirement contradicts requirement ID: {existing.Id}. Contradiction score: {contradiction:F2}");
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping contradiction check with requirement ID {existing.Id} due to low similarity ({similarity:F2}).");
                }
            }
            
            if (requirement.Id == 0)
            {
                _context.Requirements.Add(requirement);
            }
            await _context.SaveChangesAsync();
            return Ok(new { requirement, warnings });
        }
        
        // Compare the text descriptions of two requirements.
        [HttpGet("compare/{id1}/{id2}")]
        public async Task<IActionResult> CompareDescriptions(int id1, int id2)
        {
            // Retrieve both requirements from the database.
            var req1 = await _context.Requirements.FindAsync(id1);
            var req2 = await _context.Requirements.FindAsync(id2);

            if (req1 == null || req2 == null)
            {
                return NotFound("One or both requirements not found");
            }

            double similarity = await GetSimilarityUsingPythonAsync(req1.Description, req2.Description);
            double contradiction = await GetContradictionUsingPythonAsync(req1.Description, req2.Description);
            return Ok(new { similarity, contradiction });
        }
        
        // Update an existing requirement.
        [HttpPut]
        public async Task<IActionResult> Update(Requirement requirement)
        {
            var existingRequirements = _context.Requirements.AsNoTracking().ToList();
            string cleanDescription = RemoveFlag(requirement.Description);
            
            foreach (var existing in existingRequirements)
            {
                // Skip comparing with itself.
                if (existing.Id == requirement.Id)
                    continue;
                    
                string cleanExisting = RemoveFlag(existing.Description);
                double similarity = await GetSimilarityUsingPythonAsync(cleanDescription, cleanExisting);
                
                Console.WriteLine($"Computed similarity with requirement ID {existing.Id}: {similarity}");
                if (similarity >= 0.95)
                {
                    return BadRequest(
                        $"Updated requirement is too similar to ID: {existing.Id}. Similarity: {similarity:F2}");
                }
                if (similarity >= SimilarityThresholdForContradiction)
                {
                    double contradiction = await GetContradictionUsingPythonAsync(cleanDescription, cleanExisting);
                    Console.WriteLine($"Computed contradiction with requirement ID {existing.Id}: {contradiction}");
                    if (contradiction >= 0.9)
                    {
                        return BadRequest(
                            $"Updated requirement contradicts requirement ID: {existing.Id}. Contradiction score: {contradiction:F2}");
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping contradiction check with requirement ID {existing.Id} due to low similarity ({similarity:F2}).");
                }
            }
            
            _context.Requirements.Update(requirement);
            await _context.SaveChangesAsync();
            return Ok(requirement);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Requirement>> GetRequirement(int id)
        {
            var requirement = await _context.Requirements.FindAsync(id);
            if (requirement == null)
            {
                return NotFound();
            }
            return requirement;
        }

        // Remove a requirement.
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var requirement = await _context.Requirements.FindAsync(id);
            if (requirement == null)
            {
                return NotFound("Requirement does not exist");
            }
            _context.Requirements.Remove(requirement);
            await _context.SaveChangesAsync();
            return Ok("Requirement has been removed");
        }
        
        
        // Helper method to call the Python AI model service for similarity checking.
        private async Task<double> GetSimilarityUsingPythonAsync(string text1, string text2)
        {
            try
            {
                var url = "http://localhost:5001/similarity"; // Similarity service running on port 5001
                var payload = new { text1 = text1, text2 = text2 };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Python similarity service response: " + responseJson);
                    var similarityResponse = JsonConvert.DeserializeObject<SimilarityResponse>(responseJson);
                    return similarityResponse.similarity;
                }
                else
                {
                    Console.Error.WriteLine("Python similarity service error: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in GetSimilarityUsingPythonAsync: " + ex.Message);
            }
            return 0.0;
        }
        
        // Helper method to call the Python AI model service for contradiction checking.
        private async Task<double> GetContradictionUsingPythonAsync(string text1, string text2)
        {
            try
            {
                var url = "http://localhost:5002/contradiction"; // Contradiction service running on port 5002
                var payload = new { text1 = text1, text2 = text2 };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Python contradiction service response: " + responseJson);
                    // Expected JSON response: {"contradiction": <value>, "neutral": <value>, "entailment": <value>}
                    var contradictionResponse = JsonConvert.DeserializeObject<ContradictionResponse>(responseJson);
                    return contradictionResponse.contradiction;
                }
                else
                {
                    Console.Error.WriteLine("Python contradiction service error: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in GetContradictionUsingPythonAsync: " + ex.Message);
            }
            return 0.0;
        }

        [HttpPost("remove-all-flags")]
        public async Task<IActionResult> RemoveAllFlags()
        {
            var requirements = await _context.Requirements.ToListAsync();
            foreach (var req in requirements)
            {
                req.Description = RemoveFlag(req.Description);
                _context.Requirements.Update(req);
            }
            await _context.SaveChangesAsync();
            return Ok("All flags removed.");
        }
        
        
        private string RemoveFlag(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return description;
            return description.EndsWith(FlagEmoji)
                ? description.Substring(0, description.Length - FlagEmoji.Length)
                : description;
        }
        
        private string AppendFlag(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return description;
            string trimmed = description.TrimEnd();
            if (!trimmed.EndsWith(FlagEmoji))
            {
                return trimmed + FlagEmoji;
            }
            return description;
        }
    }
    
    // Helper class to manage the Python service process.
    public static class PythonServiceManager
    {
        private static Process similarityProcess;
        private static Process contradictionProcess;
    
        public static void StartService()
        {
            if (similarityProcess == null || similarityProcess.HasExited)
            {
                var psiSimilarity = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = "ai_similarity.py",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                similarityProcess = Process.Start(psiSimilarity);
                // Optionally, capture output for debugging.
            }
        
            if (contradictionProcess == null || contradictionProcess.HasExited)
            {
                var psiContradiction = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = "ai_contradiction.py",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                contradictionProcess = Process.Start(psiContradiction);
            }
        
            // Wait a short period for the services to be ready (adjust as necessary).
            Thread.Sleep(2000);
        }
    
        public static void StopService()
        {
            if (similarityProcess != null && !similarityProcess.HasExited)
            {
                similarityProcess.Kill();
                similarityProcess.WaitForExit();
                similarityProcess = null;
            }
            if (contradictionProcess != null && !contradictionProcess.HasExited)
            {
                contradictionProcess.Kill();
                contradictionProcess.WaitForExit();
                contradictionProcess = null;
            }
        }
    }
}