using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HackerNewsTask.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HackerNewsController : ControllerBase
    {
        private static readonly Uri BaseUrl = new Uri("https://hacker-news.firebaseio.com/v0/");
        private static readonly Uri SearchBaseUrl = new Uri("https://hn.algolia.com/api/v1/");
        private readonly IMemoryCache _cache;
        private readonly int _fetchNewsCacheExpirationTimeInMinutes;

        public HackerNewsController(IMemoryCache memoryCache, IConfiguration config)
        {
            _cache = memoryCache;
            _fetchNewsCacheExpirationTimeInMinutes = config.GetValue<int>("Caching:FetchNewsExpirationTimeInMinutes");
        }

        /// <summary>
        /// Fetch multiple server-side paginated items from the hack news api
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [Route("fetch")]
        [HttpGet]
        public async Task<IActionResult> FetchHackerNewsIDs(CancellationToken token)
        {
            try
            {
                //check parameters
                foreach (var p in new[] { "pageIndex", "pageSize" })
                {
                    if (!Request.Query.ContainsKey(p))
                    {
                        throw new Exception($"param: {p} is missing from the query!");
                    }
                }

                //Parse parameters
                var pageIndex = Request.Parse<int>("pageIndex");
                var pageSize = Request.Parse<int>("pageSize");

                var isSearchQuery = Request.Query.ContainsKey("Search") && !string.IsNullOrWhiteSpace(Request.Query["Search"]);

                if (isSearchQuery)
                {
                    var result = await CacheTryGetOrFetch(QueryType.SearchedQuery, token, pageIndex, pageSize);
                    return Ok(result);
                }
             
                var newsIds = await CacheTryGetOrFetch(QueryType.PagedQuery, token, pageIndex, pageSize);
                var totalNews = await CacheTryGetOrFetch(QueryType.TotalQuery, token);

                return Ok(new
                {
                    Results = newsIds,
                    Total = totalNews
                });
            }
            catch (Exception exception)
            {
                return StatusCode(500, exception); //500: Internal server error
            }
        }

        /// <summary>
        /// Loads the data of a hack news Item
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [Route("load")]
        [HttpGet]
        public async Task<IActionResult> LoadHackerNews(CancellationToken token)
        {
            try
            {
                //Parse parameters
                if (!Request.Query.ContainsKey("id"))
                {
                    throw new Exception("param: id is missing from the query!");
                }

                var result = await CacheTryGetOrFetch(QueryType.LoadNews, token);
                return Ok(result);
            }
            catch (Exception exception)
            {
                return StatusCode(500, exception); //500: Internal server error
            }
        }

        /// <summary>
        /// checks if a result is already in the cache, if not it will perform a query
        /// </summary>
        /// <param name="queryType"></param>
        /// <param name="token"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<object> CacheTryGetOrFetch(QueryType queryType, CancellationToken token, int? pageIndex = null, int? pageSize = null)
        {
            var entry = GenerateCacheEntry(queryType, pageIndex, pageSize);

            if (!_cache.TryGetValue(entry, out object cacheEntry))
            {
                cacheEntry = await FetchNews(queryType, token, pageIndex, pageSize);

                _cache.Set(entry, cacheEntry, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(_fetchNewsCacheExpirationTimeInMinutes)));
            } 
            else if (queryType == QueryType.PagedQuery || queryType == QueryType.SearchedQuery)
            {
                //we hit on some ids. Maybe we can hit on the entire object as well
                var cachedEntries = new List<dynamic>();

                var items = queryType == QueryType.PagedQuery
                    ? (dynamic[]) cacheEntry
                    : (dynamic[]) ((dynamic) cacheEntry).Results;

                foreach (var item in items)
                {
                    var entry2 = ((long) item.Id).ToString();

                    if (_cache.TryGetValue(entry2, out object cacheEntry2))
                    {
                        cachedEntries.Add(cacheEntry2);
                    }
                }

                if (cachedEntries.Any())
                {
                    if (queryType == QueryType.PagedQuery)
                    {
                        return cachedEntries;
                    }

                    return new
                    {
                        ((dynamic) cacheEntry).Total,
                        Results = cachedEntries
                    };
                }
            }

            return cacheEntry;
        }

        /// <summary>
        /// Generates the cache entry based on the query type and page parameters
        /// </summary>
        /// <param name="queryType"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        private string GenerateCacheEntry(QueryType queryType, int? pageIndex, int? pageSize)
        {
            string entry;

            switch (queryType)
            {
                case QueryType.TotalQuery:
                    entry = "total";
                    break;
                case QueryType.SearchedQuery:
                    entry = $"{pageIndex}|{pageSize}|{Request.Query["Search"]}";
                    break;
                case QueryType.PagedQuery:
                    entry = $"{pageIndex}|{pageSize}";
                    break;
                default: //QueryType.LoadNews
                    entry = $"{Request.Query["id"]}";
                    break;
            }

            return entry;
        }

        /// <summary>
        /// Performs an HTTP GET and returns the serialized response if any
        /// </summary>
        /// <param name="token"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static async Task<string> ExecuteGet(CancellationToken token, Uri uri)
        {
            string serializedResponse;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(uri, token);
                if (response.IsSuccessStatusCode)
                {
                    serializedResponse = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(serializedResponse) || serializedResponse.Equals("null"))
                    {
                        throw new KeyNotFoundException("the requested page could not be found!");
                    }
                }
                else
                {
                    throw new KeyNotFoundException("the requested page could not be found!");
                }
            }

            return serializedResponse;
        }

        /// <summary>
        /// Performs the http requests to the hack news api
        /// </summary>
        /// <param name="queryType"></param>
        /// <param name="token"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        private async Task<object> FetchNews(QueryType queryType, CancellationToken token, int? pageIndex = null, int? pageSize = null)
        {
            bool isTotalRequest = false, isSearchedQuery = false, isLoadQuery = false;
            Uri uri;

            //generate the correct uri depending on the query type
            switch (queryType)
            {
                case QueryType.TotalQuery:
                    isTotalRequest = true;
                    uri = new Uri(BaseUrl, "topstories.json");
                    break;
                case QueryType.SearchedQuery:
                    isSearchedQuery = true;
                    uri = new Uri(SearchBaseUrl, $@"search_by_date?query=""{Request.Query["Search"]}""&hitsPerPage={pageSize}&page={pageIndex}&tags=story");
                    break;
                case QueryType.PagedQuery:
                    uri = new Uri(BaseUrl, $"topstories.json?{$@"&orderBy=""$key""&startAt=""{pageSize * pageIndex}""&limitToFirst={pageSize}"}");
                    break;
                default: //QueryType.LoadNews
                    uri = new Uri(BaseUrl, $"item/{Request.Query["id"]}.json");
                    isLoadQuery = true;
                    break;
            }

            //execute get
            var serializedResponse = await ExecuteGet(token, uri);

            //the following solution was chosen based on practicality.
            //New model classes could have been created to avoid dynamic typing, but at the end some filtering would have been needed anyway
            //That's why I decided to deserialize the object and transform it to what's needed
            var obj = JsonConvert.DeserializeObject(serializedResponse);

            //if its a load query, we don't transform the value
            if (isLoadQuery)
            {
                return obj;
            }

            //depending on the values of the parameters the response may differ its structure
            //three possibilities:
            //a) Searched Query. complex object, we care about the hits array which contains item ids in a property called objectID. We also care about the nbHits, needed for the pagination
            //b) dictionary array where the keys are the positions and the values are the item ids. We just care about the ids
            //c) array of the item ids (where they can be null) -> [null, null, 12323254]. Nulls need to be filtered
            
            if (obj is JObject jObject)
            {
                if (isSearchedQuery)
                {
                    var searchedQueryResult = (jObject["hits"] as JArray)?.ToObject<Dictionary<string, object>[]>()
                        .Select(i => long.TryParse(i["objectID"].ToString(), out var j) ? j : 0)
                        .Where(i => i > 0)
                        .Select(i => new { Id = i}).ToArray();

                    var total = (int) jObject["nbHits"];

                    return new
                    {
                        Total = total,
                        Results = searchedQueryResult
                    };
                }

                return jObject.ToObject<Dictionary<string, long>>()
                    .Select(i => new { Id = i.Value} ).ToArray();
            }

            var result = ((JArray)obj).ToObject<long?[]>().Where(i => i.HasValue)
                .Select(i => new { Id = i.Value}).ToArray();

            if (isTotalRequest)
            {
                return result.Length;
            }

            return result;
        }
    }
}