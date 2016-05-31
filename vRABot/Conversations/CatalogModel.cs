using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace vRABot.Conversations
{
    [LuisModel("11c48ce9-00db-442c-b3ac-d38b9f82ce46", "9334becbdb9e411d8320908164c830f8")]
    [Serializable]
    public class CatalogModel : LuisDialog<object>
    {
        const string SERVER_ENTITY = "vra.server";
        private string currentServer;

        [LuisIntent("")]
        public async Task Default(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("What?");
            context.Wait(MessageReceived);
        }

        [LuisIntent("vra.use.server")]
        public async Task UseServer(IDialogContext context, LuisResult result)
        {
            EntityRecommendation server;
            if (result.TryFindEntity(SERVER_ENTITY, out server))
            {
                context.UserData.SetValue<string>("addingServer", server.Entity);
                PromptDialog.Confirm(context, UseServerConfirmed, $"Are you sure you want to add {server.Entity} ?");
            }
            else
            {
                await context.PostAsync("Please specify server.");
                context.Wait(MessageReceived);
            }
        }

        public async Task UseServerConfirmed(IDialogContext context, IAwaitable<bool> confirmation)
        {
            if (await confirmation && context.UserData.TryGetValue<string>("addingServer", out this.currentServer))
            {
                await context.PostAsync($"Ok will use server {this.currentServer}.");
            }
            else
            {
                await context.PostAsync("Ok! We haven't modified your alarms!");
            }

            context.Wait(MessageReceived);
        }
    }
}