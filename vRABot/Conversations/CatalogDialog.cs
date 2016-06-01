using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
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
        const string CATALOG_ITEM_ENTITY = "vra.catalog.item";
        const string NUMBER = "builtin.number";

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
                var serverForm = new FormDialog<ServerInfoForm>(new ServerInfoForm(server.Entity.Replace(" ", "")), options: FormOptions.PromptInStart);
                context.Call<ServerInfoForm>(serverForm, ServerFormComplete);
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
                var itemsFormatted = string.Join(" ", catItemsNames.Select((item, i) => $"{i + 1}. {item}"));
                await context.PostAsync($"# H3 Choose an item: {itemsFormatted}");
            }
            else
            {
                await context.PostAsync("No server configured.");
            }

            context.Wait(MessageReceived);
        }

        private async Task ServerFormComplete(IDialogContext context, IAwaitable<ServerInfoForm> result)
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

        [LuisIntent("vra.request.catalog.item")]
        public async Task RequestCatalogItem(IDialogContext context, LuisResult result)
        {
            if (this.currentServer == null)
            {
                await context.PostAsync("No server configured.");
            }
            else
            {
                EntityRecommendation item;
                if (result.TryFindEntity(CATALOG_ITEM_ENTITY, out item))
                {
                    int requests = 1;
                    EntityRecommendation itemsToRequest;
                    if (result.TryFindEntity(NUMBER, out itemsToRequest))
                    {
                        //TODO: parse number
                    }

                    for (int i = 0; i < requests; i++)
                    {
                        var requestId = await this.currentServer.RequestCatalogItem(item.Entity);
                        await context.PostAsync($"Requested item with id = {requestId}");
                    }
                }
                else
                {
                    await context.PostAsync("Please specify item to request.");
                }
            }

            context.Wait(MessageReceived);
        }
    }
}