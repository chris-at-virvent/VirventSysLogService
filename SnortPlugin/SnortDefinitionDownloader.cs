using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using VirventPluginContract;

namespace SnortPlugin
{
    public class SnortDefinitionDownloader
    {

        public static bool GetDefinitions(SnortSettings settings, out List<PluginMessage> Responses)
        {
            List<PluginMessage> pluginMessages = new List<PluginMessage>();

            // remove all prior downloaded rules
            if (Directory.Exists(settings.DownloadPath))
                Directory.Delete(settings.DownloadPath, true);
            if (Directory.Exists(settings.UnpackPath))
                Directory.Delete(settings.UnpackPath, true);

            Directory.CreateDirectory(settings.DownloadPath);
            Directory.CreateDirectory(settings.UnpackPath);

            // make a GET request to the download site
            string SnortServer = settings.SnortRemoteServer;
            SnortServer += settings.SnortVersion + ".tar.gz";
            if (settings.OinkCode.Length > 0)
                SnortServer += "?oinkcode=" + settings.OinkCode;

            WebClient webClient = new WebClient();
            webClient.Credentials = new NetworkCredential();

            webClient.DownloadFile(SnortServer, settings.DownloadPath + "\\rules.tar.gz");

            // store the downloaded file into a directory
           pluginMessages.Add( new PluginMessage()
            {
                msg = "Rules Downloaded. Decompressing...",
                severity = Severities.Informational,
                facility = Facilities.log_audit
            });

            // unzip the archive to the rules directory
            Unpack(settings.DownloadPath + "\\rules.tar.gz", settings.UnpackPath);

            // send command to stop and restart snort services
            Console.WriteLine("Decompressed. Reloading Snort services.");
            pluginMessages.Add(new PluginMessage()
            {
                msg = "Decompression complete.",
                severity = Severities.Informational,
                facility = Facilities.log_audit
            });

            Responses = pluginMessages;
            return true;
        }

        public static bool SnortRewrite(SnortSettings settings, out List<PluginMessage> Responses)
        {
            List<PluginMessage> pluginMessages = new List<PluginMessage>();
            pluginMessages.Add(new PluginMessage()
            {
                msg = "Parsing Rules Files For New Rules / Definitions",
                severity = Severities.Informational,
                facility = Facilities.log_audit
            });
            // Pick and choose files to copy - we can't use the default "snort.conf" file and wouldn't want to
            // we will parse the snort conf file to find out what rules to use   

            List<Tuple<string, string, bool>> rules = new List<Tuple<string, string, bool>>();
            List<Tuple<string, string, bool>> so_rules = new List<Tuple<string, string, bool>>();
            List<Tuple<string, string, bool>> preproc_rules = new List<Tuple<string, string, bool>>();

            // Load the new snort file and get a list of all lines that contain a rule
            StreamReader downloadedFile = File.OpenText(settings.UnpackPath + "\\etc\\snort.conf");
            while (!downloadedFile.EndOfStream)
            {
                string line = downloadedFile.ReadLine();

                if (line.Contains("include $RULE_PATH"))
                {
                    var thisRule = ParseRule(line);
                    if (thisRule != null)
                        rules.Add(thisRule);
                }

                if (line.Contains("include $PREPROC_RULE_PATH"))
                {
                    var thisRule = ParseRule(line);
                    if (thisRule != null)
                        preproc_rules.Add(thisRule);
                }
                if (line.Contains("include $SO_RULE_PATH"))
                {
                    var thisRule = ParseRule(line);
                    if (thisRule != null)
                        so_rules.Add(thisRule);
                }
            }


            StringBuilder sbNewConfFile = new StringBuilder();

            // Load the existing snort.conf file
            StreamReader currentFile = File.OpenText(settings.SnortPath + "\\etc\\snort.conf");
            while (!currentFile.EndOfStream)
            {
                string currentLine = currentFile.ReadLine();
               // Console.WriteLine(currentLine);

                // parse the line for a valid rule
                RuleType ruleType = IsRule(currentLine);
                if (ruleType != RuleType.NotARule)
                {
                    // valid rule
                    var currentRule = ParseRule(currentLine, false);
                    List<Tuple<string, string, bool>> newRules = new List<Tuple<string, string, bool>>();
                    List<Tuple<string, string, bool>> ruleSet = new List<Tuple<string, string, bool>>();
                    switch (ruleType)
                    {
                        case RuleType.Rule:
                            ruleSet = rules;
                            newRules = rules.Where(x => x.Item2 == currentRule.Item2).ToList();
                            break;
                        case RuleType.SO_Rule:
                            ruleSet = so_rules;
                            newRules = so_rules.Where(x => x.Item2 == currentRule.Item2).ToList();
                            break;
                        case RuleType.PreProc_Rule:
                            ruleSet = preproc_rules;
                            newRules = preproc_rules.Where(x => x.Item2 == currentRule.Item2).ToList();
                            break;
                        default:
                            break;
                    }



                    if (newRules.Count != 0)
                    {
                        var newRule = newRules[0];

                        // if current rule is enabled and new rule is disabled, take our config file
                        // if the new rule is enabled and the current rule is disabled, take the new rule
                        // otherwise, take the current rule
                        if (currentRule.Item3 && !newRule.Item3)
                            sbNewConfFile.AppendLine(RuleToString(currentRule));
                        else if (!currentRule.Item3 && newRule.Item3)
                            sbNewConfFile.AppendLine(RuleToString(newRule));
                        else
                            sbNewConfFile.AppendLine(RuleToString(currentRule));

                        // rule processed - remove from queue
                        ruleSet.Remove(newRule);
                    }
                    else
                    {
                        // this rule is invalid now - we need to mark the rule as invalid
                        sbNewConfFile.AppendLine(RuleToString(new Tuple<string, string, bool>(currentRule.Item1, currentRule.Item2 + " # Removed by auto-tool on " + DateTime.Now.ToRfc3339String(), false)));
                    }
                }
                else
                {
                    // not a rule line - append to string builder as-is
                    sbNewConfFile.AppendLine(currentLine);
                }
            }

            // we are done with all the old rules processing.
            // if there are new rules left over, let's import them in

            if (rules.Count > 0)
            {
                sbNewConfFile.AppendLine("# NEW RULES FROM IMPORT ON " + DateTime.Now.ToRfc3339String());
                foreach (var rule in rules)
                {
                    sbNewConfFile.AppendLine(RuleToString(rule));
                }
            }

            if (so_rules.Count > 0)
            {
                sbNewConfFile.AppendLine("# NEW SO RULES FROM IMPORT ON " + DateTime.Now.ToRfc3339String());
                foreach (var rule in so_rules)
                {
                    sbNewConfFile.AppendLine(RuleToString(rule));
                }
            }

            if (preproc_rules.Count > 0)
            {
                sbNewConfFile.AppendLine("# NEW PREPROC RULES FROM IMPORT ON " + DateTime.Now.ToRfc3339String());
                foreach (var rule in preproc_rules)
                {
                    sbNewConfFile.AppendLine(RuleToString(rule));
                }
            }

            // close streams
            currentFile.Close();
            downloadedFile.Close();

            //overwrite the existing conf file with the new conf file
            var newFile = new StreamWriter(settings.UnpackPath + "\\etc\\snort.conf");
            newFile.WriteLine(sbNewConfFile.ToString());
            newFile.Close();

            pluginMessages.Add(new PluginMessage()
            {
                msg = "Rules parsed - ready to restart snort.",
                severity = Severities.Informational,
                facility = Facilities.log_audit
            });

            Responses = pluginMessages;

            return true;
        }

