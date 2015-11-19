using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Loader.Class;
using System.ComponentModel;
using Newtonsoft.Json;

namespace LeagueSharp.Loader.Data.Assemblies
{

    public class Assembly
    {
        [JsonProperty("author_group_bold")]
        public bool AuthorGroupBold { get; set; }
        [JsonProperty("author_group_color")]
        public string AuthorGroupColor { get; set; }
        [JsonProperty("author_name")]
        public string AuthorName { get; set; }
        [JsonProperty("champions")]
        public IList<string> Champions { get; set; }
        [JsonProperty("ci_state")]
        public int CiState { get; set; }
        public int Count { get; set; } // TODO: move
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("github_url")]
        public string GithubUrl { get; set; }
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("image", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue("http://s3.amazonaws.com/f.cl.ly/items/092B0A1M0m0L0a3f2W0T/placeholder-assembly.png")]
        public string Image { get; set; }
        public bool Installed { get; set; } // TODO: move
        public bool IsChecked { get; set; } // TODO: move
        public bool UpVoted { get; set; } // TODO: move
        [JsonProperty("last_update")]
        public int LastUpdate { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        public AssemblyType Type { get; set; }
        [JsonProperty("topic_id")]
        public int TopicId { get; set; }
    }
}
