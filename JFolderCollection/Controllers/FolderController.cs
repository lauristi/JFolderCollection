namespace Jellyfin.Plugin.Template.Controllers
{
    using JFolderCollection.Configuration;
    using MediaBrowser.Common.Configuration;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Controller responsável por listar subpastas do plugin.
    /// </summary>
    [ApiController]
    [Route("Plugin/Folder")]
    public class FolderController : ControllerBase
    {
        private readonly PluginConfiguration _config;
        private readonly ILogger<FolderController> _logger;
        private readonly string _logFile;
        private readonly string pluginName = "JFolderCollection";
        private readonly string logFileName = "log.txt";

        /// <summary>
        /// Construtor do controller.
        /// </summary>
        public FolderController(IApplicationPaths appPaths, IConfigurationManager configurationManager, ILogger<FolderController> logger)
        {
            _logFile = Path.Combine(appPaths.PluginsPath, pluginName, logFileName);
            _logger = logger;

            // 🚨 DEBUG CRÍTICO
            _logger.LogInformation("🚨 FolderController CONSTRUIDO!");

            try
            {
                // Carrega a configuração salva do plugin, ou cria uma nova padrão se não existir
                _config = configurationManager.GetConfiguration<PluginConfiguration>(
                                                                                      nameof(PluginConfiguration)
                                                                                     ) ?? new PluginConfiguration();

                _logger.LogInformation("✅ Configuração carregada: {BasePath}", _config.BaseFolderPath);

                // Teste de escrita no log
                System.IO.File.AppendAllText(_logFile, $"{DateTime.Now}: Controller inicializado\n");
                _logger.LogInformation("✅ Teste de escrita no arquivo realizado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro no construtor do FolderController");
                _config = new PluginConfiguration();
            }
        }

        /// <summary>
        /// Retorna a lista de subpastas de um caminho.
        /// </summary>
        /// <param name="path">Caminho opcional, usa BaseFolderPath se nulo.</param>
        [HttpGet("Subfolders")]
        public IActionResult GetSubfolders([FromQuery] string? path = null)
        {
            _logger.LogInformation("🔍 GetSubfolders chamado - path: '{Path}'", path ?? "null");

            try
            {
                string basePath = _config.BaseFolderPath ?? "/mnt/xs1000/Filmes Colecoes";
                string targetPath = string.IsNullOrWhiteSpace(path) ? basePath : path;

                _logger.LogInformation("📂 BasePath config: '{BasePath}'", basePath);
                _logger.LogInformation("📂 TargetPath final: '{TargetPath}'", targetPath);

                // Sanitiza caminho
                targetPath = Path.GetFullPath(targetPath);
                _logger.LogInformation("📂 Path sanitizado: '{TargetPath}'", targetPath);

                // Escreve no arquivo de log
                System.IO.File.AppendAllText(_logFile, $"{DateTime.Now}: GetSubfolders - path: '{path}', final: '{targetPath}'\n");

                if (!Directory.Exists(targetPath))
                {
                    _logger.LogWarning("❌ Diretório não encontrado: {TargetPath}", targetPath);
                    System.IO.File.AppendAllText(_logFile, $"{DateTime.Now}: Diretório não encontrado: {targetPath}\n");
                    return NotFound(new { Message = $"Diretório não encontrado: {targetPath}" });
                }

                _logger.LogInformation("✅ Diretório existe: {TargetPath}", targetPath);

                var subfolders = Directory.GetDirectories(targetPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();

                _logger.LogInformation("📁 Subpastas encontradas: {Count} pastas", subfolders.Count);
                _logger.LogInformation("📁 Lista: {Folders}", string.Join(", ", subfolders));

                System.IO.File.AppendAllText(_logFile, $"{DateTime.Now}: Subpastas encontradas: {string.Join(", ", subfolders)}\n");

                return Ok(subfolders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 ERRO em GetSubfolders");
                System.IO.File.AppendAllText(_logFile, $"{DateTime.Now}: ERRO - {ex.Message}\n{ex.StackTrace}\n");
                return StatusCode(500, new { Message = "Erro interno ao listar pastas." });
            }
        }

        /// <summary>
        /// Retorna uma lista de filmes duplicados baseado no nome do arquivo.
        /// </summary>
        /// <param name="path">Caminho opcional, usa BaseFolderPath se nulo.</param>
        [HttpGet("DuplicateMovies")]
        public IActionResult GetDuplicateMovies([FromQuery] string? path = null)
        {
            _logger.LogInformation("🎬 GetDuplicateMovies chamado - path: '{Path}'", path ?? "null");

            try
            {
                string basePath = _config.BaseFolderPath ?? "/mnt/xs1000/Filmes Colecoes";
                string targetPath = string.IsNullOrWhiteSpace(path) ? basePath : path;

                _logger.LogInformation("📂 TargetPath final: '{TargetPath}'", targetPath);

                // Sanitiza caminho
                targetPath = Path.GetFullPath(targetPath);
                _logger.LogInformation("📂 Path sanitizado: '{TargetPath}'", targetPath);

                // Escreve no arquivo de log
                System.IO.File.AppendAllText(_logFile, $"{DateTime.Now}: GetDuplicateMovies - path: '{path}', final: '{targetPath}'\n");

                if (!Directory.Exists(targetPath))
                {
                    _logger.LogWarning("❌ Diretório não encontrado: {TargetPath}", targetPath);
                    System.IO.File.AppendAllText(_logFile, $"{DateTime.Now}: Diretório não encontrado: {targetPath}\n");
                    return NotFound(new { Message = $"Diretório não encontrado: {targetPath}" });
                }

                _logger.LogInformation("✅ Diretório existe: {TargetPath}", targetPath);

                // Busca recursivamente por arquivos de vídeo
                var videoExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };
                var allMovies = new List<MovieFile>();

                // Busca em todas as subpastas
                var allDirectories = Directory.GetDirectories(targetPath, "*", SearchOption.AllDirectories);
                _logger.LogInformation("📁 Total de diretórios encontrados: {Count}", allDirectories.Length);

                foreach (var directory in allDirectories)
                {
                    try
                    {
                        var files = Directory.GetFiles(directory)
                            .Where(file => videoExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                            .Select(file => new MovieFile
                            {
                                FileName = Path.GetFileNameWithoutExtension(file),
                                FullPath = file,
                                Directory = directory
                            });

                        allMovies.AddRange(files);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Erro ao acessar diretório: {Directory}", directory);
                    }
                }

                _logger.LogInformation("🎬 Total de filmes encontrados: {Count}", allMovies.Count);

                // Encontra duplicados usando LINQ
                var duplicateGroups = allMovies
                    .GroupBy(movie => movie.FileName, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .ToList();

                _logger.LogInformation("🔍 Grupos de duplicados encontrados: {Count}", duplicateGroups.Count);

                var result = duplicateGroups.Select(group => new
                {
                    MovieName = group.Key,
                    Count = group.Count(),
                    Locations = group.Select(movie => new
                    {
                        movie.FullPath,
                        movie.Directory
                    }).ToList()
                }).ToList();

                // Log dos resultados
                foreach (var duplicate in result)
                {
                    _logger.LogInformation("📝 Duplicado: {MovieName} - {Count} cópias", duplicate.MovieName, duplicate.Count);
                    foreach (var location in duplicate.Locations)
                    {
                        _logger.LogInformation("   📍 {Path}", location.FullPath);
                    }
                }

                System.IO.File.AppendAllText(_logFile,
                    $"{DateTime.Now}: DuplicateMovies - {result.Count} filmes duplicados encontrados\n");

                return Ok(new
                {
                    TotalMoviesScanned = allMovies.Count,
                    DuplicateMoviesCount = result.Count,
                    Duplicates = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 ERRO em GetDuplicateMovies");
                System.IO.File.AppendAllText(_logFile, $"{DateTime.Now}: ERRO em GetDuplicateMovies - {ex.Message}\n{ex.StackTrace}\n");
                return StatusCode(500, new { Message = "Erro interno ao buscar filmes duplicados." });
            }
        }

        /// <summary>
        /// Classe auxiliar para representar um arquivo de filme.
        /// </summary>
        private class MovieFile
        {
            public string FileName { get; set; } = string.Empty;
            public string FullPath { get; set; } = string.Empty;
            public string Directory { get; set; } = string.Empty;
        }
    }
}