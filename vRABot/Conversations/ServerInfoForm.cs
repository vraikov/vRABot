using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace vRABot.Conversations
{
    [Serializable]
    public class ServerInfoForm
    {
        [Optional]
        public string hostname;

        public string username;

        public string password;

        public string tenant;

        public ServerInfoForm(string server)
        {
            this.hostname = server;
        }

        public static IForm<ServerInfoForm> BuildForm()
        {
            return new FormBuilder<ServerInfoForm>()
                    .Message("Enter login information")
                    .Build();
        }
    }
}