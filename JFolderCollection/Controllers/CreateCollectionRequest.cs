public class CreateCollectionRequest
{
    public string CurrentPrefix { get; set; }
    public string NewPrefix { get; set; }
    public string BaseFolderPath { get; set; }
    public string PosterFolderPath { get; set; }
    public bool DeleteAll { get; set; }
    public bool OnlyNew { get; set; }
}