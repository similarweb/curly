using System;
using JetBrains.Annotations;

namespace Similarweb.Curly
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class DslMethodAttribute:Attribute
    {
        private string _name;

        public DslMethodAttribute(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}
