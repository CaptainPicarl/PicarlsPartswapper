using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace PartSwapperXMLSE
{
    /*
     * A configOptions object is primarily a dictionary with key/value pairs of Configuration Options.
     * The object also attempts to provide ObservableCollections, or some other GUI-friendly objects.
     */
    public class ConfigOptions
    {
        [JsonInclude]
        public Dictionary<string, string> OptsDict = new Dictionary<string, string>();
        
        public ObservableCollection<string> KeysList = new ObservableCollection<string>();
        public ObservableCollection<string> ValuesList = new ObservableCollection<string>();

        private string _SavePath = "";
        private JsonConverter _converter;

        public ConfigOptions()
        {
        }

        public ConfigOptions(string savePath)
        {
            this._SavePath = savePath;
            return;
        }

        public ConfigOptions(string key, string value, string savePath)
        {
            this.OptsDict.Add(key, value);
            this.KeysList.Add(key);
            this.ValuesList.Add(value);
            this._SavePath = savePath;

            return;
        }
        /// <summary>
        /// Sets the option in our OptsDict, and then attempts to update the observable lists
        /// </summary>
        /// <param name="key">key to set</param>
        /// <param name="value">value to set</param>
        /// <returns>TRUE if an option was set succesfully, false otherwise.</returns>
        public bool SetOption(string key, string value)
        {
            string oldValue = "";

            if (!this.KeysList.Contains(key) || !this.OptsDict.ContainsKey(key))
            {
                return false;
            }

            if (OptsDict.ContainsKey(key) && OptsDict[key] != null)
            {
                // Update the OpsDict first, retaining the old value.
                oldValue = OptsDict[key];
                OptsDict[key] = value;

                //...by removing the old value from the values list first
                ValuesList.Remove(oldValue);
                //...And then adding the new value!
                ValuesList.Add(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        public string? GetOption(string key)
        {
            if (OptsDict.ContainsKey(key))
            {
                return OptsDict[key];
            }
            else
            {
                return null;
            }
        }

        // This method loads the config file, and if present: Assembles an object from it
        public void LoadOrCreateConfig()
        {
            ConfigOptions? configOptions;
            JsonSerializerOptions? jsonSerializerOptions = new JsonSerializerOptions();

            if (File.Exists(this._SavePath))
            {
                configOptions = JsonSerializer.Deserialize<ConfigOptions>(File.ReadAllText(this._SavePath));

                if (configOptions != null)
                {
                    this.OptsDict = configOptions.OptsDict;

                    GenerateKeysFromMainDict();
                    GenerateValuesFromMainDict();
                }
            }
            else
            {
                // Situation where our config file does not exist
                string serializedCO = JsonSerializer.Serialize(this);
                Directory.CreateDirectory(Path.GetDirectoryName(this._SavePath));
                File.WriteAllText(this._SavePath, serializedCO);
            }
        }

        public void GenerateKeysFromMainDict()
        {
            this.KeysList.Clear();

            foreach (string key in OptsDict.Keys)
            {
                this.KeysList.Add(key);
            }
        }

        public void GenerateValuesFromMainDict()
        {
            this.ValuesList.Clear();

            foreach (string value in OptsDict.Values)
            {
                this.ValuesList.Add(value);
            }
        }

        public void SaveConfig()
        {

            string serializedCO;
            JsonSerializerOptions serializerOpts = new JsonSerializerOptions();

            // Serializer options
            serializerOpts.IncludeFields = true;

            serializedCO = JsonSerializer.Serialize(this, serializerOpts);

            if (System.IO.File.Exists(this._SavePath))
            {
                File.WriteAllText(this._SavePath, serializedCO);
            }
            else
            {
                // Situation where our config file does not exist

                // case where the appDataPath is, for some reason, invalid
                if (System.IO.Path.GetDirectoryName(this._SavePath) == null || System.IO.Path.GetDirectoryName(this._SavePath).Equals(""))
                {
                    throw new InvalidDataException("Path.GetDirectoryName(appDataPath) invalid!");
                }
                else
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(this._SavePath));
                    File.WriteAllText(this._SavePath, serializedCO);
                }
            }
        }

        // Prompts the user to locate their Mods Dir, then sets the _config object appropriately.
        // returns true if the path was set, false otherwise
        public bool ConfigOptionsPromptSetDirectory(string messageBoxText, string caption, string pathMustEndWith, string optionKey)
        {

        PromptLoop:
            OpenFolderDialog folderDialogue = new OpenFolderDialog();
            MessageBoxButton locateSEDiagButton = MessageBoxButton.OKCancel;
            MessageBoxImage locateSEDiagImage = MessageBoxImage.Exclamation;

            // Show the dialog box
            MessageBoxResult locateDirDialogResult = MessageBox.Show(messageBoxText, caption, locateSEDiagButton, locateSEDiagImage, MessageBoxResult.OK, MessageBoxOptions.None);

            if (locateDirDialogResult == MessageBoxResult.Cancel)
            {
                MessageBox.Show($"User did not select a directory! Leaving without setting {optionKey}! This may cause crashes!");
                return false;
            }

            folderDialogue.Title = "Select the path ending with: " + pathMustEndWith;
            folderDialogue.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            folderDialogue.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            folderDialogue.ShowDialog();


            // If the dialogue returns the DefaultDirectory/InitialDirectory...we assume the user chose nothing.
            if (!folderDialogue.FolderName.EndsWith(pathMustEndWith))
            {
                // Force the user to select a valid folder
                MessageBox.Show($"Invalid folder selected! Path must end with: {pathMustEndWith}. Re-prompting!");
                goto PromptLoop;
            }
            else
            {
                // create a new ConfigOptions
                this.SetOption(optionKey, folderDialogue.FolderName);

                // Save the new _configOptions immediately
                this.SaveConfig();
                return true;
            }
        }

    }


}
