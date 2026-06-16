using System;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Security;
using System.Text.Json;

namespace Dabp.Utils.Exceptions;

public static class ExpectedOperationExceptionFilter
{
    public static bool IsExpectedDataOperationException(Exception exception)
    {
        return exception is DbException
            or InvalidOperationException
            or ArgumentException
            or TimeoutException
            || exception.GetType().FullName?.Contains("SqlSugar", StringComparison.Ordinal) == true;
    }

    public static bool IsExpectedFileOperationException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or SecurityException
            or JsonException
            or NotSupportedException
            or Win32Exception
            or ArgumentException;
    }

    public static bool IsExpectedUserOperationException(Exception exception)
    {
        return IsExpectedDataOperationException(exception)
            || IsExpectedFileOperationException(exception);
    }

    public static bool IsExpectedExternalServiceException(Exception exception)
    {
        return IsExpectedUserOperationException(exception)
            || exception.GetType().Namespace?.StartsWith("StackExchange.Redis", StringComparison.Ordinal) == true;
    }
}
