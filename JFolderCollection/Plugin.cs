namespace JFolderCollection
{
    using JFolderCollection.Configuration;
    using MediaBrowser.Common.Configuration;
    using MediaBrowser.Common.Plugins;
    using MediaBrowser.Model.Plugins;
    using MediaBrowser.Model.Serialization;
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Classe principal do plugin JFolderCollection.
    /// Gerencia a integração com o servidor Jellyfin e a exposição da interface administrativa.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        #region Construtor e Instância Static

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Plugin"/>.
        /// </summary>
        /// <param name="applicationPaths">Caminhos de aplicação fornecidos pelo servidor.</param>
        /// <param name="xmlSerializer">Serializador XML para persistência de dados.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            // Log de depuração para confirmar o carregamento do plugin no início do servidor
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [JFolderCollection] 🎯 Plugin construído com sucesso.");
        }

        /// <summary>
        /// Obtém a instância atual (Singleton) do plugin para acesso global interno.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        #endregion

        #region Metadados do Plugin

        /// <inheritdoc />
        public override string Name => "JFolderCollection";

        /// <inheritdoc />
        /// <remarks>
        /// Identificador único (GUID) gerado para este plugin. 
        /// Não deve ser alterado após o lançamento para não perder as configurações salvas.
        /// </remarks>
        public override Guid Id => Guid.Parse("c7b8d1b3-41d9-4a19-b04e-f43534455342");

        /// <inheritdoc />
        public override string Description => "Plugin para gerenciar e listar pastas de mídia no Jellyfin.";

        #endregion

        #region Implementação de UI (IHasWebPages)

        /// <summary>
        /// Define as páginas de configuração que serão exibidas no painel de controle do Jellyfin.
        /// </summary>
        /// <returns>Uma lista de informações sobre a página HTML incorporada.</returns>
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    // O caminho aponta para o recurso incorporado (Embedded Resource) no assembly
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
            };
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            // Ajuste o nome "thumb.png" para o nome real da sua imagem embutida
            return type.Assembly.GetManifestResourceStream($"{type.Namespace}.plugin-thumbnail.png");
        }

        #endregion
    }
}