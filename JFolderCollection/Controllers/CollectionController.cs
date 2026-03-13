namespace Jellyfin.Plugin.Template.Controllers
{
    using Jellyfin.Data.Enums;
    using JFolderCollection; // Referência para acessar o Plugin.Instance
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

    /// <summary>
    /// Controlador responsável por gerenciar as operações de coleções baseadas em pastas.
    /// </summary>
    [ApiController]
    [Route("Plugin/Folder")]
    public class CollectionController : ControllerBase
    {
        #region Campos Privados e Dependências

        private readonly ILogger<CollectionController> _logger;
        private readonly ICollectionManager _collectionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IHttpClientFactory _httpClientFactory;

        // Nota de Estudo: URLs e chaves fixas devem ser evitadas.
        // Idealmente, o token vem da requisição e o Host do próprio servidor.
        private const string JELLYFIN_URL = "http://192.168.0.157:8096";

        private const string API_KEY = "21e58be425b747388de6fcc5f825309d";

        #endregion Campos Privados e Dependências

        #region Construtor

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

        #endregion Construtor

        #region Endpoints da API

        /// <summary>
        /// Cria coleções baseadas na estrutura de pastas.
        /// </summary>
        /// <param name="currentPrefix">Prefixo atual a ser removido do nome da pasta.</param>
        /// <param name="newPrefix">Novo prefixo a ser adicionado ao nome da coleção.</param>
        /// <param name="baseFolderPath">Caminho físico das pastas de filmes.</param>
        /// <param name="posterFolderPath">Caminho físico dos posters.</param>
        /// <param name="deleteAll">Se verdadeiro, apaga todas as coleções antes de iniciar.</param>
        /// <param name="onlyNew">Se verdadeiro, ignora pastas que já possuem uma coleção correspondente no Jellyfin.</param>

        [HttpPost("CreateCollections")]
        public async Task<IActionResult> CreateCollections([FromBody] CreateCollectionRequest request)
        {
            try
            {
                // Agora usamos request.OnlyNew, request.DeleteAll, etc.
                _logger.LogInformation("🎬 Starting collection processing. Mode OnlyNew: {OnlyNew}", request.OnlyNew);

                if (request.DeleteAll)
                {
                    DeleteAllCollections();
                }

                var config = Plugin.Instance?.Configuration;
                var finalCollectionsPath = request.BaseFolderPath ?? config?.BaseFolderPath;
                var finalPostersPath = request.PosterFolderPath ?? config?.PosterFolderPath;

                if (string.IsNullOrEmpty(finalCollectionsPath) || !Directory.Exists(finalCollectionsPath))
                {
                    _logger.LogWarning("❌ Base folder not found: {Path}", finalCollectionsPath);
                    return NotFound(new { Message = "Diretório base não encontrado ou não configurado." });
                }

                var collectionFolders = Directory.GetDirectories(finalCollectionsPath)
                    .Select(p => new { Path = p, Name = Path.GetFileName(p) })
                    .ToList();

                foreach (var folder in collectionFolders)
                {
                    var collectionName = folder.Name;
                    if (!string.IsNullOrWhiteSpace(request.CurrentPrefix) && !string.IsNullOrWhiteSpace(request.NewPrefix))
                    {
                        collectionName = folder.Name.Replace(request.CurrentPrefix, request.NewPrefix);
                    }

                    if (request.OnlyNew && CollectionExists(collectionName))
                    {
                        _logger.LogInformation("Skip: Collection '{Name}' already exists.", collectionName);
                        continue;
                    }

                    await ProcessCollectionFolder(folder.Path, folder.Name, request.CurrentPrefix, request.NewPrefix, finalPostersPath);
                }

                await TriggerLibraryScan();

                return Ok(new { Message = $"Processamento finalizado. {collectionFolders.Count} pastas avaliadas." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating collections");
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        #endregion Endpoints da API

        #region Lógica de Processamento de Pastas

        /// <summary>
        /// Orquestra o processamento de uma pasta de coleção específica.
        /// Renomeia a coleção, identifica os filmes contidos nela e solicita a criação ao servidor.
        /// </summary>
        private async Task ProcessCollectionFolder(string folderPath, string folderName, string prefixoAtual, string prefixoNovo, string postersPath)
        {
            // 1. Lógica de tratamento do nome: substitui o prefixo se ambos forem fornecidos
            var collectionName = folderName;
            if (!string.IsNullOrWhiteSpace(prefixoAtual) && !string.IsNullOrWhiteSpace(prefixoNovo))
            {
                collectionName = folderName.Replace(prefixoAtual, prefixoNovo);
            }

            // 2. Mapeia os subdiretórios (espera-se que cada subpasta seja um filme)
            var movieFolders = Directory.GetDirectories(folderPath);
            var movieIds = new List<string>();

            foreach (var movieFolder in movieFolders)
            {
                // Tenta localizar o objeto "Movie" no banco de dados do Jellyfin a partir do arquivo físico
                var movie = FindMovieInFolder(movieFolder);
                if (movie != null) movieIds.Add(movie.Id.ToString());
            }

            // 3. Só prossegue se houver filmes válidos identificados na biblioteca do Jellyfin
            if (!movieIds.Any()) return;

            // 4. Utiliza o ICollectionManager para criar a coleção (BoxSet) de forma assíncrona
            var collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
            {
                Name = collectionName,
                ItemIdList = movieIds.ToArray()
            });

            // 5. Após criar, tenta vincular a arte (poster) à nova coleção
            await AddImageToCollection(collection, folderName, postersPath);
        }

        #endregion Lógica de Processamento de Pastas

        #region Manipulação de Mídia e Imagens

        /// <summary>
        /// Gerencia a cópia física da imagem do poster para o diretório de metadados da coleção.
        /// </summary>
        private async Task AddImageToCollection(BoxSet collection, string folderName, string postersPath)
        {
            try
            {
                // Define o caminho de origem do poster (espera um .png com o nome original da pasta)
                var imagePath = Path.Combine(postersPath, $"{folderName}.png");
                if (!System.IO.File.Exists(imagePath)) return;

                // Garante que o diretório de destino da coleção exista
                var collectionFolder = collection.Path;
                if (!Directory.Exists(collectionFolder)) Directory.CreateDirectory(collectionFolder);

                var collectionImagePath = Path.Combine(collectionFolder, "poster.png");

                // Só copia a imagem se a coleção ainda não possuir um poster físico definido
                if (!System.IO.File.Exists(collectionImagePath))
                {
                    System.IO.File.Copy(imagePath, collectionImagePath);
                    _logger.LogInformation("🖼️ Imagem aplicada à coleção: {Name}", collection.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar imagem para {Name}", collection.Name);
            }
        }

        /// <summary>
        /// Busca um arquivo de vídeo dentro de uma pasta e tenta encontrar o item correspondente na biblioteca Jellyfin.
        /// </summary>
        private Movie? FindMovieInFolder(string movieFolderPath)
        {
            // Busca o primeiro arquivo que corresponda às extensões de vídeo conhecidas
            var videoFile = Directory.GetFiles(movieFolderPath, "*", SearchOption.AllDirectories)
                .FirstOrDefault(f => IsVideoFile(f));

            if (videoFile != null)
            {
                // O ILibraryManager.FindByPath é a ponte entre o arquivo no disco e o objeto no banco do Jellyfin
                return _libraryManager.FindByPath(videoFile, false) as Movie;
            }

            return null;
        }

        /// <summary>
        /// Valida se a extensão do arquivo corresponde a um formato de vídeo suportado.
        /// </summary>
        private bool IsVideoFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return new[] { ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".m4v", ".flv", ".webm" }.Contains(ext);
        }

        #endregion Manipulação de Mídia e Imagens

        #region Gerenciamento de Biblioteca e API Externa

        /// <summary>
        /// Remove todas as coleções (BoxSets) existentes na biblioteca.
        /// </summary>
        private void DeleteAllCollections()
        {
            // Busca todos os itens do tipo BoxSet (Coleções)
            var collections = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet }
            }).OfType<BoxSet>();

            foreach (var collection in collections)
            {
                // Deleta o item da biblioteca, mas mantém os arquivos físicos intactos
                _libraryManager.DeleteItem(collection, new DeleteOptions { DeleteFileLocation = false });
            }
        }

        /// <summary>
        /// Notifica o servidor Jellyfin via API HTTP para realizar um scan na biblioteca.
        /// Isso é necessário para que as novas coleções e imagens apareçam imediatamente na interface.
        /// </summary>
        private async Task TriggerLibraryScan()
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();

                // É necessário o Token de API para autenticar a requisição de Refresh
                httpClient.DefaultRequestHeaders.Add("X-MediaBrowser-Token", API_KEY);

                var scanUrl = $"{JELLYFIN_URL}/Library/Refresh";

                // Envia um POST vazio para o endpoint de varredura
                await httpClient.PostAsync(scanUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao solicitar refresh da biblioteca");
            }
        }

        #region Private Validation Methods

        /// <summary>
        /// Verifica se uma coleção com o nome especificado já existe na biblioteca do servidor.
        /// </summary>
        /// <param name="name">O nome da coleção para pesquisar.</param>
        /// <returns>Verdadeiro se a coleção for encontrada.</returns>
        private bool CollectionExists(string name)
        {
            // Simplificamos a query removendo o DtoOptions que causou o erro.
            // O InternalItemsQuery ainda é performático pois o filtro de nome é feito no nível do banco de dados.
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                Name = name,
                Recursive = true
            };

            // Usamos o Any() do LINQ para retornar true assim que o primeiro item correspondente for encontrado.
            return _libraryManager.GetItemList(query).Any();
        }

        #endregion Private Validation Methods

        #endregion Gerenciamento de Biblioteca e API Externa
    }
}