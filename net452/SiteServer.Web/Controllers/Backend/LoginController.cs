﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using System.Web.Http;
using SiteServer.BackgroundPages.Core;
using SiteServer.CMS.Caches;
using SiteServer.CMS.Core;
using SiteServer.CMS.Database.Core;
using SiteServer.CMS.Database.Models;
using SiteServer.CMS.Fx;
using SiteServer.Plugin;
using SiteServer.Utils;

namespace SiteServer.API.Controllers.Backend
{
    [RoutePrefix("backend/login")]
    public class LoginController : ApiController
    {
        private const string Route = "";
        private const string RouteCaptcha = "captcha";

        private static readonly Color[] Colors = { Color.FromArgb(37, 72, 91), Color.FromArgb(68, 24, 25), Color.FromArgb(17, 46, 2), Color.FromArgb(70, 16, 100), Color.FromArgb(24, 88, 74) };

        public static object AdminRedirectCheck(IAuthenticatedRequest request, bool checkInstall = false, bool checkDatabaseVersion = false,
            bool checkLogin = false)
        {
            var redirect = false;
            var redirectUrl = string.Empty;

            if (checkInstall && string.IsNullOrWhiteSpace(WebConfigUtils.ConnectionString))
            {
                redirect = true;
                redirectUrl = FxUtils.GetAdminUrl("installer/default.aspx");
            }
            else if (checkDatabaseVersion && ConfigManager.Instance.Initialized &&
                     ConfigManager.Instance.DatabaseVersion != SystemManager.Version)
            {
                redirect = true;
                redirectUrl = AdminPagesUtils.UpdateUrl;
            }
            else if (checkLogin && !request.IsAdminLoggin)
            {
                redirect = true;
                redirectUrl = AdminPagesUtils.LoginUrl;
            }

            if (redirect)
            {
                return new
                {
                    Value = false,
                    RedirectUrl = redirectUrl
                };
            }

            return null;
        }

        [HttpGet, Route(Route)]
        public IHttpActionResult GetStatus()
        {
            try
            {
                var rest = Request.GetAuthenticatedRequest();
                var redirect = AdminRedirectCheck(rest, checkInstall: true, checkDatabaseVersion: true);
                if (redirect != null) return Ok(redirect);

                return Ok(new
                {
                    Value = true
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet, Route(RouteCaptcha)]
        public void GetCaptcha()
        {
            var response = HttpContext.Current.Response;

            var code = VcManager.CreateValidateCode();
            if (CacheUtils.Exists($"SiteServer.API.Controllers.Admin.LoginController.{code}"))
            {
                code = VcManager.CreateValidateCode();
            }

            CookieUtils.SetCookie("SS-" + nameof(LoginController), code, TimeSpan.FromMinutes(10));

            response.BufferOutput = true;  //特别注意
            response.Cache.SetExpires(DateTime.Now.AddMilliseconds(-1));//特别注意
            response.Cache.SetCacheability(HttpCacheability.NoCache);//特别注意
            response.AppendHeader("Pragma", "No-Cache"); //特别注意
            response.ContentType = "image/png";

            byte[] buffer;

            using (var image = new Bitmap(130, 53, PixelFormat.Format32bppRgb))
            {
                var r = new Random();
                var colors = Colors[r.Next(0, 5)];

                using (var g = Graphics.FromImage(image))
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 243, 248)), 0, 0, 200, 200); //矩形框
                    g.DrawString(code, new Font(FontFamily.GenericSerif, 28, FontStyle.Bold | FontStyle.Italic), new SolidBrush(colors), new PointF(14, 3));//字体/颜色

                    var random = new Random();

                    for (var i = 0; i < 25; i++)
                    {
                        var x1 = random.Next(image.Width);
                        var x2 = random.Next(image.Width);
                        var y1 = random.Next(image.Height);
                        var y2 = random.Next(image.Height);

                        g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2);
                    }

                    for (var i = 0; i < 100; i++)
                    {
                        var x = random.Next(image.Width);
                        var y = random.Next(image.Height);

                        image.SetPixel(x, y, Color.FromArgb(random.Next()));
                    }

                    g.Save();
                }

                using (var ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    buffer = ms.ToArray();
                }
            }

            response.ClearContent();
            response.BinaryWrite(buffer);
            response.End();
        }

        [HttpPost, Route(Route)]
        public IHttpActionResult Login()
        {
            try
            {
                var rest = Request.GetAuthenticatedRequest();

                var account = Request.GetPostString("account");
                var password = Request.GetPostString("password");
                var captcha = Request.GetPostString("captcha");
                var isAutoLogin = Request.GetPostBool("isAutoLogin");

                var code = CookieUtils.GetCookie("SS-" + nameof(LoginController));

                if (string.IsNullOrEmpty(code) || CacheUtils.Exists($"SiteServer.API.Controllers.Admin.LoginController.{code}"))
                {
                    return BadRequest("验证码已超时，请点击刷新验证码！");
                }

                CookieUtils.Erase("SS-" + nameof(LoginController));
                CacheUtils.InsertMinutes($"SiteServer.API.Controllers.Admin.LoginController.{code}", true, 10);

                if (!StringUtils.EqualsIgnoreCase(code, captcha))
                {
                    return BadRequest("验证码不正确，请重新输入！");
                }

                AdministratorInfo adminInfo;

                if (!DataProvider.Administrator.Validate(account, password, true, out var userName, out var errorMessage))
                {
                    adminInfo = AdminManager.GetAdminInfoByUserName(userName);
                    if (adminInfo != null)
                    {
                        DataProvider.Administrator.UpdateLastActivityDateAndCountOfFailedLogin(adminInfo); // 记录最后登录时间、失败次数+1
                    }
                    return BadRequest(errorMessage);
                }

                adminInfo = AdminManager.GetAdminInfoByUserName(userName);
                DataProvider.Administrator.UpdateLastActivityDateAndCountOfLogin(adminInfo); // 记录最后登录时间、失败次数清零
                var accessToken = rest.AdminLogin(adminInfo.UserName, isAutoLogin);
                var expiresAt = DateTime.Now.AddDays(Constants.AccessTokenExpireDays);

                return Ok(new
                {
                    Value = adminInfo,
                    AccessToken = accessToken,
                    ExpiresAt = expiresAt
                });
            }
            catch (Exception ex)
            {
                LogUtils.AddErrorLog(ex);
                return InternalServerError(ex);
            }
        }
    }
}