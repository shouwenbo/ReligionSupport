using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoFileDownloaderApp
{
    public class AudioItem
    {
        public string DisplayName { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
