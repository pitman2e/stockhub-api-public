using System.Net;
using System.Net.Http;
using StockHub.Models;

namespace StockHub.Crawlers;

public static class HttpClientFactory
{
    public static HttpClient Get()
    {
        HttpClientHandler handler = GetDefaultHttpHandler();
        HttpClient httpClient = new HttpClient(handler);
        ConfigureHttpClient(httpClient);
        return httpClient;
    }

    public static void ConfigureHttpClient(HttpClient httpClient)
    {
        httpClient.DefaultRequestVersion = HttpVersion.Version20;
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Config.WebBrowserUserAgent);
    }

    public static HttpClientHandler GetDefaultHttpHandler()
    {
        return new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
    }
}