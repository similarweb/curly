using System.Reflection;

namespace Similarweb.Curly.Contract
{
    public interface ICurlyMethodBinder
    {
        CurlyConverter BindConverter(MethodInfo methodInfo);
        CurlyMethod Bind(MethodInfo methodInfo);
    }
}
