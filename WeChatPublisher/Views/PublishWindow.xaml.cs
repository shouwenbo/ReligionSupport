using System.Windows;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class PublishWindow : Window
{
    public PublishWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshAll();
    }

    private void RefreshAll()
    {
        try
        {
            var drafts = AppSettings.Instance.Db.ExecuteInScope(db =>
                db.Queryable<ArticleDraft>().OrderBy(d => d.Id, SqlSugar.OrderByType.Desc).ToList());
            DgDrafts.ItemsSource = drafts;

            var history = AppSettings.Instance.Db.ExecuteInScope(db =>
                db.Queryable<PublishRecord>().OrderBy(p => p.Id, SqlSugar.OrderByType.Desc).Take(50).ToList());
            DgHistory.ItemsSource = history;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e) => RefreshAll();

    private void DgDrafts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DgDrafts.SelectedItem is ArticleDraft draft)
        {
            TbPreview.Text = draft.Content ?? "(无内容)";
        }
    }

    private async void BtnPublishDraft_Click(object sender, RoutedEventArgs e)
    {
        if (DgDrafts.SelectedItem is not ArticleDraft draft)
        {
            MessageBox.Show("请先选择一个草稿", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var wechatService = new WeChatService();
            var mediaId = await wechatService.CreateDraftAsync(draft);

            AppSettings.Instance.Db.ExecuteInScope(db =>
            {
                draft.MediaId = mediaId;
                draft.Status = "published_draft";
                draft.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                db.Updateable(draft).ExecuteCommand();

                db.Insertable(new PublishRecord
                {
                    PublishType = "article_draft",
                    ArticleDraftId = draft.Id,
                    Title = draft.Title,
                    PublishId = mediaId,
                    Status = "success",
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }).ExecuteCommand();
            });

            MessageBox.Show($"已发布到公众号草稿箱!\nMediaId: {mediaId}", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"发布失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

            AppSettings.Instance.Db.ExecuteInScope(db =>
            {
                db.Insertable(new PublishRecord
                {
                    PublishType = "article_draft",
                    ArticleDraftId = draft.Id,
                    Title = draft.Title,
                    Status = "failed",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }).ExecuteCommand();
            });
        }
    }

    private async void BtnPublishNow_Click(object sender, RoutedEventArgs e)
    {
        if (DgDrafts.SelectedItem is not ArticleDraft draft)
        {
            MessageBox.Show("请先选择一个草稿", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(draft.MediaId))
        {
            MessageBox.Show("请先发布为草稿，再进行群发", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var wechatService = new WeChatService();
            var publishId = await wechatService.PublishAsync(draft.MediaId);

            AppSettings.Instance.Db.ExecuteInScope(db =>
            {
                draft.PublishId = publishId;
                draft.Status = "published";
                draft.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                db.Updateable(draft).ExecuteCommand();

                db.Insertable(new PublishRecord
                {
                    PublishType = "article_publish",
                    ArticleDraftId = draft.Id,
                    Title = draft.Title,
                    PublishId = publishId,
                    Status = "success",
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }).ExecuteCommand();
            });

            MessageBox.Show($"已群发!\nPublishId: {publishId}", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"群发失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgDrafts.SelectedItem is not ArticleDraft draft) return;

        if (MessageBox.Show($"确定删除草稿 '{draft.Title}'?", "确认",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            AppSettings.Instance.Db.ExecuteInScope(db =>
                db.Deleteable<ArticleDraft>().In(draft.Id).ExecuteCommand());
            RefreshAll();
        }
    }

    private void BtnSaveLocal_Click(object sender, RoutedEventArgs e)
    {
        if (DgDrafts.SelectedItem is not ArticleDraft draft) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Word文档|*.docx|文本文件|*.txt",
            FileName = $"{draft.Title ?? "文章"}.docx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                if (dialog.FileName.EndsWith(".docx"))
                {
                    var templatePath = AppSettings.Instance.ArticleTemplatePath;
                    if (File.Exists(templatePath))
                    {
                        DocxTemplateService.FillTemplate(templatePath, dialog.FileName,
                            new Dictionary<string, string>
                            {
                                ["{{标题}}"] = draft.Title ?? "",
                                ["{{简介}}"] = draft.Description ?? "",
                                ["{{章节}}"] = draft.Verse ?? "",
                                ["{{经文}}"] = "",
                                ["{{内容}}"] = draft.Content ?? "",
                                ["{{标签1}}"] = draft.Tags?.Split(' ').FirstOrDefault() ?? "",
                                ["{{标签2}}"] = draft.Tags?.Split(' ').Skip(1).FirstOrDefault() ?? "",
                                ["{{标签3}}"] = draft.Tags?.Split(' ').Skip(2).FirstOrDefault() ?? "",
                                ["{{标签4}}"] = draft.Tags?.Split(' ').Skip(3).FirstOrDefault() ?? "",
                                ["{{标签5}}"] = draft.Tags?.Split(' ').Skip(4).FirstOrDefault() ?? "",
                            });
                    }
                    else
                    {
                        File.WriteAllText(dialog.FileName, draft.Content ?? "");
                    }
                }
                else
                {
                    File.WriteAllText(dialog.FileName, draft.Content ?? "");
                }

                MessageBox.Show("保存成功", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
