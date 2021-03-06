﻿using System;
using System.Data;
using System.Web.UI.WebControls;
using SiteServer.CMS.Core;
using SiteServer.Utils;
using SiteServer.CMS.Model.Enumerations;
using SiteServer.CMS.StlParser.Model;
using SiteServer.CMS.StlParser.Utility;

namespace SiteServer.CMS.StlParser.StlElement
{
    [StlElement(Title = "翻页数据库列表", Description = "通过 stl:pageSqlContents 标签在模板中显示能够翻页的数据库列表")]
    public class StlPageSqlContents : StlSqlContents
    {
        public new const string ElementName = "stl:pageSqlContents";

        [StlAttribute(Title = "每页显示的内容数目")]
        private const string PageNum = nameof(PageNum);

        private readonly string _stlPageSqlContentsElement;
        private readonly ListInfo _listInfo;
        private readonly PageInfo _pageInfo;
        private readonly ContextInfo _contextInfo;
        private DataSet _dataSet;

        //public StlPageSqlContents(string stlPageSqlContentsElement, PageInfo pageInfo, ContextInfo contextInfo, bool isXmlContent, bool isLoadData)
        //{
        //    _stlPageSqlContentsElement = stlPageSqlContentsElement;
        //    _pageInfo = pageInfo;
        //    try
        //    {
        //        var stlElementInfo = StlParserUtility.ParseStlElement(_stlPageSqlContentsElement);

        //        _contextInfo = contextInfo.Clone(stlPageSqlContentsElement, stlElementInfo.InnerHtml, stlElementInfo.Attributes);

        //        _listInfo = ListInfo.GetListInfo(_pageInfo, _contextInfo, EContextType.SqlContent);
        //        if (isLoadData)
        //        {
        //            _dataSet = StlDataUtility.GetPageSqlContentsDataSet(_listInfo.ConnectionString, _listInfo.QueryString, _listInfo.StartNum, _listInfo.TotalNum, _listInfo.OrderByString);
        //        }
        //    }
        //    catch
        //    {
        //        _listInfo = new ListInfo();
        //    }
        //}

        public StlPageSqlContents(string stlPageSqlContentsElement, PageInfo pageInfo, ContextInfo contextInfo)
        {
            _stlPageSqlContentsElement = stlPageSqlContentsElement;
            _pageInfo = pageInfo;
            try
            {
                var stlElementInfo = StlParserUtility.ParseStlElement(stlPageSqlContentsElement);

                _contextInfo = contextInfo.Clone(stlPageSqlContentsElement, stlElementInfo.InnerHtml, stlElementInfo.Attributes);

                _listInfo = ListInfo.GetListInfo(_pageInfo, _contextInfo, EContextType.SqlContent);

                _dataSet = StlDataUtility.GetPageSqlContentsDataSet(_listInfo.ConnectionString, _listInfo.QueryString, _listInfo.StartNum, _listInfo.TotalNum, _listInfo.OrderByString);
            }
            catch
            {
                _listInfo = new ListInfo();
            }
        }

        //public void LoadData()
        //{
        //    _dataSet = StlDataUtility.GetPageSqlContentsDataSet(_listInfo.ConnectionString, _listInfo.QueryString, _listInfo.StartNum, _listInfo.TotalNum, _listInfo.OrderByString);
        //}

