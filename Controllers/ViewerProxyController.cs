﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace DataManagementSample.Controllers
{
  public class ViewerProxyController : ApiController
  {
    private const string FORGE_BASE_URL = "https://developer.api.autodesk.com";
    private const string PROXY_ROUTE = "api/forge/viewerproxy/";

    private string AccessToken
    {
      get
      {
        var cookies = Request.Headers.GetCookies();
        var accessToken = string.Empty;
        foreach (var c in cookies)
          foreach (var v in c.Cookies)
            if (v.Name.Equals("ForgeOAuth"))
              accessToken = v.Value;
        return accessToken;
      }
    }

    // HttpClient has been designed to be re-used for multiple calls. 
    // Even across multiple threads. 
    // https://stackoverflow.com/a/22561368/4838205
    private static HttpClient _httpClient;

    [HttpGet]
    [Route(PROXY_ROUTE + "{*.}")]
    public async Task<HttpResponseMessage> Get()
    {
      if (_httpClient == null)
      {
        _httpClient = new HttpClient(
          // this should avoid HttpClient seaching for proxy settings
          new HttpClientHandler()
          {
            UseProxy = false,
            Proxy = null
          }, true);
        _httpClient.BaseAddress = new Uri(FORGE_BASE_URL);
        ServicePointManager.DefaultConnectionLimit = int.MaxValue;
      }

      string url = Request.RequestUri.AbsolutePath.Replace(PROXY_ROUTE, string.Empty);
      string resourceUrl = url + Request.RequestUri.Query;

      try
      {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, resourceUrl);

        // add our Access Token
        request.Headers.Add("Authorization", "Bearer " + AccessToken);

        HttpResponseMessage response = await _httpClient.SendAsync(request,
          // this ResponseHeadersRead force the SendAsync to return
          // as soon as the header is ready, faster
          HttpCompletionOption.ResponseHeadersRead);

        return response;
      }
      catch
      {
        return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
      }
    }
  }
}

