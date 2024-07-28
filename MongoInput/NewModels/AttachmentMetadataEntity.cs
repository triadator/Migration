using MongoInput.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoInput.NewModels
{
    public class AttachmentMetadataEntity
    {
        public string Id { get; set; }
        public string RelatedItemId { get; set; }
        public string RelatedListId { get; set; }
        public string SiteId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime Updated { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public AttachmentTypeEntity AttachmentType { get; set; }
    }
}
