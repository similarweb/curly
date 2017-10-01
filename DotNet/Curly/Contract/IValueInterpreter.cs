namespace Curly.Contract
{
    public interface IValueInterpreter
    {
        bool TryConvert(string value, out object result);
    }
}
