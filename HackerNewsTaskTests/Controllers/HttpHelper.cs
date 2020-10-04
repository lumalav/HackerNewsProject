using System;

namespace HackerNewsTaskTests.Controllers
{
    public static class HttpHelper
    {
        public static string Get(Uri uri)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                var response = client.GetAsync(uri).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                
                throw new Exception("oh no!");
            }
        }
    }
}
