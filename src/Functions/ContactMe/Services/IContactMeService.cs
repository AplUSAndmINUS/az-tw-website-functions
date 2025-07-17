using Functions.ContactMe.Models;

namespace Functions.ContactMe.Services;

/// <summary>
/// Interface for contact form service operations
/// </summary>
public interface IContactMeService
{
    /// <summary>
    /// Processes a contact form submission by storing it and sending an email
    /// </summary>
    /// <param name="model">Contact form submission model</param>
    /// <returns>Task representing the async operation</returns>
    Task ProcessContactSubmissionAsync(ContactMeModel model);

    /// <summary>
    /// Stores a contact form submission in table storage
    /// </summary>
    /// <param name="model">Contact form submission model</param>
    /// <returns>Task representing the async operation</returns>
    Task StoreContactSubmissionAsync(ContactMeModel model);
}