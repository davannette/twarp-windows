using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Twarp.Storage
{
    class LocalFile<T>
    {
        public String filename { get; set; }
        public Boolean loaded { get; set; }
        public Boolean saved { get; set; }
        public Boolean newfile { get; set; }

        public T data { get; set; }

        public LocalFile(String fname)
        {
            filename = fname;
            
            // initialise booleans:
            loaded = false;
            newfile = false;
            saved = false;
        }

        public async Task load() {

            // load file:
            try
            {
                StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile datafile = await storageFolder.GetFileAsync(filename);
                var json = await Windows.Storage.FileIO.ReadTextAsync(datafile);
                data = (T)JsonConvert.DeserializeObject<T>(json);

                loaded = true;
            }
            catch (FileNotFoundException ex)
            {
                // find out through exception 
                data = default(T);
            }
        }

        public async Task save() { 

            // save favourites:
            StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile datafile;
            try
            {
                datafile = await storageFolder.GetFileAsync(filename);
            }
            catch (FileNotFoundException ex)
            {
                datafile = await storageFolder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            }
            var json = JsonConvert.SerializeObject(data);
            await Windows.Storage.FileIO.WriteTextAsync(datafile, json);
        }
    }
}
