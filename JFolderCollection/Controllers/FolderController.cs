using JFolderCollection.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

namespace JFolderCollection.Controllers
{
    // O segredo no Jellyfin é que o nome da classe deve terminar em "Controller"
    // e ela PRECISA ser pública.
    [ApiController]
    [Route("Plugin/Folder")]
    [Produces(MediaTypeNames.Application.Json)]
    public class FolderController : ControllerBase
    {
        private readonly PluginConfiguration _config;
        private readonly ILogger<FolderController> _logger;

        public FolderController(
            IConfigurationManager configurationManager,
            ILogger<FolderController> logger)
        {
            _logger = logger;
            _config = configurationManager.GetConfiguration<PluginConfiguration>(nameof(PluginConfiguration))
                      ?? new PluginConfiguration();
        }

        [HttpGet("Subfolders")]
        public IActionResult GetSubfolders([FromQuery] string? path = null)
        {
            try
            {
                // Se o path vier vazio, tenta a config, senão um fallback seguro
                string targetPath = !string.IsNullOrWhiteSpace(path) ? path : (_config.BaseFolderPath ?? string.Empty);

                if (string.IsNullOrEmpty(targetPath) || !Directory.Exists(targetPath))
                {
                    return NotFound(new { Message = "Caminho inválido ou não encontrado: " + targetPath });
                }

                var subfolders = Directory.GetDirectories(targetPath)
                    .Select(Path.GetFileName)
                    .ToList();

                return Ok(subfolders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar pastas");
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpGet("DuplicateMovies")]
        public IActionResult GetDuplicateMovies([FromQuery] string? path = null)
        {
            try
            {
                string targetPath = !string.IsNullOrWhiteSpace(path) ? path : (_config.BaseFolderPath ?? string.Empty);
                if (string.IsNullOrEmpty(targetPath) || !Directory.Exists(targetPath))
                    return NotFound(new { Message = "Diretório não encontrado" });

                var videoExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };

                var allMovies = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => videoExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .Select(file => new
                    {
                        FileName = Path.GetFileNameWithoutExtension(file),
                        FullPath = file
                    })
                    .ToList();

                var duplicates = allMovies
                    .GroupBy(m => m.FileName, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => new
                    {
                        MovieName = g.Key,
                        Count = g.Count(),
                        Locations = g.Select(m => m.FullPath).ToList()
                    })
                    .ToList();

                return Ok(new { TotalMoviesScanned = allMovies.Count, Duplicates = duplicates });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }
}