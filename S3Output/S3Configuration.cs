using Amazon.Runtime.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3Output
{
    public class S3Configuration
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string ServiceURL { get; set; }
    }
}
