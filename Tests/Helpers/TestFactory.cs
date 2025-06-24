using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Moq;
using System.IO;
using System.Net;
using System.Text;

namespace Tests.Helpers;

public static class TestFactory
{
  public static FunctionContext CreateFunctionContext() =>
    new Mock<FunctionContext>().Object;
  
  public static HttpRequestData CreateHttpRequestData(
    FunctionContext context,
    string method,
    string url,
    Stream body,
    string contentType)
  {
    var request = new Mock<HttpRequestData>(context);
    request.Setup(r => r.Body).Returns(body);
    request.Setup(r => r.Method).Returns(method);
    request.Setup(r => r.Headers).Returns(new HttpHeadersCollection
    {
      { "Content-Type", contentType }
    });
    request.Setup(r => r.Url).Returns(new Uri($"https://localhost:7071/{url}"));
    request.Setup(r => r.CreateResponse(It.IsAny<HttpStatusCode>()))
            .Returns<HttpStatusCode>(code =>
            {
              var response = new Mock<HttpResponseData>(context);
              response.SetupProperty(r => r.StatusCode, code);
              response.Setup(r => r.Headers).Returns(new HttpHeadersCollection());
              response.Setup(r => r.Body).Returns(new MemoryStream());
              response.Setup(r => r.WriteString(It.IsAny<string>()))
                      .Callback<string>(s =>
                      {
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes(s));
                        response.Object.Body = stream;
                      });
              return response.Object;
            });

    return request.Object;
  }
}