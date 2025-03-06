using Microsoft.SemanticKernel;

namespace ChatApp.WebApi.Services;

public sealed class FunctionInvocationFilter(Func<FunctionInvocationContext, Func<FunctionInvocationContext, Task>, Task> onFunctionInvocation) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        await onFunctionInvocation(context, next);
    }
}