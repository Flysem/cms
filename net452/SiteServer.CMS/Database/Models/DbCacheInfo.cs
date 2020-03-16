using System;
using Datory;

namespace SiteServer.CMS.Database.Models
{
    [Table("siteserver_DbCache")]
    public class DbCacheInfo : Entity
    {
        [TableColumn]
        public string CacheKey { get; set; }

        [TableColumn]
        public string CacheValue { get; set; }

        [TableColumn]
        public DateTime? AddDate { get; set; }
    }
}
