using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LeagueSharp.Loader.Data.Assemblies
{
    public class AssemblieService
    {
        [JsonProperty("array")]
        public IList<Assembly> Items { get; set; }
    }
}
