using System;

namespace PhraseHelperApi
{
    public static class PhraseRepository
    {
        private static readonly object _lock = new object();
        public static List<Phrase> Phrases { get; private set; } = new List<Phrase>();

        public static void LoadPhrases(string baseDirectory)
        {
            lock (_lock)
            {
                var phrases = new List<Phrase>();

                // Define the directories and their corresponding PhraseType
                var directories = new Dictionary<string, PhraseType>
                {
                    { "一句话系列", PhraseType.ShortPhrase },
                    { "长话术系列", PhraseType.LongPhrase },
                    { "招生系列", PhraseType.StudyPhrase }
                };

                foreach (var directory in directories)
                {
                    string fullPath = Path.Combine(baseDirectory, directory.Key);
                    if (Directory.Exists(fullPath))
                    {
                        foreach (var filePath in Directory.GetFiles(fullPath, "*.txt"))
                        {
                            var phrase = new Phrase
                            {
                                Type = directory.Value,
                                Title = Path.GetFileNameWithoutExtension(filePath),
                                Content = File.ReadAllText(filePath)
                            };
                            phrases.Add(phrase);
                        }
                    }
                }

                Phrases = phrases;
            }
        }

        public static (List<Phrase> Phrases, int TotalCount) QueryPhrases(int type, string? search, int page, int pageSize)
        {
            lock (_lock)
            {
                var query = Phrases.Where(p => p.Type == (PhraseType)Enum.Parse(typeof(PhraseType), type.ToString()));

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                             p.Content.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                int totalCount = query.Count();
                var result = query.Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToList();

                return (result, totalCount);
            }
        }
        public static Phrase GetRandomPhrase(List<int> types)
        {
            if (types == null || types.Count == 0)
            {
                return null;
            }

            var filteredPhrases = Phrases
                .Where(p => types.Contains((int)p.Type))
                .ToList();

            if (filteredPhrases.Count > 0)
            {
                var random = new Random();
                var index = random.Next(0, filteredPhrases.Count);
                var randomPhrase = filteredPhrases[index];
                return randomPhrase;
            }
            else
            {
                return null;
            }
        }
    }
}
