using System.Collections.Generic;
using Datory;
using SiteServer.Utils;

namespace SiteServer.CMS.Database.Models
{
    [Table("siteserver_PermissionsInRoles")]
    public class PermissionsInRolesInfo : Entity
    {
        [TableColumn]
        public string RoleName { get; set; }

        [TableColumn(Text = true)]
        private string GeneralPermissions { get; set; }

        public List<string> GeneralPermissionList
        {
            get => TranslateUtils.StringCollectionToStringList(GeneralPermissions);
            set => GeneralPermissions = TranslateUtils.ObjectCollectionToString(value);
        }
    }
}
