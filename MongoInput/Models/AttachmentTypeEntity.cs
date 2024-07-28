using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoInput.Models
{
    public class AttachmentTypeEntity
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string ContentTypeId { get; set; }
        public string StaticName { get; set; }
        public bool IsDefault { get; set; }
        public bool NotAllowedSelectOnForm { get; set; }
    }
}
