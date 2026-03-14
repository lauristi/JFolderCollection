using MediaBrowser.Model.Plugins;

namespace JFolderCollection.Configuration
{
    /// <summary>
    /// Estrutura de dados persistida pelo Jellyfin em XML para o plugin JFolderCollection.
    /// O Jellyfin serializa e desserializa esta classe automaticamente via IXmlSerializer.
    /// Apenas propriedades públicas com getter e setter são persistidas.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        #region Caminhos de Diretório

        /// <summary>
        /// Caminho físico do diretório raiz onde as pastas de coleções estão organizadas.
        /// Cada subpasta representa uma coleção; cada subpasta dentro dela representa um filme.
        /// </summary>
        public string BaseFolderPath { get; set; } = "/mnt/xs1000/Filmes/Filmes Colecoes";

        /// <summary>
        /// Caminho físico do diretório de posters.
        /// Cada arquivo .png deve ter o mesmo nome que a pasta da coleção correspondente.
        /// Exemplo: "Coleção Ação.png" para a pasta "Coleção Ação".
        /// </summary>
        public string PosterFolderPath { get; set; } = "/mnt/xs1000/Filmes/Poster";

        #endregion Caminhos de Diretório

        #region Regras de Processamento

        /// <summary>
        /// Texto a ser encontrado e substituído no início do nome da pasta.
        /// Deixe vazio para desabilitar a substituição de prefixo.
        /// </summary>
        public string PrefixAtual { get; set; } = string.Empty;

        /// <summary>
        /// Texto pelo qual o <see cref="PrefixAtual"/> será substituído.
        /// Deixe vazio para desabilitar a substituição de prefixo.
        /// </summary>
        public string PrefixNovo { get; set; } = string.Empty;

        /// <summary>
        /// Se verdadeiro, apaga todas as coleções existentes antes de criar as novas.
        /// ATENÇÃO: operação destrutiva e irreversível.
        /// </summary>
        public bool ApagarTudo { get; set; } = false;

        /// <summary>
        /// Se verdadeiro, apenas cria coleções para pastas que ainda não possuem
        /// uma coleção correspondente na biblioteca do Jellyfin.
        /// Recomendado para execuções incrementais.
        /// </summary>
        public bool OnlyNew { get; set; } = true;

        #endregion Regras de Processamento
    }
}