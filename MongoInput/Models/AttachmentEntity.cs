using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoInput.Models
{
    public class AttachmentEntity
    {
        public string Id { get; set; }
        public string RelatedItemId { get; set; }
        public string RelatedListId { get; set; }
        public string FileName { get; set; }
        public DateTime UploadDate { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public Stream Content { get; set; }
        public AttachmentTypeEntity AttachmentType { get; set; }

    }
}
