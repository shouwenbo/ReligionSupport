using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ImageGroupDownloader.WPF
{
    public static class HttpUtil
    {
        public static readonly HttpClient Client;

        static HttpUtil()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

                // ★ 让系统自动选择 TLS 版本（TLS1.2 / TLS1.3）
                SslProtocols = System.Security.Authentication.SslProtocols.None,

                // ★ 允许自动重定向
                AllowAutoRedirect = true,

                // ★ 关闭自动证书验证（按需开启）
                ServerCertificateCustomValidationCallback = ValidateServerCertificate
            };

            Client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(20) // ★ 防止卡死
            };

            // 默认 UA（某些服务器没有 UA 会拒绝）
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        private static bool ValidateServerCertificate(HttpRequestMessage sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors)
        {
            // ★ 不建议完全跳过，但如果你访问的都是稳定站，可以这样写
            return true;

            // 如果你只想允许“非致命错误”
            // return errors == SslPolicyErrors.None;
        }

        // ★★★ 自动重试（解决极偶发性 SSL/TCP Reset）
        public static async Task<HttpResponseMessage> GetWithRetryAsync(string url, int retry = 3)
        {
            for (int i = 0; i < retry; i++)
            {
                try
                {
                    return await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                }
                catch
                {
                    if (i == retry - 1) throw; // 最后一次，直接抛出
                    await Task.Delay(500); // 等一会再试
                }
            }
            throw new Exception("Unreachable retry logic.");
        }
    }
}
