using System;
using JetBrains.Annotations;

namespace Curly.Attributes
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
