using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VideoGameAPI.AppCore;
using VideoGameAPI.Data;

namespace VideoGameAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoGameController: ControllerBase
    {
        private readonly VideoGameDbContext _context;
      //  private readonly IDatabase _cache = redis.GetDatabase(); // Get Redis database
        private readonly ICacheService _cacheService;
        private const string cacheKey = "videogames";
        private readonly ILogger<VideoGameController> _logger;
        public VideoGameController(ICacheService cacheService, VideoGameDbContext context, ILogger<VideoGameController> logger)
        {
            _cacheService = cacheService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<VideoGame>>> GetVideoGames()
        {
            _logger.LogInformation("Fetching video games...");

            var cachedGames = await _cacheService.GetAsync<List<VideoGame>>(cacheKey);
            if (cachedGames != null)
            {
                _logger.LogInformation("Returning cached video games.");
                return Ok(cachedGames);
            }

            var games = await _context.VideoGames.ToListAsync();
            _logger.LogInformation("Fetched {Count} games from database.", games.Count);

            await _cacheService.SetAsync(cacheKey, games, TimeSpan.FromMinutes(5));
            return Ok(games);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VideoGame>> GetVideoGameById(int id)
        {
            _logger.LogInformation("Fetching video game with ID: {Id}", id);

            var cacheKey = $"videogame:{id}";
            var cachedGame = await _cacheService.GetAsync<VideoGame>(cacheKey);

            if (cachedGame != null)
            {
                _logger.LogInformation("Returning cached video game for ID: {Id}", id);
                return Ok(cachedGame);
            }

            var game = await _context.VideoGames.FindAsync(id);
            if (game is null)
            {
                _logger.LogWarning("Video game with ID: {Id} not found.", id);
                return NotFound();
            }

            await _cacheService.SetAsync(cacheKey, game, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Returning video game: {Game}", game.Title);
            return Ok(game);
        }


        [HttpPost]
        public async Task<ActionResult<VideoGame>> AddVideoGame(VideoGame newGame)
        {
            if (newGame is null)
            {
                _logger.LogWarning("Attempted to add a null video game.");
                return BadRequest();
            }

            _logger.LogInformation("Adding new video game: {Title}", newGame.Title);
            _context.VideoGames.Add(newGame);
            await _context.SaveChangesAsync();

            await _cacheService.RemoveAsync("videogames"); // Clear cache

            _logger.LogInformation("Successfully added game {Title} with ID: {Id}", newGame.Title, newGame.Id);
            return CreatedAtAction(nameof(GetVideoGameById), new { id = newGame.Id }, newGame);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVideoGame(int id, VideoGame updatesGame)
        {
            _logger.LogInformation("Updating video game with ID: {Id}", id);

            var game = await _context.VideoGames.FindAsync(id);
            if (game is null)
            {
                _logger.LogWarning("Video game with ID: {Id} not found.", id);
                return NotFound();
            }

            game.Title = string.IsNullOrWhiteSpace(updatesGame.Title) ? game.Title : updatesGame.Title;
            game.Platform = string.IsNullOrWhiteSpace(updatesGame.Platform) ? game.Platform : updatesGame.Platform;
            game.Developer = string.IsNullOrWhiteSpace(updatesGame.Developer) ? game.Developer : updatesGame.Developer;
            game.Publisher = string.IsNullOrWhiteSpace(updatesGame.Publisher) ? game.Publisher : updatesGame.Publisher;

            await _context.SaveChangesAsync();

            await _cacheService.RemoveAsync($"videogame:{id}");
            await _cacheService.RemoveAsync("videogames");

            _logger.LogInformation("Successfully updated video game with ID: {Id}", id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVideoGame(int id)
        {
            _logger.LogInformation("Deleting video game with ID: {Id}", id);

            var game = await _context.VideoGames.FindAsync(id);
            if (game is null)
            {
                _logger.LogWarning("Video game with ID: {Id} not found.", id);
                return NotFound();
            }

            _context.VideoGames.Remove(game);
            await _context.SaveChangesAsync();

            await _cacheService.RemoveAsync($"videogame:{id}");
            await _cacheService.RemoveAsync("videogames");

            _logger.LogInformation("Successfully deleted video game with ID: {Id}", id);
            return NoContent();
        }

    }
}
