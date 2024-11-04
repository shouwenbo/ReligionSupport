using Microsoft.AspNetCore.Mvc;

namespace PhraseHelperApi.Controllers
{
    [Route("api/phrase/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return "ok123";
        }
    }
}