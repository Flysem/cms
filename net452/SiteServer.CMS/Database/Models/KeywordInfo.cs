﻿using Datory;
using SiteServer.CMS.Core.Enumerations;

namespace SiteServer.CMS.Database.Models
{
    [Table("siteserver_ErrorLog")]
    public class KeywordInfo : Entity
    {
        [TableColumn]
        public string Keyword { get; set; }

        [TableColumn]
        public string Alternative { get; set; }

        [TableColumn]
        private string Grade { get; set; }

        public EKeywordGrade KeywordGrade
        {
            get => EKeywordGradeUtils.GetEnumType(Grade);
            set => Grade = EKeywordGradeUtils.GetValue(value);
        }
    }
}
