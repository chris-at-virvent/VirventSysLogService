using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirventSysLogLibrary.Configuration
{
    public class ValueElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
        }
    }

    public class ValueElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ValueElement();
        }


        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ValueElement)element).Name;
        }
    }

    public class MultipleValuesSection : ConfigurationSection
    {
        [ConfigurationProperty("Values")]
        public ValueElementCollection Values
        {
            get { return (ValueElementCollection)this["Values"]; }
        }
    }
}
