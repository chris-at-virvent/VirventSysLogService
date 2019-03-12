# Virvent SysLog Service
Syslog Server and Console app for Snort/Windows Implementations

There are two libraries and two executables included with this project.

## VirventSysLogConsole

Designed as a test harness, the Virvent SysLog Console can be used to run the SysLog server in an interactive mode for testing. Helpful when first configuring Snort or other IDS solution.

## VirventSysLogServer

Designed to be run as a Windows service with logging to SQL Server, this has an additional function that will have the SysLog server check to see if snort (or any other process) is running and log an alert if the IDS solution is not running.

These are still in the very early stages of testing in a production environment. Feedback is welcome.
