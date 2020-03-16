﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using Datory;
using SiteServer.BackgroundPages.Cms;
using SiteServer.CMS.Apis;
using SiteServer.CMS.Caches;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Enumerations;
using SiteServer.CMS.Database.Core;
using SiteServer.CMS.Database.Models;
using SiteServer.CMS.Database.Repositories.Contents;
using SiteServer.CMS.Fx;
using SiteServer.CMS.Plugin.Impl;
using SiteServer.Plugin;
using SiteServer.Utils;
using SiteServer.Utils.Enumerations;

namespace SiteServer.API.Controllers.Pages.Settings
{
    [RoutePrefix("pages/settings/siteAdd")]
    public class PagesSiteAddController : ApiController
    {
        private const string Route = "";

        [HttpGet, Route(Route)]
        public IHttpActionResult GetConfig()
        {
            try
            {
                var rest = Request.GetAuthenticatedRequest();
                if (!rest.IsAdminLoggin ||
                    !rest.AdminPermissions.HasSystemPermissions(ConfigManager.SettingsPermissions.SiteAdd))
                {
                    return Unauthorized();
                }

                var siteTemplates = SiteTemplateManager.Instance.GetSiteTemplateSortedList();

                var siteList = new List<KeyValuePair<int, string>>
                {
                    new KeyValuePair<int, string>(0, "<无上级站点>")
                };

                var siteIdList = SiteManager.GetSiteIdList();
                var siteInfoList = new List<SiteInfo>();
                var parentWithChildren = new Dictionary<int, List<SiteInfo>>();
                foreach (var siteId in siteIdList)
                {
                    var siteInfo = SiteManager.GetSiteInfo(siteId);
                    if (siteInfo.Root == false)
                    {
                        if (siteInfo.ParentId == 0)
                        {
                            siteInfoList.Add(siteInfo);
                        }
                        else
                        {
                            var children = new List<SiteInfo>();
                            if (parentWithChildren.ContainsKey(siteInfo.ParentId))
                            {
                                children = parentWithChildren[siteInfo.ParentId];
                            }
                            children.Add(siteInfo);
                            parentWithChildren[siteInfo.ParentId] = children;
                        }
                    }
                }
                foreach (SiteInfo siteInfo in siteInfoList)
                {
                    AddSite(siteList, siteInfo, parentWithChildren, 0);
                }

                var tableNameList = SiteManager.GetSiteTableNames();

                var isRootExists = SiteManager.GetSiteInfoByIsRoot() != null;

                return Ok(new
                {
                    Value = siteTemplates.Values,
                    IsRootExists = isRootExists,
                    SiteList = siteList,
                    TableNameList = tableNameList
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private static void AddSite(List<KeyValuePair<int, string>> siteList, SiteInfo siteInfo, Dictionary<int, List<SiteInfo>> parentWithChildren, int level)
        {
            if (level > 1) return;
            var padding = string.Empty;
            for (var i = 0; i < level; i++)
            {
                padding += "　";
            }
            if (level > 0)
            {
                padding += "└ ";
            }

            if (parentWithChildren.ContainsKey(siteInfo.Id))
            {
                var children = parentWithChildren[siteInfo.Id];
                siteList.Add(new KeyValuePair<int, string>(siteInfo.Id, padding + siteInfo.SiteName + $"({children.Count})"));
                level++;
                foreach (var subSiteInfo in children)
                {
                    AddSite(siteList, subSiteInfo, parentWithChildren, level);
                }
            }
            else
            {
                siteList.Add(new KeyValuePair<int, string>(siteInfo.Id, padding + siteInfo.SiteName));
            }
        }

        [HttpPost, Route(Route)]
        public IHttpActionResult Submit()
        {
            try
            {
                var rest = Request.GetAuthenticatedRequest();
                if (!rest.IsAdminLoggin ||
                    !rest.AdminPermissions.HasSystemPermissions(ConfigManager.SettingsPermissions.SiteAdd))
                {
                    return Unauthorized();
                }

                var createType = Request.GetPostString("createType");
                var createTemplateId = Request.GetPostString("createTemplateId");
                var siteName = Request.GetPostString("siteName");
                var isRoot = Request.GetPostBool("isRoot");
                var parentId = Request.GetPostInt("parentId");
                var siteDir = Request.GetPostString("siteDir");
                var tableRule = ETableRuleUtils.GetEnumType(Request.GetPostString("tableRule"));
                var tableChoose = Request.GetPostString("tableChoose");
                var tableHandWrite = Request.GetPostString("tableHandWrite");
                var isImportContents = Request.GetPostBool("isImportContents");
                var isImportTableStyles = Request.GetPostBool("isImportTableStyles");

                if (!isRoot)
                {
                    if (DirectoryUtils.IsSystemDirectory(siteDir))
                    {
                        return BadRequest("文件夹名称不能为系统文件夹名称，请更改文件夹名称！");
                    }
                    if (!DirectoryUtils.IsDirectoryNameCompliant(siteDir))
                    {
                        return BadRequest("文件夹名称不符合系统要求，请更改文件夹名称！");
                    }
                    var list = DataProvider.Site.GetLowerSiteDirList(parentId);
                    if (list.IndexOf(siteDir.ToLower()) != -1)
                    {
                        return BadRequest("已存在相同的发布路径，请更改文件夹名称！");
                    }
                }

                var channelInfo = new ChannelInfo();

                channelInfo.ChannelName = channelInfo.IndexName = "首页";
                channelInfo.ParentId = 0;
                channelInfo.ContentModelPluginId = string.Empty;

                var tableName = string.Empty;
                if (tableRule == ETableRule.Choose)
                {
                    tableName = tableChoose;
                }
                else if (tableRule == ETableRule.HandWrite)
                {
                    tableName = tableHandWrite;
                    if (!DatoryUtils.IsTableExists(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString, tableName))
                    {
                        TableColumnManager.CreateTable(tableName, DataProvider.ContentRepository.TableColumnsDefault, string.Empty, true, out _);
                    }
                    else
                    {
                        TableColumnManager.AlterTable(tableName, DataProvider.ContentRepository.TableColumnsDefault, string.Empty);
                    }
                }

                var siteInfo = new SiteInfo
                {
                    SiteName = AttackUtils.FilterXss(siteName),
                    SiteDir = siteDir,
                    TableName = tableName,
                    ParentId = parentId,
                    Root = isRoot
                };

                siteInfo.IsCheckContentLevel = false;
                siteInfo.Charset = ECharsetUtils.GetValue(ECharset.utf_8);

                var siteId = DataProvider.Channel.InsertSiteInfo(channelInfo, siteInfo, rest.AdminName);

                if (string.IsNullOrEmpty(tableName))
                {
                    tableName = ContentRepository.GetContentTableName(siteId);
                    if (!DatoryUtils.IsTableExists(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString, tableName))
                    {
                        TableColumnManager.CreateTable(tableName, DataProvider.ContentRepository.TableColumnsDefault, string.Empty, true, out _);
                    }
                    DataProvider.Site.UpdateTableName(siteId, tableName);
                }

                if (!rest.AdminPermissions.IsSuperAdmin())
                {
                    var siteIdList = rest.AdminPermissions.GetSiteIdList() ?? new List<int>();
                    siteIdList.Add(siteId);
                    var adminInfo = AdminManager.GetAdminInfoByUserId(rest.AdminId);
                    DataProvider.Administrator.UpdateSiteIdCollection(adminInfo, TranslateUtils.ObjectCollectionToString(siteIdList));
                }

                var siteTemplateDir = string.Empty;
                var onlineTemplateName = string.Empty;
                if (StringUtils.EqualsIgnoreCase(createType, "local"))
                {
                    siteTemplateDir = createTemplateId;
                }
                else if (StringUtils.EqualsIgnoreCase(createType, "cloud"))
                {
                    onlineTemplateName = createTemplateId;
                }

                var redirectUrl = PageProgressBar.GetCreateSiteUrl(siteId,
                    isImportContents, isImportTableStyles, siteTemplateDir, onlineTemplateName, StringUtils.GetGuid());

                return Ok(new
                {
                    Value = redirectUrl
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
