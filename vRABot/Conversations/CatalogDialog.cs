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
            await context.PostAsync("Oops, I didn't get your request! :$");
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
                await context.PostAsync("Please specify vRA host.");
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("vra.list.catalog")]
        public async Task ListCatalogItems(IDialogContext context, LuisResult result)
        {
            if (this.currentServer != null)
            {
                var catItemsNames = await this.currentServer.GetCatalogItemNames();
                if (!catItemsNames.Any<string>())
                {
                    await context.PostAsync($"It seems that you don't have any catalog items! :(");
                }
                else
                {
                    var itemsFormatted = string.Join("\n\n", catItemsNames.Select(item => $"*{item}*"));
                    await context.PostAsync($"Here you are:\n\n{itemsFormatted}");
                }
            }
            else
            {
                await context.PostAsync("You need to specify the vRA host prior accessing the catalog! Which host would you like to use?");
            }

            context.Wait(MessageReceived);
        }

        private async Task ServerFormComplete(IDialogContext context, IAwaitable<ServerInfoForm> result)
        {
            this.currentServer = null;
            ServerInfoForm serverInfo = null;
            try
            {
                serverInfo = await result;
                this.currentServer = new vRAServer(serverInfo.hostname, serverInfo.username, serverInfo.password, serverInfo.tenant);
            }
            catch (OperationCanceledException)
            {
                await context.PostAsync("Operation was canceled...");
                return;
            }

            if (this.currentServer != null)
            {
                await context.PostAsync($"Server {serverInfo.hostname} is fine and I'm awaiting commands. You may want to list the catalog and order something.");
            }
            else
            {
                await context.PostAsync("Failed to register vRA server!");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("vra.request.catalog.item")]
        public async Task RequestCatalogItem(IDialogContext context, LuisResult result)
        {
            if (this.currentServer == null)
            {
                await context.PostAsync("You need to specify the vRA host prior requesting catalog items! Which host would you like to use?");
                context.Wait(MessageReceived);
            }
            else
            {
                EntityRecommendation item;
                if (result.TryFindEntity(CATALOG_ITEM_ENTITY, out item))
                {
                    int requests = 1;
                    /*
                    EntityRecommendation itemsToRequest;
                    if (result.TryFindEntity(NUMBER, out itemsToRequest))
                    {
                        if(!int.TryParse(itemsToRequest.Entity, out requests))
                        {
                            requests = 1;
                        }
                    }
                    */
                    for (int i = 0; i < requests; i++)
                    {
                        this.requestId = await this.currentServer.RequestCatalogItem(item.Entity);
                        PromptDialog.Confirm(context, PollingConfirmed, $"Request {requestId} is created. Do you want a status report?", promptStyle: PromptStyle.None);
                    }
                }
                else
                {
                    await context.PostAsync("I didn't get what item you want from the catalog.");
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

            context.Wait(MessageReceived);
        }

        private async Task ReportProgress(IDialogContext context, string requestId)
        {
            string status = null;
            int attempts = 100;
            while (attempts-- > 0)
            {
                status = await this.currentServer.CheckRequestStatus(requestId);

                if (status == "SUCCESSFUL")
                {
                    await context.PostAsync($"Request \'{requestId}\' finished successfully.");
                    break;
                }
                else if (status == "FAILED")
                {
                    await context.PostAsync($"Request \'{requestId}\' failed.");
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            if (status != "SUCCESSFUL" && status != "FAILED")
            {
                await context.PostAsync($"Request \'{requestId}\' finished with status {status}.");
            }
        }
    }
}