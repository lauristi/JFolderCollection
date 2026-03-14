/// <summary>
/// DTO para o corpo da requisição de criação de coleções.
/// O [FromBody] do ASP.NET Core desserializa automaticamente o JSON do front para esta classe.
/// </summary>

namespace JFolderCollection.Models
{
    public class CreateCollectionRequest
    {
        public string? CurrentPrefix { get; set; }
        public string? NewPrefix { get; set; }
        public string? BaseFolderPath { get; set; }
        public string? PosterFolderPath { get; set; }
        public bool DeleteAll { get; set; }
        public bool OnlyNew { get; set; } = true;
    }
}