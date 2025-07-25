using Azure;
using Azure.Data.Tables;
using Utils.Extensions;

namespace Functions.ContactMe.Models;

/// <summary>
/// Entity for storing contact form submissions in Azure Table Storage
/// </summary>
public class ContactMeEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;  // Added subject field
    public string Phone { get; set; } = string.Empty;    // Added phone field
    public string Company { get; set; } = string.Empty;  // Added company field
    public string Website { get; set; } = string.Empty;  // Added website field
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow.EnsureValidStorageDate();
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;

    public ContactMeEntity()
    {
        var now = DateTime.UtcNow.EnsureValidStorageDate();
        SubmittedAt = now;
        SetKeys(now);
    }

    public ContactMeEntity(ContactMeModel model)
    {
        Id = model.Id;
        Name = model.Name;
        Email = model.Email;
        Message = model.Message;
        Subject = model.Subject ?? string.Empty;  // Added subject field
        Phone = model.Phone ?? string.Empty;      // Added phone field
        Company = model.Company ?? string.Empty;  // Added company field
        Website = model.Website ?? string.Empty;  // Added website field
        SubmittedAt = model.SubmittedAt.EnsureValidStorageDate();
        UserAgent = model.UserAgent;
        IpAddress = model.IpAddress;
        SetKeys(SubmittedAt);
    }

    private void SetKeys(DateTime submittedAt)
    {
        PartitionKey = submittedAt.ToString("yyyy-MM");
        RowKey = $"{submittedAt:yyyyMMddHHmmss}_{Id}";
    }

    public ContactMeModel ToModel()
    {
        return new ContactMeModel
        {
            Id = Id,
            Name = Name,
            Email = Email,
            Message = Message,
            Subject = Subject,              // Added subject field
            Phone = Phone,                  // Added phone field
            Company = Company,              // Added company field
            Website = Website,              // Added website field
            SubmittedAt = SubmittedAt,
            UserAgent = UserAgent,
            IpAddress = IpAddress
        };
    }
}