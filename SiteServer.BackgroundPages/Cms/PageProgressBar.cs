using System;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.BackgroundPages.Ajax;
using SiteServer.BackgroundPages.Core;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Create;
using SiteServer.Plugin;

namespace SiteServer.BackgroundPages.Cms
{
    public class PageProgressBar : BasePageCms
    {
        public Literal LtlTitle;
        public Literal LtlRegisterScripts;

        protected override bool IsSinglePage => true;

        public static readonly string CookieChannelIdCollection = "SiteServer.BackgroundPages.Cms.PageProgressBar.ChannelIdCollection ";

        public static readonly string CookieContentIdentityCollection = "SiteServer.BackgroundPages.Cms.PageProgressBar.ContentIdentityCollection";

        public static readonly string CookieTemplateIdCollection = "SiteServer.BackgroundPages.Cms.PageProgressBar.TemplateIdCollection";

        public static string GetCreateSiteUrl(int siteId, bool isImportContents, bool isImportTableStyles, string siteTemplateDir, string onlineTemplateName, bool isUseTables, string userKeyPrefix)
        {
            return PageUtils.GetCmsUrl(siteId, nameof(PageProgressBar), new NameValueCollection
            {
                {"createSite", true.ToString()},
                {"isImportContents", isImportContents.ToString()},
                {"isImportTableStyles", isImportTableStyles.ToString()},
                {"siteTemplateDir", siteTemplateDir},
                {"onlineTemplateName", onlineTemplateName},
                {"isUseTables", isUseTables.ToString()},
                {"userKeyPrefix", userKeyPrefix}
            });
        }

        public static string GetBackupUrl(int siteId, string backupType, string userKeyPrefix)
        {
            return PageUtils.GetCmsUrl(siteId, nameof(PageProgressBar), new NameValueCollection
            {
                {"backup", true.ToString()},
                {"backupType", backupType},
                {"userKeyPrefix", userKeyPrefix}
            });
        }

        public static string GetRecoveryUrl(int siteId, string isDeleteChannels, string isDeleteTemplates, string isDeleteFiles, bool isZip, string path, string isOverride, string isUseTable, string userKeyPrefix)
        {
            return PageUtils.GetCmsUrl(siteId, nameof(PageProgressBar), new NameValueCollection
            {
                {"recovery", true.ToString()},
                {"isDeleteChannels", isDeleteChannels},
                {"isDeleteTemplates", isDeleteTemplates},
                {"isDeleteFiles", isDeleteFiles},
                {"isZip", isZip.ToString()},
                {"path", path},
                {"isOverride", isOverride},
                {"isUseTable", isUseTable},
                {"userKeyPrefix", userKeyPrefix}
            });
        }

        public static string GetDeleteAllPageUrl(int siteId, TemplateType templateType)
        {
            return PageUtils.GetCmsUrl(siteId, nameof(PageProgressBar), new NameValueCollection
            {
                {"templateType", templateType.Value},
                {"deleteAllPage", true.ToString()}
            });
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            PageUtils.CheckRequestParameter("siteId");

            var userKeyPrefix = AuthRequest.GetQueryString("userKeyPrefix");

            if (AuthRequest.IsQueryExists("createSite"))
            {
                LtlTitle.Text = "新建站点";
                var pars = AjaxCreateService.GetCreateSiteParameters(SiteId, AuthRequest.GetQueryBool("isImportContents"), AuthRequest.GetQueryBool("isImportTableStyles"), AuthRequest.GetQueryString("siteTemplateDir"), AuthRequest.GetQueryString("onlineTemplateName"), AuthRequest.GetQueryBool("isUseTables"), userKeyPrefix);
                LtlRegisterScripts.Text = AjaxManager.RegisterProgressTaskScript(AjaxCreateService.GetCreateSiteUrl(), pars, userKeyPrefix, AjaxCreateService.GetCountArrayUrl(), true);
            }
            else if (AuthRequest.IsQueryExists("backup") && AuthRequest.IsQueryExists("backupType"))
            {
                LtlTitle.Text = "数据备份";

                var parameters =
                    AjaxBackupService.GetBackupParameters(SiteId, AuthRequest.GetQueryString("backupType"), userKeyPrefix);
                LtlRegisterScripts.Text = AjaxManager.RegisterWaitingTaskScript(AjaxBackupService.GetBackupUrl(), parameters);
            }
            else if (AuthRequest.IsQueryExists("recovery") && AuthRequest.IsQueryExists("isZip"))
            {
                LtlTitle.Text = "数据恢复";
                var parameters = AjaxBackupService.GetRecoveryParameters(SiteId,
                    AuthRequest.GetQueryBool("isDeleteChannels"), AuthRequest.GetQueryBool("isDeleteTemplates"),
                    AuthRequest.GetQueryBool("isDeleteFiles"), AuthRequest.GetQueryBool("isZip"),
                    PageUtils.UrlEncode(AuthRequest.GetQueryString("path")), AuthRequest.GetQueryBool("isOverride"),
                    AuthRequest.GetQueryBool("isUseTable"), userKeyPrefix);
                LtlRegisterScripts.Text = AjaxManager.RegisterWaitingTaskScript(AjaxBackupService.GetRecoveryUrl(), parameters);
            }
            else if (AuthRequest.IsQueryExists("deleteAllPage") && AuthRequest.IsQueryExists("templateType"))
            {
                DeleteAllPage();
            }
            else if (AuthRequest.IsQueryExists("createIndex"))
            {
                CreateIndex();
            }
        }

