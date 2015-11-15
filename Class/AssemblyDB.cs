using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Loader.Data.Assemblies;
using Newtonsoft.Json;

namespace LeagueSharp.Loader.Class
{
    class AssemblyDB
    {
        public static ObservableCollection<Assembly> getAssembliesFromDB()
        {
            try
            {
                var assemblyJson = "";

                using (var client = new WebClient())
                {
                    var uri = new Uri("https://services.joduska.me/loader/v1.0/static/1");
                    assemblyJson = client.DownloadString(uri);
                }

                var converted = JsonConvert.DeserializeObject<ObservableCollection<Assembly>>(assemblyJson);

                return converted ?? null;

            }
            catch
            {
                return null;
            }
        }
    }
}
