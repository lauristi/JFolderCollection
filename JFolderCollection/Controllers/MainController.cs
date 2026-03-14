using Jellyfin.Data.Enums;
using JFolderCollection.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using JFolderCollection.Models;

namespace JFolderCollection.Controllers
{
    /// <summary>
    /// Controller unificado responsável por todas as operações do plugin JFolderCollection.
    /// Gerencia listagem de pastas, detecção de duplicados e criação de coleções.
    /// </summary>
    [ApiController]
    [Route("Plugin/Folder")]
    [Produces(MediaTypeNames.Application.Json)]
    // NOTA: No Jellyfin, o [Authorize] simples usa a política padrão do servidor.
    // Para endpoints administrativos de plugin, o correto é exigir autenticação.
    // Se quiser que seja acessível sem login (não recomendado), troque por [AllowAnonymous].
    [Authorize]
    public class MainController : ControllerBase
    {
        #region Dependências

        private readonly ILogger<MainController> _logger;
        private readonly ICollectionManager _collectionManager;
        private readonly ILibraryManager _libraryManager;

        // CORREÇÃO CRÍTICA: A configuração de plugin é acessada via Plugin.Instance.Configuration,
        // não via IConfigurationManager. O IConfigurationManager é da infraestrutura do servidor
        // e não conhece as configurações do seu plugin específico.
        private PluginConfiguration Config => Plugin.Instance?.Configuration ?? new PluginConfiguration();

        #endregion Dependências

        #region Construtor

        public MainController(
            ILogger<MainController> logger,
            ICollectionManager collectionManager,
            ILibraryManager libraryManager)
        {
            _logger = logger;
            _collectionManager = collectionManager;
            _libraryManager = libraryManager;
            // CORREÇÃO: IHttpClientFactory foi removido do construtor.
            // O TriggerLibraryScan via HTTP para si mesmo era um anti-pattern.
            // Substituído por ILibraryManager diretamente (veja TriggerLibraryScan abaixo).
        }

        #endregion Construtor

        #region Endpoints — Ferramentas

