using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using vRABot.vRA;

namespace vRABot.Conversations
{
    [LuisModel("11c48ce9-00db-442c-b3ac-d38b9f82ce46", "9334becbdb9e411d8320908164c830f8")]
    [Serializable]
    public class CatalogDialog : LuisDialog<object>
    {
        const string SERVER_ENTITY = "vra.server";

        private vRAServer currentServer;

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
                var serverForm = new FormDialog<ServerInfo>(new ServerInfo(server.Entity.Replace(" ", "")), options: FormOptions.PromptInStart);
                context.Call<ServerInfo>(serverForm, ServerFormComplete);
            }
            else
            {
                await context.PostAsync("Please specify server.");
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("vra.list.catalog")]
        public async Task ListCatalogItems(IDialogContext context, LuisResult result)
        {
            if (this.currentServer != null)
            {
                var catItemsNames = await this.currentServer.GetCatalogItemNames();
                var message = "Choose an item: " + string.Join(Environment.NewLine, catItemsNames);
                await context.PostAsync(message);
            }
            else
            {
                await context.PostAsync("No server configured.");
            }

            context.Wait(MessageReceived);
        }

        private async Task ServerFormComplete(IDialogContext context, IAwaitable<ServerInfo> result)
        {
            this.currentServer = null;
            try
            {
                var serverInfo = await result;
                this.currentServer = new vRAServer(serverInfo.hostname, serverInfo.username, serverInfo.password, serverInfo.tenant);
            }
            catch (OperationCanceledException)
            {
                await context.PostAsync("Canceled...");
                return;
            }

            if (this.currentServer != null)
            {
                await context.PostAsync("Server is configured, awaiting comands");
            }
            else
            {
                await context.PostAsync("Failed to configure server!");
            }

            context.Wait(MessageReceived);
        }
    }
}