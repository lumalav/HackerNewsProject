using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HackerNewsTask.Controllers.Tests
{
    [TestClass()]
    public class HackerNewsControllerTests
    {
        private static async Task<HttpClient> Init()
        {
            var _webhost = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseUrls("http://localhost:35072")
                .UseTestServer()
                .Build();
            await _webhost.StartAsync();
            return _webhost.GetTestServer().CreateClient();
        }

        /// <summary>
        /// Simple query
        /// </summary>
        [TestMethod()]
        public void Simple_Query()
        {
            var client = Init();
            var response = client.Result.GetAsync("/api/hackernews/fetch?pageIndex=0&pageSize=50").Result;
            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        /// <summary>
        /// Simple Search Query
        /// </summary>
        [TestMethod()]
        public void Simple_Search_Query()
        {
            var client = Init();
            var response = client.Result.GetAsync("/api/hackernews/fetch?pageIndex=0&pageSize=50&Search=VPN").Result;
            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        /// <summary>
        /// Number of results need to be less or equal to the pageSize parameter
        /// </summary>
        [TestMethod()]
        public void Correct_Number_Of_Results()
        {
            var client = Init();
            var response = client.Result.GetAsync("/api/hackernews/fetch?pageIndex=0&pageSize=50&Search=VPN").Result;
            var s = response.Content.ReadAsStringAsync().Result;
            var deserialized = JsonConvert.DeserializeObject(s);
            var jObj = deserialized as JObject;
            Assert.IsTrue(jObj["results"] != null && jObj["results"] is JArray arr && arr.Count <= 50); 
        }

        /// <summary>
        /// If its too big of page, internal server error
        /// </summary>
        [TestMethod()]
        public void Big_Parameters_Internal_Server_Error()
        {
            var client = Init();
            var response = client.Result.GetAsync("/api/hackernews/fetch?pageIndex=0&pageSize=9999999999&Search=VPN").Result;
            Assert.IsTrue(!response.IsSuccessStatusCode); // internal server error
        }

        /// <summary>
        /// load simple item
        /// </summary>
        [TestMethod()]
        public void Loaded_Item_Has_A_Title()
        {
            var client = Init();
            var response = client.Result.GetAsync("/api/hackernews/load?id=24678381").Result;
            var s = response.Content.ReadAsStringAsync().Result;
            var deserialized = JsonConvert.DeserializeObject(s);
            var jObj = deserialized as JObject;
            Assert.IsTrue(jObj["title"] != null);//contains title
        }

        /// <summary>
        /// Unexisting item, internal server error
        /// </summary>
        [TestMethod()]
        public void Item_Does_Not_Exist()
        {
            var client = Init();
            var response = client.Result.GetAsync("/api/hackernews/load?id=99999999").Result;
            Assert.IsTrue(!response.IsSuccessStatusCode); // internal server error
        }
    }
}