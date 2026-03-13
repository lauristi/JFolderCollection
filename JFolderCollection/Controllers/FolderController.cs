using JFolderCollection.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

namespace Jellyfin.Plugin.Template.Controllers
{
    [ApiController]
    [Route("Plugin/Folder")]
    [Produces(MediaTypeNames.Application.Json)]
    public class FolderController : ControllerBase
    {
        private readonly PluginConfiguration _config;
        private readonly ILogger<FolderController> _logger;
        private readonly string _logFile;

        public FolderController(IApplicationPaths appPaths, IConfigurationManager configurationManager, ILogger<FolderController> logger)
        {
            _logFile = Path.Combine(appPaths.PluginsPath, "JFolderCollection", "log.txt");
            _logger = logger;

            _config = configurationManager.GetConfiguration<PluginConfiguration>(nameof(PluginConfiguration))
                      ?? new PluginConfiguration();
        }

        /// <summary>
        /// Retorna a lista de subpastas.
        /// </summary>
        [HttpGet("Subfolders")]
        public IActionResult GetSubfolders([FromQuery] string? path = null)
        {
            try
            {
                string targetPath = !string.IsNullOrWhiteSpace(path) ? path : (_config.BaseFolderPath ?? "/mnt/xs1000/Filmes Colecoes");

                if (!Directory.Exists(targetPath))
                {
                    _logger.LogWarning("Diretório não encontrado: {Path}", targetPath);
                    return NotFound(new { Message = "Diretório não encontrado" });
                }

                var subfolders = Directory.GetDirectories(targetPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();

                return Ok(subfolders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar pastas em {Path}", path);
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retorna filmes duplicados.
        /// </summary>
        [HttpGet("DuplicateMovies")]
        public IActionResult GetDuplicateMovies([FromQuery] string? path = null)
        {
            try
            {
                string targetPath = !string.IsNullOrWhiteSpace(path) ? path : (_config.BaseFolderPath ?? "/mnt/xs1000/Filmes Colecoes");

                if (!Directory.Exists(targetPath))
                    return NotFound(new { Message = "Diretório não encontrado" });

                var videoExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };

                var allMovies = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => videoExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .Select(file => new MovieFile
                    {
                        FileName = Path.GetFileNameWithoutExtension(file),
                        FullPath = file,
                        Directory = Path.GetDirectoryName(file) ?? ""
                    }).ToList();

                var duplicates = allMovies
                    .GroupBy(m => m.FileName, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => new
                    {
                        MovieName = g.Key,
                        Count = g.Count(),
                        Locations = g.Select(m => m.FullPath).ToList()
                    }).ToList();

                return Ok(new
                {
                    TotalMoviesScanned = allMovies.Count,
                    Duplicates = duplicates
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar duplicados");
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        private class MovieFile
        {
            public string FileName { get; set; } = string.Empty;
            public string FullPath { get; set; } = string.Empty;
            public string Directory { get; set; } = string.Empty;
        }
    }
}