﻿using System;
using System.Web.UI.WebControls;
using SiteServer.BackgroundPages.Utils;
using SiteServer.CMS.Caches;
using SiteServer.Utils;
using SiteServer.CMS.Database.Core;
using SiteServer.CMS.Fx;
using SiteServer.Utils.Enumerations;

namespace SiteServer.BackgroundPages.Cms
{
	public class PageConfigurationSite : BasePageCms
    {
        public DropDownList DdlCharset;
        public TextBox TbPageSize;
        public DropDownList DdlIsCreateDoubleClick;

        public static string GetRedirectUrl(int siteId)
        {
            return FxUtils.GetCmsUrl(siteId, nameof(PageConfigurationSite), null);
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            WebPageUtils.CheckRequestParameter("siteId");

            if (IsPostBack) return;

            VerifySitePermissions(ConfigManager.WebSitePermissions.Configration);

            FxUtils.AddListItemsToECharset(DdlCharset);
            SystemWebUtils.SelectSingleItem(DdlCharset, SiteInfo.Charset);

            TbPageSize.Text = SiteInfo.PageSize.ToString();

            FxUtils.AddListItems(DdlIsCreateDoubleClick, "启用双击生成", "不启用");
            SystemWebUtils.SelectSingleItemIgnoreCase(DdlIsCreateDoubleClick, SiteInfo.IsCreateDoubleClick.ToString());
        }

        public override void Submit_OnClick(object sender, EventArgs e)
		{
		    if (!Page.IsPostBack || !Page.IsValid) return;

            if (SiteInfo.Charset != DdlCharset.SelectedValue)
		    {
		        SiteInfo.Charset = DdlCharset.SelectedValue;
		    }

		    SiteInfo.PageSize = TranslateUtils.ToInt(TbPageSize.Text, SiteInfo.PageSize);
		    SiteInfo.IsCreateDoubleClick = TranslateUtils.ToBool(DdlIsCreateDoubleClick.SelectedValue);

            DataProvider.Site.Update(SiteInfo);

            AuthRequest.AddSiteLog(SiteId, "修改站点设置");

            SuccessMessage("站点设置修改成功！");
        }
	}
}
