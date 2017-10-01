using System.Reflection;

namespace Curly.Contract
{
    public interface ICurlyMethodBinder
    {
        CurlyConverter BindConverter(MethodInfo methodInfo);
        CurlyMethod Bind(MethodInfo methodInfo);
    }
}
