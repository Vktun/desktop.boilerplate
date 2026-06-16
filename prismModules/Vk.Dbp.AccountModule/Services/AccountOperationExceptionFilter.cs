using Dabp.Utils.Exceptions;

namespace Vk.Dbp.AccountModule.Services;

internal static class AccountOperationExceptionFilter
{
    public static bool IsExpectedDataOperationException(Exception exception)
    {
        return ExpectedOperationExceptionFilter.IsExpectedDataOperationException(exception);
    }
}
