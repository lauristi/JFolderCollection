namespace JFolderCollection.Configuration
{
    using MediaBrowser.Model.Plugins;

    #region Enumerações

    /// <summary>
    /// Define as opções de configuração disponíveis para o plugin.
    /// Útil para criar seletores (dropdowns) na interface administrativa.
    /// </summary>
    public enum SomeOptions
    {
        /// <summary>
        /// Representa a primeira opção de configuração.
        /// </summary>
        OneOption,

        /// <summary>
        /// Representa a segunda opção de configuração.
        /// </summary>
        AnotherOption,
    }

    #endregion

    /// <summary>
    /// Classe que define a estrutura de dados das configurações do plugin.
    /// O Jellyfin serializa esta classe automaticamente em um arquivo XML.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        #region Propriedades de Configuração

        /// <summary>
        /// Obtém ou define um valor que indica se uma funcionalidade específica está ativa.
        /// </summary>
        public bool TrueFalseSetting { get; set; }

        /// <summary>
        /// Obtém ou define um valor numérico inteiro de configuração.
        /// </summary>
        public int AnInteger { get; set; }

        /// <summary>
        /// Obtém ou define uma string genérica de configuração.
        /// </summary>
        public string AString { get; set; } = string.Empty;

        /// <summary>
        /// Obtém ou define a opção selecionada a partir da enumeração <see cref="SomeOptions"/>.
        /// </summary>
        public SomeOptions Options { get; set; }

        /// <summary>
        /// Obtém ou define o caminho base do diretório de mídia que o plugin deve processar.
        /// </summary>
        public string BaseFolderPath { get; set; } = string.Empty;

        #endregion

        #region Inicialização

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="PluginConfiguration"/> com valores padrão.
        /// </summary>
        public PluginConfiguration()
        {
            // Definição dos valores padrão (Default Settings)
            Options = SomeOptions.AnotherOption;
            TrueFalseSetting = true;
            AnInteger = 2;
            AString = "string";

            // Caminho padrão conforme estrutura do servidor
            BaseFolderPath = "/mnt/xs1000/Filmes/Filmes Colecoes";
        }

        #endregion
    }
}