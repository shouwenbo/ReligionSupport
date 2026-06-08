using System.IO;
using Microsoft.Extensions.Configuration;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher;

public class AppSettings
{
    private static readonly Lazy<AppSettings> _instance = new(() => new AppSettings());
    public static AppSettings Instance => _instance.Value;

    private readonly IConfiguration _jsonConfig;
    private DbService? _dbService;

    public DbService Db => _dbService ?? throw new InvalidOperationException("AppSettings not initialized");

    private AppSettings()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        _jsonConfig = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("Resources/appsettings.json", optional: false)
            .Build();
    }

    public void Initialize()
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GetJsonValue("Database:AppDbPath"));
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        _dbService = new DbService($"Data Source={dbPath}");
        _dbService.EnsureTablesCreated();
        SeedAllDefaults();
    }

    private void SeedAllDefaults()
    {
        var secrets = LoadSecrets();
        Services.Logger.Info("开始种子数据...");
        SeedAiConfigs(secrets);
        SeedTtsConfigs();
        SeedSubtitleConfigs();
        SeedMcpResources();
        SeedSensitiveWords();
        SeedWeChatConfig();
        SeedPromptTemplates();
        Services.Logger.Info("种子数据完成");
    }

    // ========== AI 配置 ==========
    private void SeedAiConfigs((string? DeepSeekKey, string? HunyuanKey, string? TokenHubKey) secrets)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var existing = _dbService!.ExecuteInScope(db => db.Queryable<AiConfig>().ToList());

        if (!existing.Any(c => c.ProviderType == "TextGeneration"))
        {
            _dbService.ExecuteInScope(db => db.Insertable(new AiConfig
            {
                ProviderType = "TextGeneration",
                ProviderName = "DeepSeek",
                BaseUrl = "https://api.deepseek.com",
                ApiKeyEncrypted = secrets.DeepSeekKey != null
                    ? ConfigEncryptionService.Encrypt(secrets.DeepSeekKey) : "",
                ModelName = "deepseek-chat",
                SystemPrompt = "", ExtraHeaders = "",
                DefaultMaxTokens = 4096,
                DefaultTemperature = 0.7,
                IsActive = 1, IsHealthy = 0,
                CreatedAt = now, UpdatedAt = now
            }).ExecuteCommand());
        }

        if (!existing.Any(c => c.ProviderType == "ImageGeneration"))
        {
            // 主: TokenHub (已验证API格式)
            _dbService.ExecuteInScope(db => db.Insertable(new AiConfig
            {
                ProviderType = "ImageGeneration",
                ProviderName = "TokenHub",
                BaseUrl = "https://tokenhub.tencentmaas.com/v1",
                ApiKeyEncrypted = secrets.TokenHubKey != null
                    ? ConfigEncryptionService.Encrypt(secrets.TokenHubKey) : "",
                ModelName = "hy-image-v3.0",
                SystemPrompt = "", ExtraHeaders = "",
                DefaultMaxTokens = 0,
                DefaultTemperature = 0,
                IsActive = 1, IsHealthy = 0,
                CreatedAt = now, UpdatedAt = now
            }).ExecuteCommand());

            // 备选: 混元
            if (secrets.HunyuanKey != null)
            {
                _dbService.ExecuteInScope(db => db.Insertable(new AiConfig
                {
                    ProviderType = "ImageGeneration",
                    ProviderName = "HunyuanImage",
                    BaseUrl = "https://api.hunyuan.cloud.tencent.com/v1",
                    ApiKeyEncrypted = ConfigEncryptionService.Encrypt(secrets.HunyuanKey),
                    ModelName = "hunyuan-image-3.0-instruct",
                    SystemPrompt = "", ExtraHeaders = "",
                    DefaultMaxTokens = 0, DefaultTemperature = 0,
                    IsActive = 0, IsHealthy = 0,
                    CreatedAt = now, UpdatedAt = now
                }).ExecuteCommand());
            }
        }
    }

    // ========== TTS 配置 (text-to-speech.cn) ==========
    private void SeedTtsConfigs()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var existing = _dbService!.ExecuteInScope(db =>
            db.Queryable<TtsApiConfig>().Where(c => c.ConfigType == "TTS").ToList());

        if (existing.Count > 0) return;

        var defaultParams = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["language"] = "中文（普通话，简体）",
            ["voice"] = "zh-CN-YunzeNeural",
            ["role"] = "OlderAdultMale",
            ["style"] = "calm",
            ["styledegree"] = "2",
            ["rate"] = "-26",
            ["pitch"] = "-10",
            ["kbitrate"] = "audio-48khz-192kbitrate-mono-mp3",
            ["silence"] = "500ms",
            ["volume"] = "x-loud"
        });

        _dbService.ExecuteInScope(db => db.Insertable(new TtsApiConfig
        {
            ConfigType = "TTS",
            ApiType = "WebScrape",
            BaseUrl = "https://www.text-to-speech.cn",
            TokenFetchUrl = "/",
            TokenRegexPattern = "const token = '([^']+)'",
            GenerateEndpoint = "/getSpeek.php",
            SuccessCodeField = "code",
            SuccessCodeValue = "200",
            DefaultParamsJSON = defaultParams,
            IsActive = 1,
            CreatedAt = now,
            UpdatedAt = now
        }).ExecuteCommand());
    }

    // ========== 字幕配置 (text-to-speech.cn) ==========
    private void SeedSubtitleConfigs()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var existing = _dbService!.ExecuteInScope(db =>
            db.Queryable<TtsApiConfig>().Where(c => c.ConfigType == "Subtitle").ToList());

        if (existing.Count > 0) return;

        _dbService.ExecuteInScope(db => db.Insertable(new TtsApiConfig
        {
            ConfigType = "Subtitle",
            ApiType = "WebScrape",
            BaseUrl = "https://www.text-to-speech.cn",
            TokenFetchUrl = "/srt.html",
            TokenRegexPattern = "const token = '([^']+)'",
            GenerateEndpoint = "/getSrt.php",
            SuccessCodeField = "code",
            SuccessCodeValue = "200",
            DefaultParamsJSON = "{\"language\":\"zh-CN\"}",
            IsActive = 1,
            CreatedAt = now,
            UpdatedAt = now
        }).ExecuteCommand());
    }

    // ========== MCP 资源 ==========
    private void SeedMcpResources()
    {
        var existing = _dbService!.ExecuteInScope(db => db.Queryable<McpResourceConfig>().ToList());
        var existingNames = existing.Select(r => r.Name).ToHashSet();
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var defaults = new List<McpResourceConfig>
        {
            // 文章素材 - 优先扫描公众号文案目录
            new() { Name = "公众号文案库", ResourceType = "LocalFolder",
                     Path = @"F:\传道 & 公众号文案",
                     FileFilter = "*.docx;*.txt", Description = "公众号文案主素材库（信仰灵修类）" },
            new() { Name = "F盘-全部文档(后备)", ResourceType = "LocalFolder",
                     Path = @"F:\", FileFilter = "*.docx;*.txt;*.md",
                     Description = "后备素材源（含日记等杂项，AI会识别过滤）" },
            new() { Name = "视频号输出", ResourceType = "LocalFolder",
                     Path = GetJsonValue("Defaults:OutputVideoRoot"),
                     FileFilter = "*.mp4;*.mov", Description = "短视频输出目录" },
        };

        var toAdd = defaults.Where(d => !existingNames.Contains(d.Name)).ToList();
        if (toAdd.Count == 0) return;

        foreach (var r in toAdd) { r.CreatedAt = now; r.UpdatedAt = now; }
        _dbService.ExecuteInScope(db => db.Insertable(toAdd).ExecuteCommand());
        Services.Logger.Info($"MCP种子数据: 新增 {toAdd.Count} 个默认资源");
    }

    // ========== 敏感词 ==========
    private void SeedSensitiveWords()
    {
        var existing = _dbService!.ExecuteInScope(db => db.Queryable<SensitiveWord>().ToList());
        if (existing.Count > 0) return;

        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var defaults = new List<SensitiveWord>
        {
            new() { SourceWord = "神",     ReplacementWords = "父亲;阿爸;源头",       Category = "称谓", StrictLevel = 1, Strategy = 2, IsEnabled = 1, RewriteExamples = "感谢神的恩典 → 感谢生命中的馈赠\n神爱世人 → 有一种爱超越一切", CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "耶稣",   ReplacementWords = "老师;TA;榜样",          Category = "人名", StrictLevel = 1, Strategy = 2, IsEnabled = 1, RewriteExamples = "耶稣教导我们 → 那位智者曾经说过\n跟随耶稣 → 追寻光的方向", CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "圣灵",   ReplacementWords = "内心引导;内在力量",     Category = "概念", StrictLevel = 1, Strategy = 2, IsEnabled = 1, RewriteExamples = "圣灵感动 → 内心深处有个声音\n圣灵充满 → 被温暖的力量包围", CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "主",     ReplacementWords = "TA;父亲",              Category = "称谓", StrictLevel = 1, Strategy = 2, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "基督教", ReplacementWords = "信仰团体",              Category = "组织", StrictLevel = 1, Strategy = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "教堂",   ReplacementWords = "聚会场所;家园",         Category = "场所", StrictLevel = 0, Strategy = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "祷告",   ReplacementWords = "默想;交流;倾诉",        Category = "行为", StrictLevel = 0, Strategy = 2, IsEnabled = 1, RewriteExamples = "她在祷告中找到了平安 → 她在静默中找到了平安\n让我们一起祷告 → 让我们一起安静片刻", CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "圣经",   ReplacementWords = "智慧书;经典",           Category = "物品", StrictLevel = 1, Strategy = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "福音",   ReplacementWords = "好消息;佳音",           Category = "概念", StrictLevel = 0, Strategy = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "救赎",   ReplacementWords = "更新;改变",             Category = "概念", StrictLevel = 0, Strategy = 2, IsEnabled = 1, RewriteExamples = "基督的救赎 → 那份无条件的爱带来的改变\n被救赎的生命 → 被更新的生命", CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "恩典",   ReplacementWords = "礼物;馈赠",             Category = "概念", StrictLevel = 0, Strategy = 2, IsEnabled = 1, RewriteExamples = "活在恩典中 → 活在感恩中\n神的恩典够用 → 生命中的馈赠总是够用", CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "阿门",   ReplacementWords = "诚心所愿;真心祝愿",     Category = "用语", StrictLevel = 0, Strategy = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "哈利路亚", ReplacementWords = "赞美;感恩",           Category = "用语", StrictLevel = 1, Strategy = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "弥赛亚", ReplacementWords = "老师;TA",              Category = "概念", StrictLevel = 1, Strategy = 2, IsEnabled = 1, RewriteExamples = "弥赛亚降临 → 那位应许者降临\n等候弥赛亚 → 等候那一位", CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "救主",   ReplacementWords = "老师;TA",              Category = "称谓", StrictLevel = 1, Strategy = 2, IsEnabled = 1, RewriteExamples = "救主耶稣基督 → 那位带来希望的老师", CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "敬拜",   ReplacementWords = "赞美;表达敬意",         Category = "行为", StrictLevel = 0, Strategy = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "团契",   ReplacementWords = "聚会;小组",             Category = "活动", StrictLevel = 1, Strategy = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "传道",   ReplacementWords = "分享;讲述",             Category = "行为", StrictLevel = 0, Strategy = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
        };
        _dbService.ExecuteInScope(db => db.Insertable(defaults).ExecuteCommand());
    }

    // ========== 公众号凭证空配置 ==========
    private void SeedWeChatConfig()
    {
        var existing = _dbService!.ExecuteInScope(db => db.Queryable<WeChatConfig>().ToList());
        if (existing.Count > 0) return;

        _dbService.ExecuteInScope(db => db.Insertable(new WeChatConfig
        {
            AccountName = "爱与祝福同行",
            AppId = "",
            ApiBaseUrl = "https://api.weixin.qq.com",
            PublishAsDraft = 1,
            AutoSanitize = 1,
            ContactImage = @"F:\传道 & 公众号文案\扫码关注设计\爱与祝福同行二维码设计 - 我每天喜乐.png",
            IsActive = 1,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        }).ExecuteCommand());
    }

    // ========== 读取本地密钥 ==========
    private static (string? DeepSeekKey, string? HunyuanKey, string? TokenHubKey) LoadSecrets()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "secrets.json");
            if (!File.Exists(path)) return (null, null, null);

            var json = File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var deepseek = root.TryGetProperty("DeepSeekApiKey", out var dk) ? dk.GetString() : null;
            var hunyuan = root.TryGetProperty("HunyuanImageApiKey", out var hk) ? hk.GetString() : null;
            var tokenhub = root.TryGetProperty("TokenHubApiKey", out var tk) ? tk.GetString() : null;
            return (deepseek, hunyuan, tokenhub);
        }
        catch
        {
            return (null, null, null);
        }
    }

    public string GetJsonValue(string key)
    {
        return _jsonConfig[key] ?? "";
    }

    public string FfmpegPath => GetFullPath(GetJsonValue("FFmpeg:FfmpegExePath"));
    public string FfprobePath => GetFullPath(GetJsonValue("FFmpeg:FfprobeExePath"));
    public string DefaultOutputVideoRoot => GetJsonValue("Defaults:OutputVideoRoot");
    public string DefaultOutputArticleRoot => GetJsonValue("Defaults:OutputArticleRoot");
    public string ArticleTemplatePath => GetFullPath(GetJsonValue("Defaults:ArticleTemplatePath"));
    public double DefaultTemperature => double.TryParse(GetJsonValue("Defaults:Temperature"), out var t) ? t : 0.7;
    public int DefaultMaxAgentRounds => int.TryParse(GetJsonValue("Defaults:MaxAgentRounds"), out var r) ? r : 3;
    public string DefaultArticleStyle => GetJsonValue("Defaults:DefaultArticleStyle");
    public int VideoOutputWidth => int.TryParse(GetJsonValue("Defaults:VideoOutputWidth"), out var w) ? w : 1080;
    public int VideoOutputHeight => int.TryParse(GetJsonValue("Defaults:VideoOutputHeight"), out var h) ? h : 1440;
    public string BibleDbPath => GetFullPath(GetJsonValue("Database:BibleDbPath"));

    private static string GetFullPath(string relativePath)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
    }

    // --- AI Config ---
    public AiConfig? GetActiveTextAiConfig()
    {
        return Db.ExecuteInScope(db =>
            db.Queryable<AiConfig>()
                .First(x => x.ProviderType == "TextGeneration" && x.IsActive == 1));
    }

    public AiConfig? GetActiveImageAiConfig()
    {
        return Db.ExecuteInScope(db =>
            db.Queryable<AiConfig>()
                .First(x => x.ProviderType == "ImageGeneration" && x.IsActive == 1));
    }

    public List<AiConfig> GetAllAiConfigs()
    {
        return Db.ExecuteInScope(db => db.Queryable<AiConfig>().ToList());
    }

    public void SaveAiConfig(AiConfig config)
    {
        config.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Db.ExecuteInScope(db =>
        {
            if (config.Id == 0)
            {
                config.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                db.Insertable(config).ExecuteCommand();
            }
            else
            {
                db.Updateable(config).ExecuteCommand();
            }
        });
    }

    // --- TTS Config ---
    public TtsApiConfig? GetActiveTtsConfig()
    {
        return Db.ExecuteInScope(db =>
            db.Queryable<TtsApiConfig>()
                .First(x => x.ConfigType == "TTS" && x.IsActive == 1));
    }

    public TtsApiConfig? GetActiveSubtitleConfig()
    {
        return Db.ExecuteInScope(db =>
            db.Queryable<TtsApiConfig>()
                .First(x => x.ConfigType == "Subtitle" && x.IsActive == 1));
    }

    public List<TtsApiConfig> GetAllTtsConfigs()
    {
        return Db.ExecuteInScope(db => db.Queryable<TtsApiConfig>().ToList());
    }

    public void SaveTtsApiConfig(TtsApiConfig config)
    {
        config.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Db.ExecuteInScope(db =>
        {
            if (config.Id == 0)
            {
                config.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                db.Insertable(config).ExecuteCommand();
            }
            else
            {
                db.Updateable(config).ExecuteCommand();
            }
        });
    }

    // --- WeChat Config (多账号) ---
    public WeChatConfig? GetActiveWeChatConfig(string platform = "OfficialAccount")
    {
        return Db.ExecuteInScope(db =>
            db.Queryable<WeChatConfig>()
                .First(x => x.IsActive == 1 && x.Platform == platform));
    }

    public List<WeChatConfig> GetAllWeChatConfigs()
    {
        return Db.ExecuteInScope(db =>
            db.Queryable<WeChatConfig>().OrderBy(c => c.SortOrder, SqlSugar.OrderByType.Asc).ToList());
    }

    public void SaveWeChatConfig(WeChatConfig config)
    {
        config.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Db.ExecuteInScope(db =>
        {
            if (config.Id == 0)
            {
                config.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                db.Insertable(config).ExecuteCommand();
            }
            else
            {
                db.Updateable(config).ExecuteCommand();
            }
        });
    }

    public void DeleteWeChatConfig(int id)
    {
        Db.ExecuteInScope(db => db.Deleteable<WeChatConfig>().In(id).ExecuteCommand());
    }

    public void SetActiveWeChatAccount(int id)
    {
        Db.ExecuteInScope(db =>
        {
            var config = db.Queryable<WeChatConfig>().InSingle(id);
            if (config == null) return;

            // Deactivate all accounts of same platform
            db.Updateable<WeChatConfig>()
                .SetColumns(c => c.IsActive == 0)
                .Where(c => c.Platform == config.Platform)
                .ExecuteCommand();

            // Activate selected
            config.IsActive = 1;
            db.Updateable(config).ExecuteCommand();
        });
    }

    // --- MCP Resources ---
    public List<McpResourceConfig> GetMcpResources()
    {
        return Db.ExecuteInScope(db => db.Queryable<McpResourceConfig>().ToList());
    }

    public void SaveMcpResource(McpResourceConfig config)
    {
        config.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Db.ExecuteInScope(db =>
        {
            if (config.Id == 0)
            {
                config.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                db.Insertable(config).ExecuteCommand();
            }
            else
            {
                db.Updateable(config).ExecuteCommand();
            }
        });
    }

    public void DeleteMcpResource(int id)
    {
        Db.ExecuteInScope(db => db.Deleteable<McpResourceConfig>().In(id).ExecuteCommand());
    }

    // --- Prompt Templates ---
    public List<PromptTemplate> GetPromptTemplates(string? category = null)
    {
        return Db.ExecuteInScope(db =>
        {
            var q = db.Queryable<PromptTemplate>();
            if (category != null) q = q.Where(p => p.Category == category);
            return q.OrderBy(p => p.SortOrder, SqlSugar.OrderByType.Asc).ToList();
        });
    }

    public PromptTemplate? GetActivePromptTemplate(string category)
    {
        return Db.ExecuteInScope(db =>
            db.Queryable<PromptTemplate>().First(p => p.Category == category && p.IsActive == 1));
    }

    public void SavePromptTemplate(PromptTemplate template)
    {
        template.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Db.ExecuteInScope(db =>
        {
            if (template.Id == 0)
            {
                template.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                db.Insertable(template).ExecuteCommand();
            }
            else
            {
                db.Updateable(template).ExecuteCommand();
            }
        });
    }

    public void DeletePromptTemplate(int id)
    {
        Db.ExecuteInScope(db => db.Deleteable<PromptTemplate>().In(id).ExecuteCommand());
    }

    public void SetActivePromptTemplate(int id)
    {
        Db.ExecuteInScope(db =>
        {
            var t = db.Queryable<PromptTemplate>().InSingle(id);
            if (t == null) return;
            db.Updateable<PromptTemplate>()
                .SetColumns(p => p.IsActive == 0)
                .Where(p => p.Category == t.Category)
                .ExecuteCommand();
            t.IsActive = 1;
            db.Updateable(t).ExecuteCommand();
        });
    }

    private void SeedPromptTemplates()
    {
        var existing = _dbService!.ExecuteInScope(db => db.Queryable<PromptTemplate>().ToList());
        if (existing.Count > 0) return;

        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var articlePrompt = new PromptTemplate
        {
            Name = "爱与祝福同行-信仰灵修",
            Category = "Article",
            SystemPrompt = "你是一位信仰灵修类公众号的文案创作者，主题为'爱与祝福同行'。用温暖、柔和、真诚的文字打动人心，传递希望和安慰，让读者在平凡生活中感受到爱与光。文章整体要有灵修深度但不生硬，情感自然流露，像一位知心朋友在轻声分享。",
            UserPromptTemplate = @"【规则与要求】

1. 必须按以下格式输出：
    - 标题（吸引人的信仰灵修类标题）
    - 简介（1-2句话，简洁引人，点出文章灵修主题）
    - 经文（放在最前，经文要与正文核心呼应）
    - 正文内容（结构清晰，开头吸引，中间分段展开灵修感悟，结尾'愿...'收尾）
    - 5个两个字的标签，一共10个字，5个词（如：恩典 信心 盼望 感恩 生命）

2. 正文不使用粗体小标题，段落之间自然过渡，用内容本身的逻辑衔接，而不是用标题分割。

3. 原文如果是生活记录或日记形式，你需要在理解原文核心思想和情感后，将其升华为有灵修深度的文章。提炼出其中的信仰感悟、生命反思，而不是照搬日常流水账。

4. 原文可能来源于各种碎片化的记录，顺序不能完全按照原文来。你需要深度学习原文，理解核心思想，重新拟定文章的顺序和结构，保留核心思想的深度，剔除过于私人的日常琐事。

5. 避免过于批判性或说教性的语气。用分享、陪伴的姿态来写。

6. 内容如果深奥，可以用生活中的小例子来辅佐表达。重点在于层层递进，一段一段抓住读者的心，让读者有读完的渴望。

7. 如果文章篇幅太短，可以根据核心思想适当扩展和补充灵修感悟。

8. 务必用简体中文输出。

9. 文章的主题方向必须是信仰灵修类，包含但不限于：感恩、信心、盼望、爱、平安、生命的反思、日常中的恩典等。

【原文素材】
{source_title}
{source_verse}
{source_content}",
            Description = "公众号信仰灵修文章生成模板",
            SortOrder = 1,
            CreatedAt = now, UpdatedAt = now
        };
        _dbService.ExecuteInScope(db => db.Insertable(articlePrompt).ExecuteCommand());

        var videoPrompt = new PromptTemplate
        {
            Name = "视频号-天父书信",
            Category = "Video",
            SystemPrompt = "你是一位短视频文案创作者，擅长以父亲的口吻撰写温暖、感人、适合短视频配音的天父书信。",
            UserPromptTemplate = @"请用即将提供给你的【原文】，来编写天父书信。

要求如下：

1. 书信的开头必须是""亲爱的孩子""这5个字。
2. 书信的视角，要以天父或者父亲为第一人称视角，将所提供的【原文】改写为对孩子写的书信。
3. 一行一行进行输出，每一行都要以句号进行结尾。
4. 再次强调，一定要将【原文】的视角进行更换，要用天父或者父亲作为第一人称的写作手法进行写作。
5. 文字最好控制在200字以内，或者控制在50~250字。
6. 请务必用简体中文进行输出，如果【原文】不是简体中文，请首先翻译为简体中文。
7. 请深度理解我所提供的【原文】，内容一定要适合短视频创作，层层递进的情绪，容易引起共鸣。
8. 文章写完后，要为我提供5组超级爆款标题和简介。标题务必是【6字+空格+6字】或者【7字+空格+7字】或者【8字+空格+8字】这三个格式中的第一个，也就是[xxxxxx xxxxxx]格式。另外简介不要太短，这个简介会被我粘贴到公众号文章的简介里面去。标题和简介不要挤在一起，分行显示。
9. 请提供30个与这封天父书信内容高度匹配的经文，不要使用日常太常见太常用的经文（否则视频号经文重复率太高），以章节例如【太1:1（大致内容）】这种格式输出给我即可。
10. 最后帮我生成10个2个字标签，以【#xx #xx #xx #xx #xx】这样的格式输出给我。

【原文】
{source_content}",
            Description = "视频号天父书信生成模板",
            SortOrder = 2,
            CreatedAt = now, UpdatedAt = now
        };
        _dbService.ExecuteInScope(db => db.Insertable(videoPrompt).ExecuteCommand());
    }

    // --- Key-Value settings ---
    public string GetSetting(string key, string defaultValue = "")
    {
        return Db.ExecuteInScope(db =>
        {
            var row = db.Ado.GetDataTable($"SELECT Value FROM KeyValueSettings WHERE Key = '{key}'");
            return row.Rows.Count > 0 ? row.Rows[0]["Value"]?.ToString() ?? defaultValue : defaultValue;
        });
    }

    public void SetSetting(string key, string value)
    {
        Db.ExecuteInScope(db =>
        {
            db.Ado.ExecuteCommand(
                $"INSERT OR REPLACE INTO KeyValueSettings (Key, Value) VALUES ('{key}', '{value}')");
        });
    }
}
