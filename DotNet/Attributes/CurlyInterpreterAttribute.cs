using System;

namespace Similarweb.Curly
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
