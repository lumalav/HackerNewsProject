using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using HackerNewsTaskTests.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

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


        //[TestMethod()]
        //public void asda()
        //{
        //    // Arrange
        //    var myDependency = Substitute.For<IMemoryCache>();
        //    var webHostBuilder = WebHostBuilderFactory.Create<Startup>(
        //        services => services.Replace<IMyDependency>(myDependency), 
        //        services => services.SetSystemClock(DateTime.Now.AddYears(-2)));
        //    var serviceProvider = webHostBuilder.Build().Services;
        //    var myService = serviceProvider.GetRequiredService<IMyService>();
        //    var expected = "awesome stuff";

        //    // Act
        //    var actual = myService.DoStuff();

        //    // Assert
        //    Assert.That(actual, Is.EqualTo(expected));
        //}
    }

    internal static class WebHostBuilderFactory
    {
        public static IWebHostBuilder Create<TStartup>(
            params Action<IServiceCollection>[] testSpecificServiceConfigurations)
            where TStartup : class
        {
            var contentRootPath = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\src\App");
            return new WebHostBuilder().UseContentRoot(contentRootPath)
                .UseEnvironment(EnvironmentName.Development)
                .UseStartup<TStartup>()
                .ConfigureServices(services =>
                {
                    services.AddCors(options =>
                    {
                        options.AddPolicy("CorsPolicy",
                            builder => builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader());
                    });
                    services.AddMemoryCache();
                    services.AddControllers().AddNewtonsoftJson(options =>
                        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                    );
                });

        }
    }
}