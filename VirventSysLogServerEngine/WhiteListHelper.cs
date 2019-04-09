using System;
using System.Collections.Generic;
using System.IO;

namespace VirventSysLogServerEngine
{
    public class WhiteListHelper
    {
        public string[] WhiteList;

        public WhiteListHelper(string path) : base()
        {
            try
            {
                List<string> whiteList = new List<string>();

                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    // read file
                    StreamReader reader = File.OpenText(file);
                    while (!reader.EndOfStream)
                    {
                        var item = reader.ReadLine();
                        if (item.Length > 0 && item.Substring(0,1) != @"#")
                        whiteList.Add(item.Trim());
                    }
                    reader.Close();
                    reader.Dispose();
                }
                var wl = whiteList.ToArray();
                Array.Sort(wl);
                WhiteList = wl;
            }
            catch
            {
                // failed to load white list
                WhiteList = new string[0];

            }
        }

        public bool IsWhiteListed(string address)
        {
            var inArray = Array.BinarySearch(WhiteList, address);
            if (inArray >= 0)
                return true;
            return false;
        }
    }



}
