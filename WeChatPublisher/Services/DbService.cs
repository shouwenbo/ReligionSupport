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
    }
}
