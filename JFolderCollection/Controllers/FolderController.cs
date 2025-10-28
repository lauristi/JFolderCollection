namespace Jellyfin.Plugin.Template.Controllers
{
    using JFolderCollection.Configuration;
    using MediaBrowser.Common.Configuration;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
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
                // Pass the required key argument for GetConfiguration<T>
                //_config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
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
        /// Teste de log.
        /// </summary>
        /// <returns>ok</returns>
        [HttpPost("LogTest")]
        public IActionResult PostLogTest()
        {
            _logger.LogInformation("🔄 LogTest chamado via POST");

            try
            {
                _logger.LogInformation("📝 Escrevendo no arquivo de log...");
                System.IO.File.AppendAllText(_logFile, $"{DateTime.Now}: Apenas um teste de LOG\n");

                _logger.LogInformation("✅ Log escrito com sucesso");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 ERRO em LogTest");
                return StatusCode(500, new { Message = "Erro interno ao gerar log." });
            }
        }
    }
}