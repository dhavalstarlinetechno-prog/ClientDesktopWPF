using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Helpers;
using Newtonsoft.Json;

namespace ClientDesktop.Infrastructure.Services
{
    public class FileRepository<T> : IRepository<T>
    {
        private readonly string _baseFolder;

        public FileRepository()
        {
            _baseFolder = AppConfig.AppDataPath;
            if (!Directory.Exists(_baseFolder)) Directory.CreateDirectory(_baseFolder);
        }

        public void Save(string filename, T data, string key = null)
        {
            try
            {
                string path = Path.Combine(_baseFolder, filename + ".dat");

                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

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
                        catch { /* Ignore errors, start fresh */ }
                    }

                    dataDictionary[key] = data;
                    contentToSave = JsonConvert.SerializeObject(dataDictionary);
                }

                string encrypted = AESHelper.CompressAndEncryptString(contentToSave);
                File.WriteAllText(path, encrypted);
            }
            catch (Exception ex) { Console.WriteLine("Save Error: " + ex.Message); }
        }

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
            catch { return default; }
        }
    }
}