﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs;
using vRABot.Conversations;

namespace vRABot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            try
            {
                if (message.Type == "Message")
                {
                    if (message.Text.ToLowerInvariant() == "reset")
                    {
                        var msg = message.CreateReplyMessage("reset");
                        msg.SetBotPerUserInConversationData("DialogState", null);
                        return msg;
                    }
                    else
                    {
                        return await Conversation.SendAsync(message, () => new CatalogDialog());
                    }
                }
                else
                {
                    return HandleSystemMessage(message);
                }
            }
            catch (Exception ex)
            {
                return message.CreateReplyMessage($"An error occured - {ex.Message}");
            }
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }
    }
}