using JFolderCollection.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

namespace JFolderCollection.Controllers // Ajustado para bater com o projeto
{
    [ApiController]
    [Route("Plugin/Folder")]
    [Produces(MediaTypeNames.Application.Json)]
    // IHasService é uma interface de marcação que ajuda no registro em algumas versões
    public class FolderController : ControllerBase
    {
        private readonly PluginConfiguration _config;
        private readonly ILogger<FolderController> _logger;

        public FolderController(
            IApplicationPaths appPaths,
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
            // O código interno está perfeito, não mude a lógica do FromQuery.
            try
            {
                string targetPath = !string.IsNullOrWhiteSpace(path) ? path : (_config.BaseFolderPath ?? "/mnt/xs1000/Filmes Colecoes");

                if (!Directory.Exists(targetPath))
                    return NotFound(new { Message = "Diretório não encontrado" });

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
            // Lógica interna mantida...
            try
            {
                string targetPath = !string.IsNullOrWhiteSpace(path) ? path : (_config.BaseFolderPath ?? "/mnt/xs1000/Filmes Colecoes");
                if (!Directory.Exists(targetPath)) return NotFound(new { Message = "Diretório não encontrado" });

                var videoExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };
                var allMovies = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => videoExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .Select(file => new { FileName = Path.GetFileNameWithoutExtension(file), FullPath = file })
                    .ToList();

                var duplicates = allMovies
                    .GroupBy(m => m.FileName, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => new { MovieName = g.Key, Count = g.Count(), Locations = g.Select(m => m.FullPath).ToList() })
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