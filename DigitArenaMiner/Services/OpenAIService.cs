using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Discord.Rest;

namespace DigitArenaBot.Services
{
    public class OpenAIService
    {
        private readonly DiscordSocketClient _client;

        public OpenAIService(DiscordSocketClient client)
        {
            _client = client;

            _client.MessageReceived += async message =>
            {
                if (message.Author.Id != 256114627794960384) return;

                var messageContentLowered = message.Content.ToLower();
                if (messageContentLowered.Contains("me") && messageContentLowered.Contains("on") &&  (messageContentLowered.Contains("left") || messageContentLowered.Contains("right")))
                {
                    var isLeft = message.Content.ToLower().Contains("left") ? "right" : "left";
                    await message.Channel.SendMessageAsync($"me on {isLeft}", messageReference: message.Reference);
                    return;
                }
                
                if (messageContentLowered.Equals("us"))
                {
                    await message.Channel.SendMessageAsync($"this is so us");
                    return;
                }
                
                if (messageContentLowered.Contains("me") && messageContentLowered.Contains("and") && messageContentLowered.Contains("who"))
                {
                    await message.Channel.SendMessageAsync($"me <:feelsWOWman:946051635610812456>", messageReference: message.Reference);
                    return;
                }
                
                var options = new ChatCompletionsOptions();
                options.Messages.Add(new ChatMessage(ChatRole.System, 
                    "Don't mention you are language model. You should behave as Tomoko Kuroki, who is 23 year old girl who studies Applied informatics at VŠE. " +
                    "Thanks to erasmus+ exchange program." +
                    "YOU ARE GIRL. Everytime someone mentions Tomoko in context it is you no one else." +
                    "You love person with nick 'Lajtkek' (when adressing just use Lajtkek), 'Matthew' and czech name 'Matěj Lajtkep'" +
                    "Don't write too formally." +
                    "In chat provided messages you will see '<name>:<message>' DO NOT REPLY IN SAME PATTERN this pattern is used only to give you context who said what."+
                    "You don't need to advance conversation all the time."));
                
                var idMessageReply = message.Reference?.MessageId;
                if (idMessageReply != null)
                {
                    var replyMsg = await message.Channel.GetMessageAsync(idMessageReply.GetValueOrDefault(0).Value);
    
                    if(replyMsg == null) return;

                    
                    if (replyMsg.Author.Id != 1155178035046252555 && !message.Content.ToLower().Contains("tomoko")) return;
                    
                    options.Messages.Add(new ChatMessage(ChatRole.Assistant, $"{replyMsg.Author.Username}:" + replyMsg.Content));
                    options.Messages.Add(new ChatMessage(ChatRole.User,$"{replyMsg.Author.Username}:" + message.Content));
                }

                if (message.Content.ToLower().Contains("tomoko"))
                {
                    options.Messages.Add(new ChatMessage(ChatRole.User, $"{message.Author.Username}:" + message.Content));
                }
                
                if (options.Messages.Count > 1)
                {
                    OpenAIClient client = new OpenAIClient("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");
  
                    var response = await client.GetChatCompletionsAsync("gpt-4", options);

                    await message.Channel.SendMessageAsync(string.Join(" ", response.Value.Choices.Select(x =>x.Message.Content)));
                }
            };
            
        }
    }
}