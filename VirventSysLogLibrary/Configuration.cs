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
        [ConfigurationProperty("key", IsKey = true, IsRequired = true)]
        public string Key
        {
            get { return (string)this["key"]; }
        }
        [ConfigurationProperty("value", IsKey = false, IsRequired = true)]
        public string Value
        {
            get { return (string)this["value"]; }
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
            return ((ValueElement)element).Key;
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
