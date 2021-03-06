﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalizationContext.cs" company="Slash Games">
//   Copyright (c) Slash Games. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace BlueprintEditor.ViewModels
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;

    using BlueprintEditor.Controls;

    using CsvHelper;

    using Slash.ECS.Inspector.Attributes;
    using Slash.SystemExt.Exceptions;
    using Slash.Tools.BlueprintEditor.Logic.Context;
    using Slash.Tools.BlueprintEditor.Logic.Data;
    using Slash.Tools.BlueprintEditor.Logic.Localization;

    public class LocalizationContext
    {
        #region Constants

        public const string LocalizationExportExtension = "csv";

        #endregion

        #region Fields

        private readonly EditorContext context;

        private readonly Dictionary<string, ILocalizationTable> languages = new Dictionary<string, ILocalizationTable>();

        private readonly ILocalizationTableSerializer localizationTableSerializer =
            new LocalizationTableNGUISerializer();

        #endregion

        #region Constructors and Destructors

        public LocalizationContext(EditorContext context)
        {
            this.context = context;

            this.context.PropertyChanged += this.OnContextPropertyChanged;
        }

        #endregion

        #region Delegates

        public delegate void ProjectLanguageChangedDelegate(string newLanguage);

        #endregion

        #region Public Events

        public event ProjectLanguageChangedDelegate ProjectLanguageChanged;

        #endregion

        #region Public Properties

        public string ProjectLanguage
        {
            get
            {
                return this.context.ProjectLanguage;
            }
        }

        public bool RawLocalizationKeys
        {
            get
            {
                return this.ProjectLanguage == EditorSettings.LanguageTagRawLocalizationKeys;
            }
        }

        #endregion

        #region Properties

        private string CurrentBlueprintId
        {
            get
            {
                return this.context.SelectedBlueprint.BlueprintId;
            }
        }

        #endregion

        #region Indexers

        private string this[string localizationKey]
        {
            get
            {
                if (this.ProjectLanguage == EditorSettings.LanguageTagRawLocalizationKeys)
                {
                    return localizationKey;
                }

                return this.languages[this.ProjectLanguage][localizationKey];
            }

            set
            {
                if (this.RawLocalizationKeys)
                {
                    return;
                }

                this.languages[this.ProjectLanguage][localizationKey] = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///   Exports all localized strings as CSV to the specified stream.
        /// </summary>
        /// <param name="stream">Stream to write all localized strings to.</param>
        public void ExportLocalizationData(Stream stream)
        {
            using (var streamWriter = new StreamWriter(stream))
            {
                using (var csvWriter = new CsvWriter(streamWriter))
                {
                    // Configure writer.
                    csvWriter.Configuration.QuoteAllFields = true;

                    // Write header.
                    csvWriter.WriteField(EditorSettings.LanguageTagRawLocalizationKeys);

                    foreach (var language in this.languages.Keys)
                    {
                        csvWriter.WriteField(language);
                    }

                    csvWriter.NextRecord();

                    // Write records.
                    var blueprintManager = this.context.BlueprintManagerViewModel;

                    foreach (var blueprint in
                        blueprintManager.Blueprints.Where(
                            blueprintViewModel => blueprintViewModel.DerivedBlueprints.Count == 0))
                    {
                        foreach (var componentType in blueprint.GetComponents())
                        {
                            var componentInfo = InspectorComponentTable.Instance.GetInspectorType(componentType);
                            if (componentInfo == null)
                            {
                                continue;
                            }

                            foreach (var inspectorProperty in componentInfo.Properties)
                            {
                                var stringProperty = inspectorProperty as InspectorStringAttribute;

                                if (stringProperty != null && stringProperty.Localized)
                                {
                                    // Write localization key.
                                    var localizationKey = this.GetLocalizationKey(
                                        blueprint.BlueprintId, stringProperty.Name);
                                    csvWriter.WriteField(localizationKey);

                                    // Write localized strings.
                                    foreach (var language in this.languages.Values)
                                    {
                                        var localizedValue = language[localizationKey];
                                        csvWriter.WriteField(
                                            localizationKey == localizedValue ? string.Empty : localizedValue);
                                    }

                                    csvWriter.NextRecord();
                                }
                            }
                        }
                    }
                }
            }
        }

        public string GetLocalizedStringForBlueprint(string blueprintId, string key)
        {
            var localizationKey = this.GetLocalizationKey(blueprintId, key);
            return this[localizationKey];
        }

        public string GetLocalizedStringForCurrentBlueprint(string key)
        {
            var localizationKey = this.GetLocalizationKey(this.CurrentBlueprintId, key);
            var localizedValue = this[localizationKey];
            return localizedValue;
        }

        /// <summary>
        ///   Imports all localized strings as CSV from the specified stream.
        /// </summary>
        /// <param name="stream">Stream to read all localized strings from.</param>
        public void ImportLocalizationData(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            {
                using (var csvReader = new CsvReader(streamReader))
                {
                    // Read column headers and first row.
                    csvReader.Read();

                    while (csvReader.CurrentRecord != null)
                    {
                        var localizationId = csvReader[0];

                        for (var i = 1; i < csvReader.FieldHeaders.Length; i++)
                        {
                            var localizationLanguageId = csvReader.FieldHeaders[i];
                            var localizationTable = this.languages[localizationLanguageId];
                            var localizedValue = csvReader[i];

                            if (!string.IsNullOrEmpty(localizedValue))
                            {
                                localizationTable[localizationId] = localizedValue;
                            }
                        }

                        // Read next record.
                        csvReader.Read();
                    }
                }
            }
        }

        public void LoadLanguages()
        {
            this.languages.Clear();

            if (this.context.ProjectSettings == null)
            {
                return;
            }

            foreach (var languageFile in this.context.ProjectSettings.LanguageFiles)
            {
                var fileInfo = new FileInfo(languageFile.Path);

                try
                {
                    using (var stream = fileInfo.OpenRead())
                    {
                        var languageTag = languageFile.LanguageTag;
                        var localizationTable = this.localizationTableSerializer.Deserialize(stream);

                        this.languages.Add(languageTag, localizationTable);
                    }
                }
                catch (AggregateException e)
                {
                    EditorDialog.Error(
                        "Loading localization data failed",
                        string.Format(
                            "Please fix the following errors in {0} and re-load the project:\r\n\r\n{1}",
                            fileInfo.Name,
                            e.Messages));
                }
            }
        }

        public void SaveLanguages()
        {
            foreach (var languageFile in this.context.ProjectSettings.LanguageFiles)
            {
                var fileInfo = new FileInfo(languageFile.Path);

                using (var stream = fileInfo.Create())
                {
                    var languageTag = languageFile.LanguageTag;
                    var localizationTable = this.languages[languageTag];

                    this.localizationTableSerializer.Serialize(stream, localizationTable);
                }
            }
        }

        public void SetLocalizedStringForBlueprint(string blueprintId, string key, string value)
        {
            var localizationKey = this.GetLocalizationKey(blueprintId, key);
            this[localizationKey] = value;
        }

        public void SetLocalizedStringForCurrentBlueprint(string key, string value)
        {
            var localizationKey = this.GetLocalizationKey(this.CurrentBlueprintId, key);
            this[localizationKey] = value;
        }

        #endregion

        #region Methods

        private string GetLocalizationKey(string blueprintId, string key)
        {
            return string.Format("{0}.{1}", blueprintId, key);
        }

        private void OnContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ProjectLanguage")
            {
                this.OnProjectLanguageChanged(this.ProjectLanguage);
            }
        }

        private void OnProjectLanguageChanged(string newLanguage)
        {
            var handler = this.ProjectLanguageChanged;
            if (handler != null)
            {
                handler(newLanguage);
            }
        }

        #endregion
    }
}