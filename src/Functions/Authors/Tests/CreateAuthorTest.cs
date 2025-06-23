using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.Authors.Functions;
using Functions.Authors.Models;
using Functions.Authors.Services;
using Functions.Authors.Validators;
using SharedStorage.Services;
using Utils;
using Utils.Validation;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Functions.Authors.Tests;

public class CreateAuthorTests
{
  [Fact]
  public void SampleTest_ShouldPass()
  {
    Assert.Equal(2, 1 + 1);
  }
}