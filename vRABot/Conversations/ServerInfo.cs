using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace vRABot.Conversations
{
    [Serializable]
    public class ServerInfo
    {
        [Optional]
        public string hostname;

        public string username;

        public string password;

        public string tenant;

        public ServerInfo(string server)
        {
            this.hostname = server;
        }

        public static IForm<ServerInfo> BuildForm()
        {
            return new FormBuilder<ServerInfo>()
                    .Message("Enter login information")
                    .Build();
        }
    }
}