using System;

namespace Curly.Attributes
{
    public class CurlyInterpreterAttribute:Attribute
    {
        public int Order { get; } = 999;

        public CurlyInterpreterAttribute()
        {
            
        }

        public CurlyInterpreterAttribute(int order)
        {
            Order = order;
        }
    }
}
