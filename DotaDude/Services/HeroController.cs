using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DotaDude.Services;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DotaDude.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeroController : ControllerBase
    {
        private readonly OpenDotaService _openDotaService;
        private readonly GeminiService _geminiService;
        private readonly ILogger<HeroController> _logger;

        public HeroController(OpenDotaService openDotaService, GeminiService geminiService, ILogger<HeroController> logger)
        {
            _openDotaService = openDotaService;
            _geminiService = geminiService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetHeroDetails(int id)
        {
            try
            {
                var hero = await _openDotaService.GetHeroDetailsAsync(id);
                if (hero == null)
                    return NotFound(new { message = $"Hero with ID {id} not found" });
                return Ok(hero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting hero with ID {id}");
                return StatusCode(500, new { message = "An error occurred while retrieving hero details" });
            }
        }

        [HttpGet("models")]
        public async Task<IActionResult> GetAvailableModels()
        {
            try
            {
                var models = await _geminiService.ListAvailableModelsAsync();
                return Ok(new { models });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available models");
                return StatusCode(500, new { message = "An error occurred while retrieving available models" });
            }
        }

        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetHeroComments(int id)
        {
            try
            {
                var hero = await _openDotaService.GetHeroDetailsAsync(id);
                if (hero == null)
                    return NotFound(new { message = $"Hero with ID {id} not found" });

                _logger.LogInformation($"Fetching comments for hero: {hero.LocalizedName}");
                var comments = await _geminiService.GetGenericCommentAsync(hero.LocalizedName);

                return Ok(new { heroName = hero.LocalizedName, comments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting comments for hero ID {id}");
                return StatusCode(500, new { message = "An error occurred while retrieving hero comments" });
            }
        }
    }
}