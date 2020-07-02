using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotHost
{
    public class TelegramBotService : BackgroundService
    {
        private TelegramBotClient _bot;

        private readonly IWebHostEnvironment _env;

        private readonly HttpClient _httpClient;

        BotToken _options;

        public TelegramBotService(IWebHostEnvironment env, IOptions<BotToken> options)
        {
            _env = env;
            _options = options.Value;
            Init().Wait();

            _httpClient = new HttpClient();
        }

        public async Task Init()
        {
            _bot = new TelegramBotClient(_options.Token);

            var me = await _bot.GetMeAsync();
            Console.Title = me.Username;

            _bot.OnMessage += BotOnMessageReceived;
            _bot.OnMessageEdited += BotOnMessageReceived;

            _bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            _bot.OnInlineQuery += BotOnInlineQueryReceived;
            _bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            _bot.OnReceiveError += BotOnReceiveError;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _bot.StartReceiving(Array.Empty<UpdateType>());

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _bot.StopReceiving();

            return base.StopAsync(cancellationToken);
        }

        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            if (message == null || message.Type != MessageType.Text)
                return;

            switch (message.Text.Split(' ').First())
            {
                // Send inline keyboard
                case "/inline":
                    await SendInlineKeyboard(message);
                    break;

                // send custom keyboard
                case "/keyboard":
                    await SendReplyKeyboard(message);
                    break;

                // send a photo
                case "/photo":
                    //await SendDocument(message);
                    break;

                // request location or contact
                case "/request":
                    await RequestContactAndLocation(message);
                    break;

                default:
                    await Usage(message);
                    break;
            }
        }

        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handler
        async Task SendInlineKeyboard(Message message)
        {
            await _bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            // Simulate longer running task
            await Task.Delay(500);

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Yes", "Yeee"),
                        InlineKeyboardButton.WithCallbackData("No", "Booo"),
                    }
                });
            await _bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Are you a squirrel?",
                replyMarkup: inlineKeyboard
            );
        }

        async Task SendReplyKeyboard(Message message)
        {
            var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                new KeyboardButton[][]
                {
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                },
                resizeKeyboard: true
            );

            await _bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Choose",
                replyMarkup: replyKeyboardMarkup

            );
        }

        async Task SendDocument(Message message, bool squarel)
        {
            await _bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

            string picName = $"{(squarel ? ("yes.jpg") : ("no.jpg")) }";
            string filePath = Path.Combine(_env.WebRootPath, picName);
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
            await _bot.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputOnlineFile(fileStream, fileName),
                caption: squarel ? "Yeee" : "Boo"
            );
        }

        async Task RequestContactAndLocation(Message message)
        {
            var RequestReplyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });
            await _bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Who or Where are you?",
                replyMarkup: RequestReplyKeyboard
            );
        }

        async Task Usage(Message message)
        {
            const string usage = "Usage:\n" +
                                    "/inline   - send inline keyboard\n" +
                                    "/keyboard - send custom keyboard\n" +
                                    "/photo    - send a photo\n" +
                                    "/request  - request location or contact" + "\n" +
                                    "Contanct:   slorinc@mail.com";
            await _bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove()
            );
        }

        // Process Inline Keyboard callback data
        private async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            await _bot.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"{callbackQuery.Data}"
            );

            await _bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"{callbackQuery.Data}"
            );

            if (callbackQuery.Data == "Yeee")
            {
                await SendDocument(callbackQuery.Message, true);
            }
            else
            {
                await SendDocument(callbackQuery.Message, false);
            }
        }

        #region Inline Mode

        private async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };
            await _bot.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0
            );
        }

        private void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        #endregion

        private void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _httpClient.GetAsync("https://telegrambothost.herokuapp.com/");
                }
                catch
                {
                }

                await Task.Delay(TimeSpan.FromMinutes(15));
            }
        }
    }
}
