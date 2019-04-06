using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFwTypeLib;

namespace WindowsFirewallAutoRulePlugin
{
    public class FirewallHandler
    {

        public static void CheckMessage()
        {

        }

        public static bool CreateBlockRule(string IPAddress)
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRule.Description = "Virvent->Snort Auto-Ban " + IPAddress;
            firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN; // inbound
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.RemoteAddresses = IPAddress; // add more blocks comma separated
            firewallRule.Name = "Virvent->Snort Auto-Ban " + IPAddress;
            try { 
                fwPolicy2.Rules.Add(firewallRule);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to create firewall rule");
                return false;
            }
        }

        public static bool CheckForRule(string IPAddress)
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
            var currentProfiles = fwPolicy2.CurrentProfileTypes;

            List<INetFwRule> RuleList = new List<INetFwRule>();

            foreach (INetFwRule rule in fwPolicy2.Rules)
            {
                // Add rule to list
                //RuleList.Add(rule);
                // Console.WriteLine(rule.Name);
                if (rule.Name.IndexOf("Virvent->Snort Auto-Ban " + IPAddress) != -1)
                    return true;
            }
            return false;
        }
    }
}
