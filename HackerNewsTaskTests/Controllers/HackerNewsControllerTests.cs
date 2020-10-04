using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using HackerNewsTaskTests.Controllers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HackerNewsTask.Controllers.Tests
{
    [TestClass()]
    public class HackerNewsControllerTests
    {
        private static readonly Uri BaseUri = new Uri("http://localhost:35072/");
        [TestMethod()]
        public void HackerNewsFetchFirstPage()
        {
            var httpResult = HttpHelper.Get(new Uri(BaseUri, "api/hackernews/fetch?pageIndex=0&pageSize=50"));

            var des = JsonConvert.DeserializeObject(httpResult);

            var jObj = des as JObject;

            Assert.IsTrue(jObj["results"] is JArray jArr && jArr.Count <= 50);
        }

        [TestMethod()]
        [Timeout(50000)]
        [ExpectedException(typeof(Exception), "oh no!")]
        public void HackerNewsFetchBigPage()
        {
            HttpHelper.Get(new Uri(BaseUri, "api/hackernews/fetch?pageIndex=5000&pageSize=10000"));
        }
    }
}