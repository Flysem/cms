using Datory;

namespace SiteServer.CMS.Database.Models
{
    [Table("siteserver_TableStyleItem")]
    public class TableStyleItemInfo : Entity
    {
        [TableColumn]
        public int TableStyleId { get; set; }

        [TableColumn]
        public string ItemTitle { get; set; }

        [TableColumn]
        public string ItemValue { get; set; }

        [TableColumn]
        private string IsSelected { get; set; }

        public bool Selected
        {
            get => IsSelected == "True";
            set => IsSelected = value.ToString();
        }
    }
}
