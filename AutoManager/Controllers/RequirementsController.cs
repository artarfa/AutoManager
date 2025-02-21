using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoManager.Models;
using AutoManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

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
            // Initialize MLContext and define the text featurization pipeline.
            var mlContext = new MLContext();
            var pipeline = mlContext.Transforms.Text.FeaturizeText(
                outputColumnName: "Features", inputColumnName: nameof(TextData.Text)
            );

            // Create feature vector for the new requirement.
            var newData = new List<TextData> { new TextData { Text = requirement.Description } };
            var newDataView = mlContext.Data.LoadFromEnumerable(newData);
            var model = pipeline.Fit(newDataView);
            var newTransformed = model.Transform(newDataView);
            var newFeatures = mlContext.Data.CreateEnumerable<TransformedText>(newTransformed, reuseRowObject: false)
                                          .First().Features.Select(f => (double)f).ToArray();

            // Retrieve all existing requirements.
            var existingRequirements = _context.Requirements.ToList();
            foreach (var existing in existingRequirements)
            {
                // Generate feature vector for each existing requirement.
                var existingData = new List<TextData> { new TextData { Text = existing.Description } };
                var existingDataView = mlContext.Data.LoadFromEnumerable(existingData);
                var existingTransformed = model.Transform(existingDataView);
                var existingFeatures = mlContext.Data.CreateEnumerable<TransformedText>(existingTransformed, reuseRowObject: false)
                                                     .First().Features.Select(f => (double)f).ToArray();

                // Compute cosine similarity.
                double similarity = CosineSimilarity(newFeatures, existingFeatures);
                // Using threshold of 0.8 for similarity check.
                if (similarity >= 0.8)
                {
                    return BadRequest($"New requirement is too similar to ID: {existing.Id}. Similarity: {similarity:F2}");
                }
                
            }

            // No similar requirement found so we can add
            if (requirement.Id == 0)
            {
                _context.Requirements.Add(requirement);
            }
            _context.SaveChanges();
            return new JsonResult(Ok(requirement));
        }

        
        // Helper function for calculating cosine similarity between 2 requirement descriptions.
        private static double CosineSimilarity(double[] vectorA, double[] vectorB)
        {
            double dotProduct = 0.0;
            double magnitudeA = 0.0;
            double magnitudeB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += Math.Pow(vectorA[i], 2);
                magnitudeB += Math.Pow(vectorB[i], 2);
            }
            // If a vector has zero magnitude, the similarity is undefined so return 0.
            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;
            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }

        // Compare the text features (descriptions) of two requirements.
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

            // Initialize
            var mlContext = new MLContext();

            // Prepare the data: both descriptions are added to a list.
            var data = new List<TextData>
            {
                new TextData { Text = req1.Description },
                new TextData { Text = req2.Description }
            };

            // Load the data into an IDataView.
            IDataView dataView = mlContext.Data.LoadFromEnumerable(data);

            // Define the text featurization pipeline.
            var pipeline = mlContext.Transforms.Text.FeaturizeText(
                outputColumnName: "Features", inputColumnName: nameof(TextData.Text)
            );

            // Fit the model on the data and transform it.
            var transformer = pipeline.Fit(dataView);
            var transformedData = transformer.Transform(dataView);

            // Extract the feature vectors.
            var features = mlContext.Data.CreateEnumerable<TransformedText>(transformedData, reuseRowObject: false)
                                         .ToList();

            if (features.Count < 2)
            {
                return BadRequest("Insufficient data for comparison.");
            }

            // Convert the float feature vectors to double arrays.
            var vectorA = features[0].Features.Select(f => (double)f).ToArray();
            var vectorB = features[1].Features.Select(f => (double)f).ToArray();

            // Compute the cosine similarity.
            double similarity = CosineSimilarity(vectorA, vectorB);

            return Ok(new { similarity });
        }
        
        private float[] GetTextFeatures(MLContext mlContext, ITransformer model, string text)
        {
            var inputData = new List<TextData> { new TextData { Text = text } };
            var dataView = mlContext.Data.LoadFromEnumerable(inputData);
            var transformedData = model.Transform(dataView);
            //  extract the "Features" column.
            var features = mlContext.Data.CreateEnumerable<TransformedText>(transformedData, reuseRowObject: false)
                .First().Features;
            return features;
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