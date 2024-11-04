using Microsoft.AspNetCore.Mvc;

namespace PhraseHelperApi.Controllers
{
    [Route("api/phrase/[controller]/[action]")]
    [ApiController]
    public class SettingController : BaseController
    {
        private readonly IWebHostEnvironment _env;

        public SettingController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult Reload()
        {
            var phrasesDirectory = Path.Combine(_env.WebRootPath, "phrases");
            PhraseRepository.LoadPhrases(phrasesDirectory);
            return new CustomActionResult("ok");
        }
    }
}