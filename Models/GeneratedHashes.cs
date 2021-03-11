using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hash_comparison_tool.Models
{
    public class GeneratedHashes
    {
            public List<HashesPerQuestion> GeneratedHashesList = new List<HashesPerQuestion>();
    }
    public class HashesPerQuestion
    {
        public string QuestionNumber { get; set; }
        public List<string> Hashes = new List<string>();
    }
}
