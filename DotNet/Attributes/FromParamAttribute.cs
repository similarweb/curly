using System;

namespace Similarweb.Curly
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromParamAttribute:Attribute
    {
        private int? _index;

        public FromParamAttribute()
        {
        }

        public FromParamAttribute(int index)
        {
            _index = index;
        }

        public int? Index
        {
            get { return _index; }
            set { _index = value; }
        }
    }
}
