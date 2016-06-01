using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Web;

namespace vRABot.vRA
{
    [Serializable]
    public class vRAServer
    {
        string token;
        string server;
        string username;
        string password;
        string tenant;

        public vRAServer(string server, string username, string password, string tenant)
        {
            this.server = server;
            this.username = username;
            this.password = password;
            this.tenant = tenant;
        }

        public async Task<IEnumerable<string>> GetCatalogItemNames()
        {
            return await GetServerResult<IEnumerable<string>>(() => APIClientHelper.GetCatalogItems(this.server, this.token)).ConfigureAwait(false);
        }

        public async Task<string> RequestCatalogItem(string item)
        {
            return await GetServerResult<string>(() => APIClientHelper.RequestCatalogItem(this.server, this.token, item));
        }

        private async Task<T> GetServerResult<T>(Func<Task<T>> action)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(this.token))
                {
                    this.token = await APIClientHelper.GetBearerToken(this.server, this.username, this.password, this.tenant);
                }

                return await action().ConfigureAwait(false);
            }
            catch (TokenExpiredException)
            {
                this.token = await APIClientHelper.GetBearerToken(this.server, this.username, this.password, this.tenant).ConfigureAwait(false);
                return await action().ConfigureAwait(false);
            }
        }
    }
}