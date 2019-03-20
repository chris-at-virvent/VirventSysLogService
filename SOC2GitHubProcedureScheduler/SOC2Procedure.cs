using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOC2GitHubProcedureScheduler
{
    public class SOC2Procedure
    {
        public string FileName { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public string Cron { get; set; }
        public int Years { get; set; }
        public int Months { get; set; }
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public DateTime LastRun { get; set; }
        public List<string> RawJSON { get; set; }
        public string Json { get; set; }
        public string TaskBody { get; set; }

        public SOC2Procedure() { }
        public SOC2Procedure(string path, string filename)
        {
            RawJSON = new List<string>();
            FileName = filename;
            StreamReader reader = File.OpenText(FileName);
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.Contains("!!"))
                {
                    //line = line.Substring(2, line.Length - 2);
                    var lineData = line.Split("|".ToCharArray());
                    if (lineData.Length < 3)
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
                            Cron = lineData[2];
                            ParseCronData(Cron);
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
                    RawJSON.Add(line);
                }
                else
                {                    
                    TaskBody += line.Replace("\r\n","") + "\\n\\n";
                }
            }

            // do the json replacement for the task body
            Json = Json.Replace("{{TaskBody}}", TaskBody);

            reader.Close();
            reader.Dispose();
        }

        // saves the template data back to the file system
        public bool Save()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("!!|id|" + this.ID);
            sb.AppendLine("!!|name|" + this.Name);
            sb.AppendLine("!!|cron|" + this.Cron);
            sb.AppendLine("!!|lastrun|" + this.LastRun);
            sb.AppendLine("");

            foreach (var i in RawJSON)
            {
                sb.AppendLine("$$" + i);
            }

            sb.AppendLine(TaskBody);

            File.Delete(FileName);
            var file = File.CreateText(FileName);
            file.Write(sb.ToString());
            file.Flush();
            file.Close();
            file.Dispose();

            return true;
        }

        public bool Runnable()
        {
            var nextRun = LastRun.
                AddYears(Years).
                AddMonths(Months).
                AddDays(Days).
                AddHours(Hours).
                AddMinutes(Minutes).
                AddSeconds(Seconds);

            if (nextRun < DateTime.Now)
                return true;

            return false;
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
