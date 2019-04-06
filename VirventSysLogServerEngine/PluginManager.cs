using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using VirventPluginContract;

namespace VirventSysLogServerEngine
{

    public static class PluginManager
    {
        public static ICollection<IPlugin> LoadPlugins(string path)
        {
            string[] dllFileNames = null;
            if (Directory.Exists(path))
            {
                try
                {
                    dllFileNames = Directory.GetFiles(path, "*.dll");
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("Virvent Syslog Server", ex.Message + "\r\n" + ex.StackTrace);
                }

                ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Length);
                foreach (string dllFile in dllFileNames)
                {
                    try
                    {
                        AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
                        Assembly assembly = Assembly.Load(an);
                        assemblies.Add(assembly);
                    }
                    catch (System.Reflection.ReflectionTypeLoadException ex)
                    {
                        EventLog.WriteEntry("Virvent Syslog Server", ex.Message + "\r\n" + ex.StackTrace, EventLogEntryType.Warning);
                    }
                    catch (Exception ex)
                    {
                        EventLog.WriteEntry("Virvent Syslog Server", ex.Message + "\r\n" + ex.StackTrace, EventLogEntryType.Warning);
                    }
                }

                Type pluginType = typeof(IPlugin);
                ICollection<Type> pluginTypes = new List<Type>();
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly != null)
                    {
                        Type[] types = new Type[] { };

                        try
                        {
                            types = assembly.GetTypes();
                        }
                        catch (System.Reflection.ReflectionTypeLoadException ex)
                        {
                            string exceptions = "";
                            foreach (var i in ex.LoaderExceptions)
                            {
                                exceptions += i.Message + "\r\n" + i.InnerException + "\r\n\r\n" + i.StackTrace;
                            }

                            EventLog.WriteEntry("Virvent Syslog Server", exceptions + "\r\n" + ex.StackTrace, EventLogEntryType.Warning);
                        }
                        catch (Exception ex)
                        {
                            EventLog.WriteEntry("Virvent Syslog Server", ex.Message + "\r\n" + ex.StackTrace, EventLogEntryType.Warning);

                        }

                        foreach (Type type in types)
                        {
                            if (type.IsInterface || type.IsAbstract)
                            {
                                continue;
                            }
                            else
                            {
                                try
                                {
                                    if (type.GetInterface(pluginType.FullName) != null)
                                    {
                                        pluginTypes.Add(type);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    EventLog.WriteEntry("Virvent Syslog Server", ex.Message + "\r\n" + ex.StackTrace, EventLogEntryType.Warning);
                                }
                            }
                        }
                    }
                }

                ICollection<IPlugin> plugins = new List<IPlugin>(pluginTypes.Count);
                foreach (Type type in pluginTypes)
                {
                    try
                    {
                        IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                        plugins.Add(plugin);
                    }
                    catch (System.Reflection.ReflectionTypeLoadException ex)
                    {
                        EventLog.WriteEntry("Virvent Syslog Server", ex.Message + "\r\n" + ex.StackTrace, EventLogEntryType.Warning);
                    }
                    catch (Exception ex)
                    {
                        EventLog.WriteEntry("Virvent Syslog Server", ex.Message + "\r\n" + ex.StackTrace, EventLogEntryType.Warning);

                    }
                }

                return plugins;
            }

            return null;
        }
    }

    public class Plugin
    {

        public string Name { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int AfterStartup { get; set; }
        public int OnMessageEvent { get; set; }
        public long TimeUntilEvent { get; set; }
        public long SecondsSinceLastEvent { get; set; }
        public string ConnectionString { get; set; }
        public List<PluginSetting> Settings { get; set; }
        public IPlugin PluginAssembly { get; set; }

    }

}
