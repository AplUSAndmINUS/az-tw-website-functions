using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Tests.Helpers;

public static class TestFactory
{
  public static FunctionContext CreateFunctionContext() =>
    new Mock<FunctionContext>().Object;

  public static HttpRequestData CreateHttpRequestData(
    FunctionContext context,
    string method,
    string url,
    string jsonBody,
    Dictionary<string, string> headers)
  {
    var request = new Mock<HttpRequestData>(context);
    
    // Setup body
    var bodyStream = new MemoryStream(
      string.IsNullOrEmpty(jsonBody) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(jsonBody)
    );
    request.Setup(r => r.Body).Returns(bodyStream);
    
    // Setup method and URL
    request.Setup(r => r.Method).Returns(method);
    request.Setup(r => r.Url).Returns(new Uri($"https://localhost:7071/{url}"));
    
    // Setup headers
    var headerCollection = new HttpHeadersCollection();
    if (headers != null)
    {
      foreach (var header in headers)
      {
        headerCollection.Add(header.Key, header.Value);
      }
    }
    request.Setup(r => r.Headers).Returns(headerCollection);
    
    return request.Object;
  }
  
  public static HttpRequestData CreateJsonRequest<T>(
    FunctionContext context,
    T data,
    string method,
    string url,
    Dictionary<string, string> additionalHeaders)
  {
    var json = JsonSerializer.Serialize(data);
    var headers = new Dictionary<string, string>
    {
      { "Content-Type", "application/json" }
    };
    
    if (additionalHeaders != null)
    {
      foreach (var header in additionalHeaders)
      {
        headers[header.Key] = header.Value;
      }
    }
    
    return CreateHttpRequestData(context, method, url, json, headers);
  }
  
  public static HttpRequestData CreateJsonRequestWithApiKey<T>(
    FunctionContext context,
    T data,
    string apiKey,
    string method,
    string url)
  {
    var headers = new Dictionary<string, string>
    {
      { "x-functions-key", apiKey }
    };
    
    return CreateJsonRequest(context, data, method, url, headers);
  }

  public static Stream CreateJsonStream<T>(T data)
  {
    var json = JsonSerializer.Serialize(data);
    return new MemoryStream(Encoding.UTF8.GetBytes(json));
  }
  
  public static Stream CreateStringStream(string content)
  {
    return new MemoryStream(Encoding.UTF8.GetBytes(content));
  }
}