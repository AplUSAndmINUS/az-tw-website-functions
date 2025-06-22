namespace Functions.Authors.Validators;

using Authors.Models;
using Utils.Validation;

public static class AuthorModelDataValidator
{
  public static IEnumerable<string> Validate(AuthorModel model)
  {
    if (model == null)
    {
      yield return "Author model cannot be null.";
      yield break;
    }
    if (string.IsNullOrWhiteSpace(model.FirstName))
    {
      yield return "First name is required.";
    }
    if (string.IsNullOrWhiteSpace(model.LastName))
    {
      yield return "Last name is required.";
    }
    if (DataValidation.TryValidateEmail(model.Email) == false)
    {
      yield return "Email is not valid.";
    }
    if (string.IsNullOrWhiteSpace(model.Username) || model.Username.Length < 5)
    {
      yield return "Username must be at least 5 characters long.";
    }
    if (string.IsNullOrWhiteSpace(model.DisplayName))
    {
      yield return "Display name is required.";
    }
  }
  public static bool TryValidate(AuthorModel model, out List<string> errors)
  {
    errors = Validate(model).ToList();
    return errors.Count == 0;
  }
}
