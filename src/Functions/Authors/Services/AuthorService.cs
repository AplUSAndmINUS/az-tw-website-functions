using Functions.Authors.Models;
using SharedStorage.Services;
using SharedStorage.Validators;

namespace Functions.Authors.Services;

public interface IAuthorService
{
  Task<AuthorDTO> CreateAuthorAsync(AuthorModel model);
  Task<AuthorEntity> UpsertEntityAsync(string tableName, AuthorEntity authorEntity);
}

