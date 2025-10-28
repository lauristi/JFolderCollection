// <copyright file="PluginConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>


namespace JFolderCollection.Configuration;
using MediaBrowser.Model.Plugins;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class with default settings.
    /// </summary>
    public enum SomeOptions
    {
        /// <summary>
        /// Option one.
        /// </summary>
        OneOption,

        /// <summary>
        /// Second option.
        /// </summary>
        AnotherOption,
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class with default settings.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class with default settings.
        /// </summary>
        public bool TrueFalseSetting { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class with default settings.
        /// </summary>
        public int AnInteger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class with default settings.
        /// </summary>
        public string AString { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class with default settings.
        /// </summary>
        public SomeOptions Options { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class with default settings.
        /// </summary>
        public string BaseFolderPath { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class with default settings.
        /// </summary>
        public PluginConfiguration()
        {
            // set default options here
            this.Options = SomeOptions.AnotherOption;
            this.TrueFalseSetting = true;
            this.AnInteger = 2;
            this.AString = "string";
            this.BaseFolderPath = "/mnt/xs1000/Filmes/Filmes Colecoes";
        }
    }
