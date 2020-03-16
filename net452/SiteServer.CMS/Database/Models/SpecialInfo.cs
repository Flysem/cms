using System;
using Datory;

namespace SiteServer.CMS.Database.Models
{
    [Table("siteserver_Special")]
    public class SpecialInfo : Entity
    {
        [TableColumn]
        public int SiteId { get; set; }

        [TableColumn]
        public string Title { get; set; }

        [TableColumn]
        public string Url { get; set; }

        [TableColumn]
        public DateTime? AddDate { get; set; }
    }
}
