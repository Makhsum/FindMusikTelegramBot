using FindMusik.Application.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode.Search;
using File = System.IO.File;
using User = FindMusik.Domain.Models.User;

namespace FindMusik.Application.Services;

public class BotTelegram
{
  private readonly TelegramBotClient _botClient;
  private readonly ISaveToDatabase _saveToDatabase;
  private FindMusik _findMusik = new FindMusik();
    private IReadOnlyList<VideoSearchResult> _videoSearchResults = new List<VideoSearchResult>();
    Task handlecallbackquery = Task.CompletedTask;
    public BotTelegram(TelegramBotClient botClient,ISaveToDatabase saveToDatabase)
    {
        _botClient = botClient;
        _saveToDatabase = saveToDatabase;
    }
   
    CancellationTokenSource cts = new (); 
    ReceiverOptions receiverOptions = new ()
    {
       AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
    };
    public async Task StartReciving()
    {
         _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );
         
        var me = await _botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();
        
        cts.Cancel();
    }
   
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
   {
       if (await IsUserExist(update.Message.Chat.Id))
       {
           if (update.Type == UpdateType.CallbackQuery)
           {
               var callbackquery = update.CallbackQuery;
               try
               {
                   handlecallbackquery = HandleCallBackQueryAsync(callbackquery);
               }
               catch(Exception ex)
               {
                   if (ex.Message.Contains("bot was blocked by the user"))
                   {
                       await UserDeactive(update.Message.Chat.Id);
                   }
                   await botClient.SendTextMessageAsync(callbackquery.Message.Chat.Id, "Ошибка!чтобы скачать эту музыку напишите ее название еще раз в чате!");
                   await botClient.DeleteMessageAsync(callbackquery.Message.Chat.Id,callbackquery.Message.MessageId);
                   await botClient.SendTextMessageAsync(1635253907, ex.Message);
                   if (ex.Message.Contains("bot was blocked by the user"))
                   {
                       await UserDeactive(update.Message.Chat.Id);
                   }
                   return;
               }
           }
           if (update.Message is not { } message)
               return;
           if (message.Text is not { } messageText)
               return;

           try
           {
               var chatId = message.Chat.Id;
               var a = HandleCommands(update);
               Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
               await _botClient.SendTextMessageAsync(-1002212319511,$"Пользователь {update.Message.Chat.FirstName} сделал запрос на {messageText}");
               await handlecallbackquery;
           }
           catch (Exception e)
           {
               await botClient.SendTextMessageAsync(1635253907, e.Message);
               if (e.Message.Contains("bot was blocked by the user"))
               {
                  await UserDeactive(update.Message.Chat.Id);
               }
           }
       }
       else
       {
           User user = new User();
           user.Id = update.Message.Chat.Id;
           user.UserName = update.Message.Chat.Username??" ";
           user.Name = update.Message.Chat.FirstName ?? " ";
           user.LastName = update.Message.Chat.LastName ?? " ";
           user.isActive = true;
           await _saveToDatabase.AddAsync(user);
           await _saveToDatabase.SaveChangesAsync();

       }
        
      

   }
    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        
        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    private async Task HandleCallBackQueryAsync(CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var data = callbackQuery.Data;
        try
        {
         

            switch (data)
            {
                case "chatMember":
                    if (await IsMemberOfChannel(chatId))
                    {
                        await _botClient.SendTextMessageAsync(chatId,
                            "Поздроваляею теперь вы являетесь подписчиком нашей оффициальной группы");
                        await _botClient.SendTextMessageAsync(chatId,
                            "\u2764\ufe0f");
                    
                    }
                
                    break;
                default:
                   var message = await _botClient.SendTextMessageAsync(chatId,
                        "\u231b\ufe0f");
                    var coll = _videoSearchResults.ToList();
                    var fileName = await  _findMusik.DownloadAsync(coll[int.Parse(data)].Url);
                    if (File.Exists(fileName))
                    { 
                        var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var inputOnlineFile = 
                            await _botClient.SendAudioAsync(chatId, InputFile.FromStream(fileStream),
                                parseMode: ParseMode.Html,
                                title: fileName);
                        await _botClient.SendTextMessageAsync(chatId, "разработчик @isMakhsum");
                        await _botClient.DeleteMessageAsync( chatId,message.MessageId);
                       await fileStream.DisposeAsync();
                       File.Delete(fileName);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, fileName);
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            if (e.Message.Contains("bot was blocked by the user"))
            {
                await UserDeactive(chatId);
            }
            await _botClient.SendTextMessageAsync(1635253907, e.Message);
            return;
        }
        
      

    }
    
    private async Task<InlineKeyboardMarkup>  CreateKeyBoard(IReadOnlyList<VideoSearchResult> videoSearchResults)
    {
        try
        {
            int takeCount = 10;
            var resultList= videoSearchResults.Take(takeCount);

            int i = 0;
            List<(string Text, string сallbackData)> buttonsData = new List<(string Text, string сallbackData)>();

            foreach (var res in resultList)
            {
            
                buttonsData.Add(($"{res.Author}-{res.Title}",$"{i}"));
                i++;
            }
        
        
            List<List<InlineKeyboardButton>> inlineKeyboardButtons = new List<List<InlineKeyboardButton>>();
            foreach (var buttonData in buttonsData)
            {
                inlineKeyboardButtons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(buttonData.Text, buttonData.сallbackData)
                });
            }
            var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboardButtons);
            return inlineKeyboard;
        }
        catch (Exception e)
        {
            await _botClient.SendTextMessageAsync(1635253907, e.Message);
            return null;
        }
       
    }
    
    private async Task HandleCommands(Update update)
    {
        try
        {
            var chatId = update.Message.Chat.Id;
            switch (update.Message.Text)
            {
                case "/start":
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id,$"Здравствуйте {update.Message.Chat.FirstName} добро пожаловать! Чтобы найти музыку напишите ее имя и бот посторается найти его вам приятного слушанья \u2764\ufe0f ");
                  //  await _botClient.SendTextMessageAsync(-1002212319511,$"Пользователь {update.Message.Chat.FirstName} Запустил бота  ");
                 
                    
                    //-1002212319511
                    break;
                default:
                    if (await IsMemberOfChannel(update.Message.Chat.Id))
                    {
                        var message =  await _botClient.SendTextMessageAsync(chatId, "\u231b\ufe0f");
                        _videoSearchResults = await _findMusik.SearchAsync(update.Message.Text); 
                        var inlineKeyboard = await CreateKeyBoard(_videoSearchResults);
                        Message sentMessage = await _botClient.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            text: "result",
                            replyMarkup: inlineKeyboard
                        );
                        await _botClient.DeleteMessageAsync(chatId,message.MessageId);
                    }
                    break;
                
            }
        }
        catch (Exception e)
        {
            await _botClient.SendTextMessageAsync(1635253907, e.Message);
            if (e.Message.Contains("bot was blocked by the user"))
            {
                await UserDeactive(update.Message.Chat.Id);
            }
        }
        
    }

    private async Task<bool> IsMemberOfChannel(long chatId)
    {
        try
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
                new []{
                    InlineKeyboardButton.WithUrl(
                        text: "Подписаться на канал",
                        url: "https://t.me/FindMusikBotChannel"),
                    InlineKeyboardButton.WithCallbackData(text:"проверить подписку","chatMember")
                }
            
            });
            var chatMember = await _botClient.GetChatMemberAsync("@FindMusikBotChannel", chatId);

            if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Member || chatId == 1635253907)
            {
                return true;
            }

            await _botClient.SendTextMessageAsync(chatId,
                $"Пожалуйста подпишитесь на канал чтобы получить доступ к боту!",replyMarkup:inlineKeyboard);
            
        }
        catch (Exception e)
        {
            if (e.Message.Contains("bot was blocked by the user"))
            {
                await UserDeactive(chatId);
            }
           
        }
        return false;
        

    }
    private async Task<bool> IsUserExist(long userId)
    {
        var user = await _saveToDatabase.ReadAsync(userId);
        if (user!=null)
        {
            return true;
        }

        return false;

    }

    private async Task<bool> UserActive(long id)
    {
        var user = await _saveToDatabase.ReadAsync(id);
        if (user!=null)
        {
                user.isActive = true;
                await _saveToDatabase.SaveChangesAsync();
           
        }

        return false;
    }

    private async Task UserDeactive(long id)
    {
        var user = await _saveToDatabase.ReadAsync(id);
        if (user!=null)
        {
            user.isActive = false;
            await _saveToDatabase.SaveChangesAsync();
        }
       
    }
    
    
}