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

    // --- WeChat Config ---
    public WeChatConfig? GetActiveWeChatConfig()
    {
        return Db.ExecuteInScope(db =>
            db.Queryable<WeChatConfig>()
                .First(x => x.IsActive == 1));
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