        //生成首页
        private void CreateIndex()
        {
            LtlTitle.Text = "生成首页";
            var link = new HyperLink
            {
                NavigateUrl = PageUtility.GetIndexPageUrl(SiteInfo, false),
                Text = "浏览"
            };
            if (link.NavigateUrl != PageUtils.UnclickedUrl)
            {
                link.Target = "_blank";
            }
            link.Style.Add("text-decoration", "underline");
            try
            {
                CreateManager.CreateChannel(SiteId, SiteId);
                //FileSystemObject FSO = new FileSystemObject(base.SiteId);

                //FSO.AddIndexToWaitingCreate();

                LtlRegisterScripts.Text = @"
<script>
$(document).ready(function(){
    writeResult('首页生成成功。', '');
})
</script>
";
            }
            catch (Exception ex)
            {
                LtlRegisterScripts.Text = $@"
<script>
$(document).ready(function(){{
    writeResult('', '{ex.Message}');
}})
</script>
";
            }
        }

        private void DeleteAllPage()
        {
            var templateType = TemplateTypeUtils.GetEnumType(AuthRequest.GetQueryString("TemplateType"));

            if (templateType == TemplateType.ChannelTemplate)
            {
                LtlTitle.Text = "删除已生成的栏目页文件";
            }
            else if (templateType == TemplateType.ContentTemplate)
            {
                LtlTitle.Text = "删除所有已生成的内容页文件";
            }
            else if (templateType == TemplateType.FileTemplate)
            {
                LtlTitle.Text = "删除所有已生成的文件页";
            }

            try
            {
                if (templateType == TemplateType.ChannelTemplate)
                {
                    var channelIdList = ChannelManager.GetChannelIdList(SiteId);
                    DirectoryUtility.DeleteChannelsByPage(SiteInfo, channelIdList);
                }
                else if (templateType == TemplateType.ContentTemplate)
                {
                    var channelIdList = ChannelManager.GetChannelIdList(SiteId);
                    DirectoryUtility.DeleteContentsByPage(SiteInfo, channelIdList);
                }
                else if (templateType == TemplateType.FileTemplate)
                {
                    DirectoryUtility.DeleteFiles(SiteInfo, DataProvider.TemplateDao.GetIdListByType(SiteId, TemplateType.FileTemplate));
                }

                AuthRequest.AddSiteLog(SiteId, LtlTitle.Text);

                LtlRegisterScripts.Text = @"
<script>
$(document).ready(function(){
    writeResult('任务执行成功。', '');
})
</script>
";
            }
            catch (Exception ex)
            {
                LtlRegisterScripts.Text = $@"
<script>
$(document).ready(function(){{
    writeResult('', '{ex.Message}');
}})
</script>
";
            }
        }
    }
}
