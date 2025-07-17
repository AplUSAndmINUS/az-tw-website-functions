using Functions.Authors.Models;
using Functions.Authors.Helpers;
using Xunit;

namespace Tests.Authors;

/// <summary>
/// Basic unit tests for Author models and helpers
/// </summary>
public class AuthorBasicTests
{
    [Fact]
    public void AuthorModel_CanBeCreated()
    {
        // Arrange & Act
        var author = new AuthorModel
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Username = "johndoe"
        };

        // Assert
        Assert.NotNull(author);
        Assert.Equal("John", author.FirstName);
        Assert.Equal("Doe", author.LastName);
        Assert.Equal("john.doe@example.com", author.Email);
        Assert.Equal("johndoe", author.Username);
    }

    [Fact]
    public void AuthorDTO_CanBeCreated()
    {
        // Arrange & Act
        var authorDTO = new AuthorDTO
        {
            FirstName = "John",
            LastName = "Doe",
            Username = "johndoe"
        };

        // Assert
        Assert.NotNull(authorDTO);
        Assert.Equal("John", authorDTO.FirstName);
        Assert.Equal("Doe", authorDTO.LastName);
        Assert.Equal("johndoe", authorDTO.Username);
    }

    [Fact]
    public void SlugGenerator_ValidString_GeneratesSlug()
    {
        // Arrange
        var input = "This is a Test Title";

        // Act
        var slug = SlugGenerator.FromString(input);

        // Assert
        Assert.NotNull(slug);
        Assert.Equal("this-is-a-test-title", slug);
    }

    [Fact]
    public void SlugGenerator_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var slug = SlugGenerator.FromString(input);

        // Assert
        Assert.Equal(string.Empty, slug);
    }

    [Fact]
    public void SlugGenerator_NullString_ReturnsEmpty()
    {
        // Arrange
        string input = null;

        // Act
        var slug = SlugGenerator.FromString(input);

        // Assert
        Assert.Equal(string.Empty, slug);
    }

    [Fact]
    public void SlugGenerator_SpecialCharacters_RemovesSpecialChars()
    {
        // Arrange
        var input = "Test Title! @#$%^&*()";

        // Act
        var slug = SlugGenerator.FromString(input);

        // Assert
        Assert.NotNull(slug);
        Assert.DoesNotContain("!", slug);
        Assert.DoesNotContain("@", slug);
        Assert.DoesNotContain("#", slug);
        Assert.DoesNotContain("$", slug);
        Assert.DoesNotContain("%", slug);
    }
}