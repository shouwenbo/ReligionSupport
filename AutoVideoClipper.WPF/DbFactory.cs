using SqlSugar;
using System.IO;

namespace AutoClipper
{
    public static class DbContext
    {

        /// <summary>
        /// 获取数据库连接
        /// </summary>
        /// <returns>数据库连接</returns>
        public static SqlSugarClient GetDbClient()
        {
            var config = new ConnectionConfig()
            {
                DbType = DbType.Sqlite,
                ConnectionString = "Data source=bible_简体中文和合本.db",
                IsAutoCloseConnection = true,
            };
            return new SqlSugarClient(config);
        }
        public static void InitModels()
        {
            var path = Path.Combine(AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf(@"\BibleVerseLookupApp\") + 21), "Models");
            using (SqlSugarClient db = GetDbClient())
            {
                db.DbFirst.CreateClassFile(path, "BibleVerseLookupApp.Models");
            }
        }
    }
}
