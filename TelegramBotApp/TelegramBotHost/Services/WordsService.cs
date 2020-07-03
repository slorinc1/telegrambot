using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TelegramBotHost
{
    public class WordsService
    {
        private readonly HttpClient _httpClient;

        public WordsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetWordAsync()
        {
            var word = await _httpClient.GetStringAsync("words/get");

            return word.Trim('\"');
        }

        public async Task<AnswerModel> GetAnswerAsync(string word)
        {
            var model = await _httpClient.GetStringAsync($"words/answer?word={word}");

            return JsonSerializer.Deserialize<AnswerModel>(model); ;
        }
    }
}
