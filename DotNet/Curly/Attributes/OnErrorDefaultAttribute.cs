using System;

namespace Curly.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class OnErrorDefaultAttribute:Attribute
    {
        private string _default;

        public OnErrorDefaultAttribute()
        {
            
        }
        public OnErrorDefaultAttribute(string @default)
        {
            _default = @default;
        }

        public string Value
        {
            get { return _default; }
            set { _default = value; }
        }
    }
}
