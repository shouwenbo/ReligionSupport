using SqlSugar;
using WeChatPublisher.Models;

namespace WeChatPublisher.Services;

public class DbService
{
    private readonly string _connectionString;

    public DbService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlSugarClient CreateClient()
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = _connectionString,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }

    public void ExecuteInScope(Action<SqlSugarClient> action)
    {
        using var db = CreateClient();
        action(db);
    }

    public T ExecuteInScope<T>(Func<SqlSugarClient, T> func)
    {
        using var db = CreateClient();
        return func(db);
    }

    public void EnsureTablesCreated()
    {
        using var db = CreateClient();
        db.CodeFirst.InitTables(
            typeof(SensitiveWord),
            typeof(AiConfig),
            typeof(TtsApiConfig),
            typeof(WeChatConfig),
            typeof(McpResourceConfig),
            typeof(McpCacheEntry),
            typeof(PromptTemplate),
            typeof(AuthorPersona),
            typeof(EditSession),
            typeof(ArticleDraft),
            typeof(VideoDraft),
            typeof(PublishRecord),
            typeof(AgentTask),
            typeof(AgentStepLog)
        );
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS KeyValueSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT,
                ValueType TEXT DEFAULT 'string',
                Description TEXT
            )");
        MigrateSchema(db);
    }

    /// <summary>增量迁移: 为已存在的表添加新列, 不丢失数据</summary>
    private static void MigrateSchema(SqlSugarClient db)
    {
        // 2024-06: WeChatConfig 新增 ContactImage/CoverPrompt
        TryAddColumn(db, "WeChatConfigs", "ContactImage", "TEXT");
        TryAddColumn(db, "WeChatConfigs", "CoverPrompt", "TEXT");
        // 2024-06: AiConfig 新增 ImageSize/ImageCount/LogoAdd
        TryAddColumn(db, "AiConfigs", "ImageSize", "TEXT");
        TryAddColumn(db, "AiConfigs", "ImageCount", "INTEGER DEFAULT 1");
        TryAddColumn(db, "AiConfigs", "LogoAdd", "INTEGER DEFAULT 0");

        // 清除旧的DPAPI加密AppSecret（已改为明文存储）
        try
        {
            var count = db.Ado.ExecuteCommand(
                "UPDATE WeChatConfigs SET AppSecretEncrypted='' WHERE AppSecretEncrypted IS NOT NULL AND length(AppSecretEncrypted)>50");
            if (count > 0) Logger.Info($"DB迁移: 清除了 {count} 个旧的DPAPI加密AppSecret");
        }
        catch { }
    }

    private static void TryAddColumn(SqlSugarClient db, string table, string column, string type)
    {
        try
        {
            var exists = db.Ado.GetInt($"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{column}'") > 0;
            if (!exists)
            {
                db.Ado.ExecuteCommand($"ALTER TABLE [{table}] ADD COLUMN [{column}] {type}");
                Logger.Info($"DB迁移: {table}.{column} ({type}) 已添加");
            }
        }
        catch (Exception ex) { Logger.Warn($"DB迁移 {table}.{column} 跳过: {ex.Message}"); }
    }
}
