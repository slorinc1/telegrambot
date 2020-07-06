using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace TelegramBotHost
{
    public class MyBotClient {

        public TelegramBotClient Bot { get; set; }

        public MyBotClient(IOptions<BotToken> options)
        {
            Bot = new TelegramBotClient(options.Value.Token);
        }
    }
}