        public int GetPageCount(out int contentNum)
        {
            var pageCount = 1;
            contentNum = 0;//数据库中实际的内容数目
            if (_dataSet == null) return pageCount;

            contentNum = _dataSet.Tables[0].DefaultView.Count;
            if (_listInfo.PageNum != 0 && _listInfo.PageNum < contentNum)//需要翻页
            {
                pageCount = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(contentNum) / Convert.ToDouble(_listInfo.PageNum)));//需要生成的总页数
            }
            return pageCount;
        }

        public ListInfo DisplayInfo => _listInfo;

        public string Parse(int currentPageIndex, int pageCount)
        {
            var parsedContent = string.Empty;

            _contextInfo.PageItemIndex = currentPageIndex * _listInfo.PageNum;

            try
            {
                if (_dataSet != null)
                {
                    var objPage = new PagedDataSource { DataSource = _dataSet.Tables[0].DefaultView }; //分页类

                    if (pageCount > 1)
                    {
                        objPage.AllowPaging = true;
                        objPage.PageSize = _listInfo.PageNum;//每页显示的项数
                    }
                    else
                    {
                        objPage.AllowPaging = false;
                    }

                    objPage.CurrentPageIndex = currentPageIndex;//当前页的索引


                    if (_listInfo.Layout == ELayout.None)
                    {
                        var rptContents = new Repeater
                        {
                            ItemTemplate =
                                new RepeaterTemplate(_listInfo.ItemTemplate, _listInfo.SelectedItems,
                                    _listInfo.SelectedValues, _listInfo.SeparatorRepeatTemplate,
                                    _listInfo.SeparatorRepeat, _pageInfo, EContextType.SqlContent, _contextInfo)
                        };


                        if (!string.IsNullOrEmpty(_listInfo.HeaderTemplate))
                        {
                            rptContents.HeaderTemplate = new SeparatorTemplate(_listInfo.HeaderTemplate);
                        }
                        if (!string.IsNullOrEmpty(_listInfo.FooterTemplate))
                        {
                            rptContents.FooterTemplate = new SeparatorTemplate(_listInfo.FooterTemplate);
                        }
                        if (!string.IsNullOrEmpty(_listInfo.SeparatorTemplate))
                        {
                            rptContents.SeparatorTemplate = new SeparatorTemplate(_listInfo.SeparatorTemplate);
                        }
                        if (!string.IsNullOrEmpty(_listInfo.AlternatingItemTemplate))
                        {
                            rptContents.AlternatingItemTemplate = new RepeaterTemplate(_listInfo.AlternatingItemTemplate, _listInfo.SelectedItems, _listInfo.SelectedValues, _listInfo.SeparatorRepeatTemplate, _listInfo.SeparatorRepeat, _pageInfo, EContextType.SqlContent, _contextInfo);
                        }

                        rptContents.DataSource = objPage;
                        rptContents.DataBind();

                        if (rptContents.Items.Count > 0)
                        {
                            parsedContent = ControlUtils.GetControlRenderHtml(rptContents);
                        }
                    }
                    else
                    {
                        var pdlContents = new ParsedDataList();

                        //设置显示属性
                        TemplateUtility.PutListInfoToMyDataList(pdlContents, _listInfo);

                        pdlContents.ItemTemplate = new DataListTemplate(_listInfo.ItemTemplate, _listInfo.SelectedItems, _listInfo.SelectedValues, _listInfo.SeparatorRepeatTemplate, _listInfo.SeparatorRepeat, _pageInfo, EContextType.SqlContent, _contextInfo);
                        if (!string.IsNullOrEmpty(_listInfo.HeaderTemplate))
                        {
                            pdlContents.HeaderTemplate = new SeparatorTemplate(_listInfo.HeaderTemplate);
                        }
                        if (!string.IsNullOrEmpty(_listInfo.FooterTemplate))
                        {
                            pdlContents.FooterTemplate = new SeparatorTemplate(_listInfo.FooterTemplate);
                        }
                        if (!string.IsNullOrEmpty(_listInfo.SeparatorTemplate))
                        {
                            pdlContents.SeparatorTemplate = new SeparatorTemplate(_listInfo.SeparatorTemplate);
                        }
                        if (!string.IsNullOrEmpty(_listInfo.AlternatingItemTemplate))
                        {
                            pdlContents.AlternatingItemTemplate = new DataListTemplate(_listInfo.AlternatingItemTemplate, _listInfo.SelectedItems, _listInfo.SelectedValues, _listInfo.SeparatorRepeatTemplate, _listInfo.SeparatorRepeat, _pageInfo, EContextType.SqlContent, _contextInfo);
                        }

                        pdlContents.DataSource = objPage;
                        pdlContents.DataBind();

                        if (pdlContents.Items.Count > 0)
                        {
                            parsedContent = ControlUtils.GetControlRenderHtml(pdlContents);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                parsedContent = LogUtils.AddStlErrorLog(_pageInfo, ElementName, _stlPageSqlContentsElement, ex);
            }

            //还原翻页为0，使得其他列表能够正确解析ItemIndex
            _contextInfo.PageItemIndex = 0;

            return parsedContent;
        }

    }

}
