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
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;

    public ContactMeEntity()
    {
        var now = DateTime.UtcNow;
        SubmittedAt = now;
        SetKeys(now);
    }

    public ContactMeEntity(ContactMeModel model)
    {
        Id = model.Id;
        Name = model.Name;
        Email = model.Email;
        Message = model.Message;
        SubmittedAt = model.SubmittedAt.EnsureUtc();
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
            SubmittedAt = SubmittedAt,
            UserAgent = UserAgent,
            IpAddress = IpAddress
        };
    }
}