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
        SeedAiConfigs(secrets);
        SeedTtsConfigs();
        SeedSubtitleConfigs();
        SeedMcpResources();
        SeedSensitiveWords();
        SeedWeChatConfig();
        SeedPromptTemplates();
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
                    ? ConfigEncryptionService.Encrypt(secrets.DeepSeekKey) : null,
                ModelName = "deepseek-chat",
                DefaultMaxTokens = 4096,
                DefaultTemperature = 0.7,
                IsActive = 1,
                CreatedAt = now,
                UpdatedAt = now
            }).ExecuteCommand());
        }

        if (!existing.Any(c => c.ProviderType == "ImageGeneration"))
        {
            // 主: 混元
            _dbService.ExecuteInScope(db => db.Insertable(new AiConfig
            {
                ProviderType = "ImageGeneration",
                ProviderName = "HunyuanImage",
                BaseUrl = "https://api.hunyuan.cloud.tencent.com/v1",
                ApiKeyEncrypted = secrets.HunyuanKey != null
                    ? ConfigEncryptionService.Encrypt(secrets.HunyuanKey) : null,
                ModelName = "hunyuan-image-3.0-instruct",
                IsActive = 1,
                CreatedAt = now, UpdatedAt = now
            }).ExecuteCommand());

            // 备选: TokenHub
            if (secrets.TokenHubKey != null)
            {
                _dbService.ExecuteInScope(db => db.Insertable(new AiConfig
                {
                    ProviderType = "ImageGeneration",
                    ProviderName = "TokenHub",
                    BaseUrl = "https://tokenhub.tencentmaas.com/v1",
                    ApiKeyEncrypted = ConfigEncryptionService.Encrypt(secrets.TokenHubKey),
                    ModelName = "hy3-preview",
                    IsActive = 0,
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
        if (existing.Count > 0) return;

        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var defaults = new List<McpResourceConfig>
        {
            new() { Name = "公众号文案", ResourceType = "LocalFolder",
                     Path = GetJsonValue("Defaults:OutputArticleRoot"),
                     FileFilter = "*.docx", Description = "公众号文案输出目录",
                     CreatedAt = now, UpdatedAt = now },
            new() { Name = "读经感悟", ResourceType = "LocalFolder",
                     Path = @"F:\个人 & 文档\读经感悟",
                     FileFilter = "*.txt", Description = "读经感悟笔记",
                     CreatedAt = now, UpdatedAt = now },
            new() { Name = "插图素材", ResourceType = "LocalFolder",
                     Path = @"F:\传道 & 美图\插图素材",
                     FileFilter = "*.jpg;*.jpeg;*.png", Description = "文章配图素材库",
                     CreatedAt = now, UpdatedAt = now },
            new() { Name = "视频号输出", ResourceType = "LocalFolder",
                     Path = GetJsonValue("Defaults:OutputVideoRoot"),
                     FileFilter = "*.mp4", Description = "短视频输出目录",
                     CreatedAt = now, UpdatedAt = now },
        };

        _dbService.ExecuteInScope(db => db.Insertable(defaults).ExecuteCommand());
    }

    // ========== 敏感词 ==========
    private void SeedSensitiveWords()
    {
        var existing = _dbService!.ExecuteInScope(db => db.Queryable<SensitiveWord>().ToList());
        if (existing.Count > 0) return;

        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var defaults = new List<SensitiveWord>
        {
            new() { SourceWord = "神",     ReplacementWords = "父亲;阿爸;源头",       Category = "称谓", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "上帝",   ReplacementWords = "父亲;源头",            Category = "称谓", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "耶稣",   ReplacementWords = "老师;TA;榜样",          Category = "人名", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "基督",   ReplacementWords = "老师;TA",              Category = "称谓", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "圣灵",   ReplacementWords = "内心引导;内在力量",     Category = "概念", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "主",     ReplacementWords = "TA;父亲",              Category = "称谓", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "基督教", ReplacementWords = "信仰团体",              Category = "组织", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "教堂",   ReplacementWords = "聚会场所;家园",         Category = "场所", StrictLevel = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "祷告",   ReplacementWords = "默想;交流;倾诉",        Category = "行为", StrictLevel = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "圣经",   ReplacementWords = "智慧书;经典",           Category = "物品", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "福音",   ReplacementWords = "好消息;佳音",           Category = "概念", StrictLevel = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "救赎",   ReplacementWords = "更新;改变",             Category = "概念", StrictLevel = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "恩典",   ReplacementWords = "礼物;馈赠",             Category = "概念", StrictLevel = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "阿门",   ReplacementWords = "诚心所愿;真心祝愿",     Category = "用语", StrictLevel = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "哈利路亚", ReplacementWords = "赞美;感恩",           Category = "用语", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "弥赛亚", ReplacementWords = "老师;TA",              Category = "概念", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "救主",   ReplacementWords = "老师;TA",              Category = "称谓", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "敬拜",   ReplacementWords = "赞美;表达敬意",         Category = "行为", StrictLevel = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "团契",   ReplacementWords = "聚会;小组",             Category = "活动", StrictLevel = 1, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
            new() { SourceWord = "传道",   ReplacementWords = "分享;讲述",             Category = "行为", StrictLevel = 0, IsEnabled = 1, CreatedAt = now, UpdatedAt = now },
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
            AppId = "",
            ApiBaseUrl = "https://api.weixin.qq.com",
            PublishAsDraft = 1,
            AutoSanitize = 1,
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
            Name = "公众号文章-标准版",
            Category = "Article",
            SystemPrompt = "你是一位公众号文案创作者，擅长用温暖、柔和、真诚的文字打动人心。本公众号主题为'爱与祝福同行'。",
            UserPromptTemplate = @"【规则与要求】
1. 必须按以下格式输出：标题、简介（1-2句话）、经文（放在最前，经文要与正文呼应）、正文内容、5个两字标签
2. 正文不使用粗体小标题，段落之间自然过渡，用内容本身的逻辑衔接
3. 深度学习原文，理解核心思想，重新拟定文章顺序和结构，保留核心思想
4. 避免批判性语气
5. 内容如果深奥，适当用有趣的表达和例子辅佐，层层递进吸引读者读完
6. 篇幅太短可根据内容适当扩展和补充
7. 非中文语言翻译为简体中文

【原文】
{source_title}
{source_verse}
{source_content}",
            Description = "公众号文章生成标准模板",
            SortOrder = 1,
            CreatedAt = now, UpdatedAt = now
        };
        _dbService.ExecuteInScope(db => db.Insertable(articlePrompt).ExecuteCommand());

        var videoPrompt = new PromptTemplate
        {
            Name = "视频号-天父书信",
            Category = "Video",
            SystemPrompt = "你是一位短视频文案创作者，擅长以天父的视角撰写温暖的书信。",
            UserPromptTemplate = @"请用以下【原文】编写天父书信：
1. 开头必须是""亲爱的孩子""
2. 以天父/父亲为第一人称视角改写
3. 一行一行输出，每行以句号结尾
4. 控制在50-250字
5. 简体中文输出
6. 深度理解原文，层层递进的情绪，引起共鸣
7. 写完后提供5组爆款标题和简介，标题格式为【6字+空格+6字】或【7字+空格+7字】或【8字+空格+8字】
8. 提供30个与书信内容高度匹配的经文，格式如【太1:1（大致内容）】，避免过于常见经文
9. 最后生成10个两字标签，以 #xx #xx #xx 格式输出

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
