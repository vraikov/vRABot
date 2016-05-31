using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using vRABot.Conversations;
using System.Threading.Tasks;

namespace vRABot.Tests
{
    [TestClass]
    public class APIClientHelperTests
    {
        const string server = "cafe:8443";
        const string user = "administrator";
        const string password = "VMware1!";
        const string tenant = "mcm";

        [TestMethod]
        public async Task When_Requested_Correct_Values_GetBearerToken_Should_Return_Token()
        {
            // Arrange

            // Act
            var result = await APIClientHelper.GetBearerToken(server, user, password, tenant);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task When_Requested_Correct_Values_GetBearerToken_Should_Return_Items_Names()
        {
            // Arrange

            // Act
            var token = await APIClientHelper.GetBearerToken(server, user, password, tenant);
            var result = await APIClientHelper.GetCatalogItems(server, token);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task When_Requested_Should_Request_Item()
        {
            // Arrange

            // Act
            var token = await APIClientHelper.GetBearerToken(server, user, password, tenant);
            var result = await APIClientHelper.RequestCatalogItem(server, token, "test-docker-amazon");

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
