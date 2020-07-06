using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace TelegramBotHost.Controllers
{
    public class MessageController : Controller
    {
        MyBotClient _myBotClient;

        IOptions<BotToken> _options;

        public MessageController(MyBotClient myBotClient, IOptions<BotToken> options)
        {
            _myBotClient = myBotClient;
            _options = options;
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> SendAsync([FromQuery] string m)
        {
            var chat = new ChatId(_options.Value.Id);

            await _myBotClient.Bot.SendTextMessageAsync(chat, m);

            return Ok();
        }
    }
}
