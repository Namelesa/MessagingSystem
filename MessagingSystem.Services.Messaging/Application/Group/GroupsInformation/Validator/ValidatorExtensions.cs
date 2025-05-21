using FluentValidation;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Validator;

public static class ValidatorExtensions
{
    public static async Task<OperationResult<T>> ToOperationResultAsync<T>(
        this IValidator<T> validator, T instance)
    {
        var result = await validator.ValidateAsync(instance);
        return result.IsValid
            ? OperationResult<T>.Ok(instance)
            : OperationResult<T>.Fail(string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }
}
