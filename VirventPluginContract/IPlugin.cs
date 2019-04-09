﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VirventPluginContract
{
    public interface IPlugin
    {
        string Name { get; }

        void Run(List<PluginSetting> settings, Message message, out List<PluginMessage> messages);
    }
}