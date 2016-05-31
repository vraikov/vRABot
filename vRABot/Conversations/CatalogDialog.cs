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
    public class CatalogDialog : LuisDialog<object>
    {
        const string SERVER_ENTITY = "vra.server";

        private string currentServer;
        private string addingServer;

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
                this.addingServer = server.Entity.Replace(" ", "");
                PromptDialog.Confirm(context, UseServerConfirmed, $"Are you sure you want to connect to \'{this.addingServer}\'?", promptStyle: PromptStyle.None);

            }
            else
            {
                await context.PostAsync("Please specify server.");
                context.Wait(MessageReceived);
            }
        }

        public async Task UseServerConfirmed(IDialogContext context, IAwaitable<bool> confirmation)
        {
            if (await confirmation && !string.IsNullOrWhiteSpace(this.addingServer))
            {
                this.currentServer = this.addingServer;
                this.addingServer = null;
                await context.PostAsync($"Using server \'{this.currentServer}\'.");
            }
            else
            {
                await context.PostAsync($"Still using server \'{this.currentServer ?? "N/A"}\'.");
            }

            context.Wait(MessageReceived);
        }
    }
}