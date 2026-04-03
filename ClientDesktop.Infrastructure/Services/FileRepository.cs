using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using Newtonsoft.Json;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Generic repository for saving and loading data to and from the local file system with encryption.
    /// </summary>
    public class FileRepository<T> : IRepository<T>
    {
        #region Fields

        private readonly string _baseFolder;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the FileRepository class and ensures the base directory exists.
        /// </summary>
        public FileRepository()
        {
            try
            {
                _baseFolder = AppConfig.AppDataPath;
                if (!Directory.Exists(_baseFolder))
                {
                    Directory.CreateDirectory(_baseFolder);
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(FileRepository<T>), ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Saves the specified data to a file, optionally under a specific key within a JSON dictionary, applying encryption.
        /// </summary>
        public void Save(string filename, T data, string key = null)
        {
            try
            {
                string path = Path.Combine(_baseFolder, filename + ".dat");
                string dir = Path.GetDirectoryName(path);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string contentToSave;

                if (string.IsNullOrEmpty(key))
                {
                    contentToSave = JsonConvert.SerializeObject(data);
                }
                else
                {
                    Dictionary<string, object> dataDictionary = new Dictionary<string, object>();

                    if (File.Exists(path))
                    {
                        try
                        {
                            string existingEncrypted = File.ReadAllText(path);
                            string existingJson = AESHelper.DecompressAndDecryptString(existingEncrypted);
                            var existingDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(existingJson);

                            if (existingDict != null)
                            {
                                dataDictionary = existingDict;
                            }
                        }
                        catch (Exception ex)
                        {
                            FileLogger.ApplicationLog($"{nameof(Save)}_Deserialization", ex);
                            // Ignore deserialization errors to start fresh
                        }
                    }

                    dataDictionary[key] = data;
                    contentToSave = JsonConvert.SerializeObject(dataDictionary);
                }

                string encrypted = AESHelper.CompressAndEncryptString(contentToSave);
                File.WriteAllText(path, encrypted);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Save), ex);
            }
        }

        /// <summary>
        /// Loads and decrypts data from a file, optionally extracting it via a specific key.
        /// </summary>
        public T Load(string filename, string key = null)
        {
            try
            {
                string path = Path.Combine(_baseFolder, filename + ".dat");
                if (!File.Exists(path)) return default;

                string encrypted = File.ReadAllText(path);
                string json = AESHelper.DecompressAndDecryptString(encrypted);

                if (string.IsNullOrEmpty(key))
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                else
                {
                    var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (dataDictionary != null && dataDictionary.ContainsKey(key))
                    {
                        var itemJson = dataDictionary[key].ToString();
                        return JsonConvert.DeserializeObject<T>(itemJson);
                    }
                }

                return default;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Load), ex);
                return default;
            }
        }

        #endregion
    }
}