        /// <summary>
        /// Retorna a lista de subpastas de um diretório.
        /// Usado pelo botão "Listar Pastas" na interface administrativa.
        /// </summary>
        [HttpGet("Subfolders")]
        public IActionResult GetSubfolders([FromQuery] string? path = null)
        {
            try
            {
                // Prioriza o path da query string; cai para a configuração salva se vazio.
                var targetPath = ResolveTargetPath(path, Config.BaseFolderPath);

                if (!IsValidDirectory(targetPath, out var errorResult))
                    return errorResult!;

                var subfolders = Directory.GetDirectories(targetPath)
                    .Select(Path.GetFileName)
                    .OrderBy(name => name)
                    .ToList();

                _logger.LogInformation("Listando {Count} subpastas em: {Path}", subfolders.Count, targetPath);
                return Ok(subfolders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar subpastas em: {Path}", path);
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Detecta filmes duplicados (mesmo nome de arquivo, extensões de vídeo) em subpastas.
        /// Usado pelo botão "Buscar Duplicados" na interface administrativa.
        /// </summary>
        [HttpGet("DuplicateMovies")]
        public IActionResult GetDuplicateMovies([FromQuery] string? path = null)
        {
            try
            {
                var targetPath = ResolveTargetPath(path, Config.BaseFolderPath);

                if (!IsValidDirectory(targetPath, out var errorResult))
                    return errorResult!;

                var allMovies = Directory
                    .GetFiles(targetPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => IsVideoFile(f))
                    .Select(f => new
                    {
                        FileName = Path.GetFileNameWithoutExtension(f),
                        FullPath = f
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
                    .OrderBy(d => d.MovieName)
                    .ToList();

                _logger.LogInformation(
                    "Scan de duplicados: {Total} filmes escaneados, {Dups} grupos duplicados.",
                    allMovies.Count, duplicates.Count);

                return Ok(new
                {
                    TotalMoviesScanned = allMovies.Count,
                    Duplicates = duplicates
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar duplicados em: {Path}", path);
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        #endregion Endpoints — Ferramentas

        #region Endpoints — Criação de Coleções

        /// <summary>
        /// Endpoint principal: cria coleções no Jellyfin baseado na estrutura de pastas.
        /// Acionado pelo botão "EXECUTAR CRIAÇÃO DE COLEÇÕES".
        /// </summary>
        [HttpPost("CreateCollections")]
        public async Task<IActionResult> CreateCollections([FromBody] CreateCollectionRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Iniciando criação de coleções. OnlyNew={OnlyNew}, DeleteAll={DeleteAll}",
                    request.OnlyNew, request.DeleteAll);

                if (request.DeleteAll)
                {
                    _logger.LogWarning("Apagando todas as coleções existentes conforme solicitado.");
                    DeleteAllCollections();
                }

                // Prioriza os valores do request; usa a configuração salva como fallback.
                var basePath = ResolveTargetPath(request.BaseFolderPath, Config.BaseFolderPath);
                var postersPath = ResolveTargetPath(request.PosterFolderPath, Config.PosterFolderPath);

                if (!IsValidDirectory(basePath, out var errorResult))
                    return errorResult!;

                var collectionFolders = Directory.GetDirectories(basePath)
                    .Select(p => new { FullPath = p, FolderName = Path.GetFileName(p)! })
                    .ToList();

                int created = 0, skipped = 0;

                foreach (var folder in collectionFolders)
                {
                    // Calcula o nome final da coleção aplicando substituição de prefixo se configurado.
                    var collectionName = ApplyPrefixRule(folder.FolderName, request.CurrentPrefix, request.NewPrefix);

                    if (request.OnlyNew && CollectionExists(collectionName))
                    {
                        _logger.LogInformation("Pulando '{Name}': coleção já existe.", collectionName);
                        skipped++;
                        continue;
                    }

                    await ProcessCollectionFolderAsync(folder.FullPath, collectionName, postersPath, folder.FolderName);
                    created++;
                }

                // CORREÇÃO: Substitui o anti-pattern de HTTP para si mesmo.
                // O próprio LibraryManager já expõe o método correto para forçar o refresh.
                await TriggerLibraryScanAsync();

                _logger.LogInformation(
                    "Processamento concluído. Criadas: {Created}, Puladas: {Skipped}.",
                    created, skipped);

                return Ok(new
                {
                    Message = $"Processamento finalizado. {created} coleções criadas, {skipped} puladas.",
                    Created = created,
                    Skipped = skipped,
                    Total = collectionFolders.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico durante criação de coleções.");
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        #endregion Endpoints — Criação de Coleções

        #region Lógica de Processamento de Pastas

        /// <summary>
        /// Orquestra o processamento de uma pasta: localiza filmes, cria a coleção e vincula o poster.
        /// </summary>
        /// <param name="folderPath">Caminho físico da pasta da coleção.</param>
        /// <param name="collectionName">Nome final da coleção (já com prefixo aplicado).</param>
        /// <param name="postersPath">Caminho da pasta de posters.</param>
        /// <param name="originalFolderName">Nome original da pasta, usado para localizar o poster.</param>
        private async Task ProcessCollectionFolderAsync(
            string folderPath,
            string collectionName,
            string postersPath,
            string originalFolderName)
        {
            // Mapeia os subdiretórios: cada subpasta é esperada ser um filme.
            var movieIds = Directory.GetDirectories(folderPath)
                .Select(movieFolder => FindMovieInFolder(movieFolder))
                .Where(movie => movie is not null)
                .Select(movie => movie!.Id.ToString())
                .ToList();

            if (movieIds.Count == 0)
            {
                _logger.LogWarning(
                    "Pasta '{Name}' pulada: nenhum filme encontrado na biblioteca do Jellyfin.",
                    collectionName);
                return;
            }

            _logger.LogInformation(
                "Criando coleção '{Name}' com {Count} filmes.", collectionName, movieIds.Count);

            var collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
            {
                Name = collectionName,
                // NOTA: ItemIdList espera um IReadOnlyList<string> no 10.11.
                // Passamos o array diretamente para garantir compatibilidade.
                ItemIdList = movieIds.ToArray()
            });

            // O poster é buscado pelo nome original da pasta, não pelo nome da coleção.
            await AddImageToCollectionAsync(collection, originalFolderName, postersPath);
        }

        #endregion Lógica de Processamento de Pastas

        #region Manipulação de Imagens

        /// <summary>
        /// Copia o arquivo de poster para o diretório físico da coleção recém-criada.
        /// O Jellyfin detecta o arquivo "poster.png" automaticamente durante o próximo scan.
        /// </summary>
        private async Task AddImageToCollectionAsync(BoxSet collection, string folderName, string postersPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(postersPath)) return;

                var imagePath = Path.Combine(postersPath, $"{folderName}.png");
                if (!System.IO.File.Exists(imagePath))
                {
                    _logger.LogDebug("Poster não encontrado para '{Name}': {Path}", folderName, imagePath);
                    return;
                }

                var collectionFolder = collection.Path;
                if (!Directory.Exists(collectionFolder))
                    Directory.CreateDirectory(collectionFolder);

                var destImagePath = Path.Combine(collectionFolder, "poster.png");

                if (!System.IO.File.Exists(destImagePath))
                {
                    // Usamos await com Task.Run para não bloquear a thread do request em I/O síncrono.
                    await Task.Run(() => System.IO.File.Copy(imagePath, destImagePath));
                    _logger.LogInformation("Poster aplicado à coleção '{Name}'.", collection.Name);
                }
            }
            catch (Exception ex)
            {
                // Erro de imagem não deve abortar o processamento da coleção.
                _logger.LogError(ex, "Erro ao copiar poster para '{Name}'.", collection.Name);
            }
        }

        #endregion Manipulação de Imagens

        #region Gerenciamento de Biblioteca

        /// <summary>
        /// Remove todas as coleções (BoxSet) da biblioteca do Jellyfin.
        /// ATENÇÃO: operação destrutiva e irreversível.
        /// </summary>
        private void DeleteAllCollections()
        {
            // CORREÇÃO 10.11: GetItemList agora retorna IReadOnlyList<BaseItem>.
            // A chamada em si é a mesma, mas o tipo de retorno mudou — não tente
            // atribuir a List<BaseItem> pois isso causa MissingMethodException em runtime.
            var collections = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                Recursive = true
            }).OfType<BoxSet>().ToList(); // .ToList() materializa antes do loop de delete

            _logger.LogWarning("Apagando {Count} coleções.", collections.Count);

            foreach (var collection in collections)
            {
                // DeleteFileLocation = false: remove da biblioteca mas preserva os arquivos físicos.
                _libraryManager.DeleteItem(collection, new DeleteOptions
                {
                    DeleteFileLocation = false
                });
            }
        }

        /// <summary>
        /// Verifica se uma coleção com o nome especificado já existe na biblioteca.
        /// </summary>
        private bool CollectionExists(string name)
        {
            // CORREÇÃO 10.11: mesma observação do DeleteAllCollections.
            // GetItemList retorna IReadOnlyList<BaseItem> — usamos .Any() do LINQ que funciona
            // em qualquer IEnumerable, independente do tipo concreto retornado.
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                Name = name,
                Recursive = true
            }).Any();
        }

        /// <summary>
        /// Solicita ao servidor que reescaneie a biblioteca para refletir as novas coleções.
        /// CORREÇÃO: substituímos a chamada HTTP externa (anti-pattern) pelo método interno correto.
        /// O ILibraryManager.QueueLibraryScan agenda o scan na fila de tarefas do servidor.
        /// </summary>
        private Task TriggerLibraryScanAsync()
        {
            try
            {
                // QueueLibraryScan é thread-safe e não bloqueante: apenas enfileira a tarefa.
                _libraryManager.QueueLibraryScan();
                _logger.LogInformation("Library scan agendado com sucesso.");
            }
            catch (Exception ex)
            {
                // Falha no scan não é crítica; as coleções já foram criadas.
                _logger.LogError(ex, "Erro ao agendar library scan.");
            }

            return Task.CompletedTask;
        }

        #endregion Gerenciamento de Biblioteca

        #region Helpers Privados

        /// <summary>
        /// Localiza o objeto Movie no banco do Jellyfin a partir do caminho físico de uma pasta.
        /// ILibraryManager.FindByPath é a ponte entre arquivo no disco e entidade no banco.
        /// </summary>
        private Movie? FindMovieInFolder(string movieFolderPath)
        {
            var videoFile = Directory
                .GetFiles(movieFolderPath, "*.*", SearchOption.AllDirectories)
                .FirstOrDefault(IsVideoFile);

            if (videoFile is null) return null;

            return _libraryManager.FindByPath(videoFile, false) as Movie;
        }

        /// <summary>
        /// Valida se a extensão do arquivo é um formato de vídeo suportado.
        /// </summary>
        private static bool IsVideoFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext is ".mkv" or ".mp4" or ".avi" or ".mov" or ".wmv" or ".m4v" or ".flv" or ".webm";
        }

        /// <summary>
        /// Retorna <paramref name="queryPath"/> se não for vazio; caso contrário retorna <paramref name="configPath"/>.
        /// Garante nunca retornar null.
        /// </summary>
        private static string ResolveTargetPath(string? queryPath, string? configPath)
            => !string.IsNullOrWhiteSpace(queryPath) ? queryPath : (configPath ?? string.Empty);

        /// <summary>
        /// Aplica a regra de substituição de prefixo ao nome da pasta.
        /// Só substitui se ambos os prefixos forem fornecidos e não-vazios.
        /// </summary>
        private static string ApplyPrefixRule(string folderName, string? currentPrefix, string? newPrefix)
        {
            if (!string.IsNullOrWhiteSpace(currentPrefix) && !string.IsNullOrWhiteSpace(newPrefix))
                return folderName.Replace(currentPrefix, newPrefix, StringComparison.Ordinal);

            return folderName;
        }

        /// <summary>
        /// Valida se um diretório existe e não é vazio.
        /// Retorna false e preenche <paramref name="errorResult"/> com o IActionResult adequado se inválido.
        /// </summary>
        private bool IsValidDirectory(string path, out IActionResult? errorResult)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                _logger.LogWarning("Diretório inválido ou não encontrado: '{Path}'", path);
                errorResult = NotFound(new { Message = $"Diretório não encontrado: '{path}'" });
                return false;
            }

            errorResult = null;
            return true;
        }

        #endregion Helpers Privados
    }
}