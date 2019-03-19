using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOC2GitHubProcedureScheduler
{
    public class SOC2GitHubProcedureScheduler
    {

    }

    public class SOC2File
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int Years { get; set; }
        public int Months { get; set; }
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public DateTime LastRun { get; set; }
        public string Json { get; set; }
        public string TaskBody { get; set; }

        SOC2File() { }
        SOC2File(string filename)
        {
            StreamReader reader = File.OpenText(filename);
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.Contains("!!"))
                {
                    line = line.Substring(2, line.Length - 2);
                    var lineData = line.Split(":".ToCharArray());
                    if (lineData.Length != 3)
                        break;

                    switch (lineData[1])
                    {
                        case "id":
                            ID = lineData[2];
                            break;
                        case "name":
                            Name = lineData[2];
                            break;
                        case "cron":
                            ParseCronData(lineData[2]);
                            break;
                        case "lastrun":
                            DateTime dateTime;
                            bool parseResult = DateTime.TryParse(lineData[2], out dateTime);
                            if (parseResult)
                                LastRun = dateTime;
                            else
                                LastRun = DateTime.Now;
                            break;
                        default:
                            break;
                    }
                }
                else if (line.Contains("$$"))
                {
                    line = line.Substring(2, line.Length - 2);
                    Json += line;
                }
                else
                {
                    TaskBody += line;
                }
            }
        }

        private void ParseCronData(string cronData)
        {
            var cronStrings = cronData.Split(" ".ToCharArray());
            Years = int.Parse(cronStrings[0]);
            Months = int.Parse(cronStrings[1]);
            Days = int.Parse(cronStrings[2]);
            Hours = int.Parse(cronStrings[3]);
            Minutes = int.Parse(cronStrings[4]);
            Seconds = int.Parse(cronStrings[5]);
        }

    }
}
