﻿using System;
using System.Linq;
using System.Management;

namespace AtYourService
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] hosts = { "localhost" };
            // Test if input arguments were supplied
            if (args.Length > 0)
            {
                hosts = args[0].Split(',');
            }

            string ns = @"root\cimv2";
            foreach (string host in hosts)
            {
                ManagementScope scope = new ManagementScope(string.Format(@"\\{0}\{1}", host, ns));
                //https://docs.microsoft.com/en-us/windows/win32/wmisdk/wql-operators
                //https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-service
                SelectQuery query = new SelectQuery("SELECT name,displayname,startname,description,systemname FROM Win32_Service WHERE startname IS NOT NULL");

                // Requires Local Admin. Try catch for insufficient privs, network connectivity issues, etc
                try
                {
                    Console.WriteLine("[+] Connecting to {0}", host);
                    scope.Connect();
                    //https://stackoverflow.com/questions/842533/in-c-sharp-how-do-i-query-the-list-of-running-services-on-a-windows-server
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                    {
                        Console.WriteLine("[+] Enumerating services...");
                        ManagementObjectCollection services = searcher.Get();
                        Console.WriteLine("[+] Found {0} services running...", services.Count);
                        Console.WriteLine("[+] Filtering out LocalSystem and NT Authority Account services...");
                        bool results = false;
                        foreach (ManagementObject service in services)
                        {
                            // Exclude services running as local accounts
                            string[] exclusions = { "LOCALSYSTEM", "NT AUTHORITY\\LOCALSERVICE", "NT AUTHORITY\\NETWORKSERVICE" };
                            if (!exclusions.Contains(service["StartName"].ToString().ToUpper()))
                            {
                                Console.WriteLine("\t[+] Service:\t {0}", service["Name"]);
                                Console.WriteLine("\t    Name:\t {0}", service["DisplayName"]);
                                Console.WriteLine("\t    Account:\t {0}", service["StartName"]);
                                Console.WriteLine("\t    Description: {0}", service["Description"]);
                                Console.WriteLine("\t    System:\t {0}", service["SystemName"]);
                                results = true;
                            }
                        }
                        if (!results)
                        {
                            Console.WriteLine("[!] No other services identified on {0}", host);
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[!] " + ex.Message);
                    Console.WriteLine("[!] Unable to query services on {0}", host);
                    continue;
                }
            }
            Console.WriteLine("[+] Finished");
        }
    }
}
