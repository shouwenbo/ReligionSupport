using Newtonsoft.Json;

namespace PhraseHelperApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<DirectoryService>();

            services.AddMemoryCache();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
            });

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            services.AddCors();

            services.AddControllers(options =>
            {
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DirectoryService directoryService)
        {
            if (env.IsDevelopment())
            {
                string sourceDir = @"F:\cd helper\文字话术 & 问候语";
                string destDir = Path.Combine(env.WebRootPath, "phrases");
                directoryService.CopyDirectory(sourceDir, destDir);
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseRouting();

            app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(o => true).AllowCredentials());

            app.UseAuthorization();

            app.UseResponseCompression();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var phrasesDirectory = Path.Combine(env.WebRootPath, "phrases");
            PhraseRepository.LoadPhrases(phrasesDirectory);
        }
    }
}