        public static void RestartService(string servicename, out List<PluginMessage> Responses)
        {
            List<PluginMessage> pluginMessages = new List<PluginMessage>();

            ServiceController service = new ServiceController(servicename);
            try
            {
                int millisec1 = Environment.TickCount;
                TimeSpan timeout = TimeSpan.FromMilliseconds(10000);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                // count the rest of the timeout
                int millisec2 = Environment.TickCount;
                timeout = TimeSpan.FromMilliseconds(10000 - (millisec2 - millisec1));

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);

                pluginMessages.Add(new PluginMessage()
                {
                    msg = "Service Restarted.",
                    severity = Severities.Informational,
                    facility = Facilities.log_audit
                });
            }
            catch (Exception ex)
            {
                pluginMessages.Add(new PluginMessage()
                {
                    msg = "Service FAILED.",
                    severity = Severities.Emergency,
                    facility = Facilities.kernel_messages
                });
            }

            Responses = pluginMessages;
            return;
        }

        public static RuleType IsRule(string rule)
        {
            if (rule.Contains("include $RULE_PATH"))
                return RuleType.Rule;
            if (rule.Contains("include $PREPROC_RULE_PATH"))
                return RuleType.PreProc_Rule;
            if (rule.Contains("include $RULE_PATH"))
                return RuleType.SO_Rule;

            return RuleType.NotARule;
        }

        public static Tuple<string, string, bool> ParseRule(string rule, bool linuxFormat = true)
        {
            try
            {
                var enabledByDefault = false;
                if (!rule.Contains("#"))
                    enabledByDefault = true;

                rule = rule.Replace("# ", "");
                string[] splitRule;

                if (linuxFormat)
                    splitRule = rule.Split(new string[] { "/" }, StringSplitOptions.None);
                else
                    splitRule = rule.Split(new string[] { "\\" }, StringSplitOptions.None);

                if (splitRule.Length > 0)
                    return new Tuple<string, string, bool>(splitRule[0], splitRule[1], enabledByDefault);

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string RuleToString(Tuple<string, string, bool> rule)
        {
            // we add this rule but in windows config format
            var commentRule = rule.Item3 == true ? "" : "# ";
            return commentRule + rule.Item1 + "\\" + rule.Item2;
        }

        public enum RuleType
        {
            NotARule,
            Rule,
            SO_Rule,
            PreProc_Rule
        }

        public static void Unpack(string gzArchiveName, string destFolder)
        {
            Stream inStream = File.OpenRead(gzArchiveName);
            Stream gzipStream = new GZipInputStream(inStream);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
            tarArchive.ExtractContents(destFolder);
            tarArchive.Close();

            gzipStream.Close();
            inStream.Close();
        }
    }

}

