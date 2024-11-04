using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace PhraseHelperApi.Controllers
{
    [Route("api/phrase/[controller]/[action]")]
    [ApiController]
    public class PhraseController: BaseController
    {
        private readonly IMemoryCache _cache;
        private readonly Random _random = new Random();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public PhraseController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        [HttpPost]
        public IActionResult GetRandomPhrase(GetRandomPhraseRequestModel request)
        {
            var result = PhraseRepository.GetRandomPhrase(request.types);

            if (result == null)
            {
                return new CustomActionResult(DBNull.Value, 500, "No phrases found for the provided types.");
            }

            var clonedResult = result.DeepClone();
            clonedResult.OutputHandle(request.outputType);

            return new CustomActionResult(clonedResult);
        }

        [HttpPost]
        public IActionResult QueryPhrasesList(QueryPhraseRequestModel request)
        {
            var (phrases, totalCount) = PhraseRepository.QueryPhrases(request.type, request.search, request.page, request.pageSize);

            var clonedList = phrases.Select(p => p.DeepClone()).ToList();
            clonedList.ForEach(p => { p.OutputHandle(request.outputType); });

            return new CustomActionResult(new
            {
                list = clonedList,
                total = totalCount
            });
        }
    }
    public class GetRandomPhraseRequestModel
    {
        public List<int> types { get; set; }
        public int outputType { get; set; }
    }
    public class QueryPhraseRequestModel
    {
        public int type { get; set; } = 1;
        public string? search { get; set; } = null;
        public int page { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public int outputType { get; set; }
    }
}