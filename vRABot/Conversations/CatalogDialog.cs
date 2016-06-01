using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
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
        private string requestId;

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
                        this.requestId = await this.currentServer.RequestCatalogItem(item.Entity);
                        PromptDialog.Confirm(context, PollingConfirmed, $"Requested item with id = {requestId}. Do you want a status report?", promptStyle: PromptStyle.None);
                    }
                }
                else
                {
                    await context.PostAsync("Please specify item to request.");
                    context.Wait(MessageReceived);
                }
            }
        }

        private async Task PollingConfirmed(IDialogContext context, IAwaitable<bool> result)
        {
            if (await result)
            {
                await ReportProgress(context, this.requestId);
            }
        }

        private async Task<string> ReportProgress(IDialogContext context, string requestId)
        {
            string status = null;
            int attempts = 100;
            while (attempts-- > 0)
            {
                status = await this.currentServer.CheckRequestStatus(requestId);

                if (status == "SUCCESSFUL")
                {
                    await context.PostAsync($"Request with id \'{requestId}\' finished successfully.");
                    return status;
                }
                else if (status == "FAILED")
                {
                    await context.PostAsync($"Request with id \'{requestId}\' failed.");
                    return status;
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            return status;
        }
    }
}