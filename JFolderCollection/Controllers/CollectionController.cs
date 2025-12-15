namespace Jellyfin.Plugin.Template.Controllers
{
    using Jellyfin.Data.Enums;
    using MediaBrowser.Controller.Collections;
    using MediaBrowser.Controller.Entities.Movies;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Library;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System;

    [ApiController]
    [Route("Plugin/Folder")]
    public class CollectionController : ControllerBase
    {
        private readonly ILogger<CollectionController> _logger;
        private readonly ICollectionManager _collectionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IHttpClientFactory _httpClientFactory;

        // Configurações do Jellyfin
        private const string JELLYFIN_URL = "http://192.168.0.157:8096";

        private const string API_KEY = "21e58be425b747388de6fcc5f825309d";

        public CollectionController(
            ILogger<CollectionController> logger,
            ICollectionManager collectionManager,
            ILibraryManager libraryManager,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _collectionManager = collectionManager;
            _libraryManager = libraryManager;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("CreateCollections")]
        public async Task<IActionResult> CreateCollections(
                                                            [FromQuery] string prefixoAtual,
                                                            [FromQuery] string prefixoNovo,
                                                            [FromQuery] string baseFolderPath,
                                                            [FromQuery] string posterFolderPath,
                                                            [FromQuery]   bool apagarTudo = false)
        {
            try
            {
                _logger.LogInformation("🎬 Criando coleções - ApagarTudo: {ApagarTudo}", apagarTudo);

                // 1. Apagar todas as coleções se solicitado
                if (apagarTudo)
                {
                    DeleteAllCollections();
                }

                // 2. Buscar pastas de coleções
                var collectionsPath = "/mnt/xs1000/Filmes/Filmes Colecoes";
                var postersPath = "/mnt/xs1000/Posters/Colecoes";

                if (!Directory.Exists(collectionsPath))
                {
                    _logger.LogWarning("❌ Pasta de coleções não encontrada: {Path}", collectionsPath);
                    return NotFound(new { Message = "Pasta de coleções não encontrada" });
                }

                if (!Directory.Exists(postersPath))
                {
                    _logger.LogWarning("❌ Pasta de posters não encontrada: {Path}", postersPath);
                    return NotFound(new { Message = "Pasta de posters não encontrada" });
                }

                var collectionFolders = Directory.GetDirectories(collectionsPath)
                    .Select(p => new { Path = p, Name = Path.GetFileName(p) })
                    .ToList();

                _logger.LogInformation("📁 Pastas encontradas: {Count}", collectionFolders.Count);

                // 3. Processar cada pasta
                foreach (var folder in collectionFolders)
                {
                    await ProcessCollectionFolder(folder.Path, folder.Name, prefixoAtual, prefixoNovo, postersPath);
                }

                // 4. Disparar scan da biblioteca para detectar as novas imagens
                await TriggerLibraryScan();

                return Ok(new { Message = $"Processadas {collectionFolders.Count} coleções. Scan da biblioteca iniciado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao criar coleções");
                return StatusCode(500, new { Message = "Erro interno" });
            }
        }

        private async Task ProcessCollectionFolder(string folderPath, string folderName, string prefixoAtual, string prefixoNovo, string postersPath)
        {
            try
            {
                // 01 Aplicar transformação do nome
                var collectionName = folderName;
                if (!string.IsNullOrWhiteSpace(prefixoAtual) && !string.IsNullOrWhiteSpace(prefixoNovo))
                {
                    collectionName = folderName.Replace(prefixoAtual, prefixoNovo);
                }

                _logger.LogInformation("📂 Processando: {Folder} -> {CollectionName}", folderName, collectionName);

                // 02 Buscar filmes (cada filme em sua própria pasta)
                var movieFolders = Directory.GetDirectories(folderPath);
                var movieIds = new List<string>();

                foreach (var movieFolder in movieFolders)
                {
                    var movie = FindMovieInFolder(movieFolder);
                    if (movie != null)
                    {
                        movieIds.Add(movie.Id.ToString());
                    }
                }

                if (!movieIds.Any())
                {
                    _logger.LogWarning("⚠️ Nenhum filme em: {Folder}", folderName);
                    return;
                }

                // 03 Criar a coleção
                var collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                {
                    Name = collectionName,
                    ItemIdList = movieIds.ToArray()
                });

                _logger.LogInformation("✅ Coleção '{Name}' criada com {Count} filmes", collectionName, movieIds.Count);

                // 04 Adicionar imagem à coleção (apenas se não existir)
                await AddImageToCollection(collection, folderName, postersPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar pasta: {Folder}", folderName);
            }
        }

        #region Support Methods

        private async Task AddImageToCollection(BoxSet collection, string folderName, string postersPath)
        {
            try
            {
                // Construir o caminho da imagem baseado no nome da pasta
                var imagePath = Path.Combine(postersPath, $"{folderName}.png");

                if (!System.IO.File.Exists(imagePath))
                {
                    _logger.LogWarning("⚠️ Imagem não encontrada para coleção {Collection}: {ImagePath}", collection.Name, imagePath);
                    return;
                }

                _logger.LogInformation("🖼️ Verificando imagem para coleção: {Collection}", collection.Name);

                // Abordagem compatível com Jellyfin 10.11.0:
                // Copiar a imagem apenas se não existir na pasta da coleção
                var collectionFolder = collection.Path;
                if (!Directory.Exists(collectionFolder))
                {
                    Directory.CreateDirectory(collectionFolder);
                }

                var collectionImagePath = Path.Combine(collectionFolder, "poster.png");

                // Verificar se a imagem já existe na pasta da coleção
                if (System.IO.File.Exists(collectionImagePath))
                {
                    _logger.LogInformation("⏭️ Imagem já existe na coleção {Collection}, pulando...", collection.Name);
                    return;
                }

                // Copiar a imagem para o diretório da coleção (apenas se não existir)
                System.IO.File.Copy(imagePath, collectionImagePath);

                _logger.LogInformation("✅ Imagem copiada para: {CollectionImagePath}", collectionImagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao adicionar imagem à coleção: {Collection}", collection.Name);
            }
        }

        private async Task TriggerLibraryScan()
        {
            try
            {
                _logger.LogInformation("🔄 Disparando scan da biblioteca...");

                using var httpClient = _httpClientFactory.CreateClient();

                // Configurar headers
                httpClient.DefaultRequestHeaders.Add("X-MediaBrowser-Token", API_KEY);

                // Endpoint para scan da biblioteca
                var scanUrl = $"{JELLYFIN_URL}/Library/Refresh";

                var response = await httpClient.PostAsync(scanUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Scan da biblioteca iniciado com sucesso");
                }
                else
                {
                    _logger.LogWarning("⚠️ Falha ao iniciar scan da biblioteca: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao disparar scan da biblioteca");
            }
        }

        private void DeleteAllCollections()
        {
            var collections = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet }
            }).OfType<BoxSet>();

            foreach (var collection in collections)
            {
                _libraryManager.DeleteItem(collection, new DeleteOptions { DeleteFileLocation = false });
                _logger.LogDebug("🗑️ Coleção apagada: {Name}", collection.Name);
            }

            _logger.LogInformation("✅ Apagadas {Count} coleções", collections.Count());
        }

        private Movie FindMovieInFolder(string movieFolderPath)
        {
            try
            {
                var videoFiles = Directory.GetFiles(movieFolderPath, "*", SearchOption.AllDirectories)
                    .Where(IsVideoFile)
                    .FirstOrDefault();

                if (videoFiles != null)
                {
                    var movie = _libraryManager.FindByPath(videoFiles, false) as Movie;
                    if (movie != null)
                    {
                        _logger.LogDebug("🎥 Filme encontrado: {Movie}", movie.Name);
                        return movie;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar filme em: {Path}", movieFolderPath);
                return null;
            }
        }

        private bool IsVideoFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return new[] { ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".m4v", ".flv", ".webm" }.Contains(ext);
        }

        #endregion Support Methods
    }
}