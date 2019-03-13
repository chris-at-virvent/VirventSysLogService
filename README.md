# Virvent SysLog Service
Syslog Server and Console app for Snort/Windows Implementations

There are two libraries and two executables included with this project.

## VirventSysLogConsole

Designed as a test harness, the Virvent SysLog Console can be used to run the SysLog server in an interactive mode for testing. Helpful when first configuring Snort or other IDS solution.

## VirventSysLogServer

Designed to be run as a Windows service with logging to SQL Server, this has an additional function that will have the SysLog server check to see if snort (or any other process) is running and log an alert if the IDS solution is not running.

These are still in the very early stages of testing in a production environment. Feedback is welcome.

## Configuration

The appname.exe.config file is used for application configuration. Available settings are:

  * PortNumber: The port to listen on
  * IPAddressToListen: IP Address to listen on
  * Protocol: Comma-separated list of protocols to montor. Supports TCP and UDP
  * ProcessCheckFrequency: How long between checks (60 = 1 minute).
  * ProcessCheckInterval: How many check cycles until processes are validated.
  * LogLevel: 0-7 - conforms to RFC 5424 PRI values
  * ProcessesToMonitor section
    * Add a single name here for each process the syslog system should watch.
    * Notifications will be generated in the SysLog database for each process status.

## Console Installation

The console application requires no installation to run.

## Service Installation

The service installation requires administrative privilages and requires the installutil.exe application that comes with the .NET framework. Sample syntax:

```
installutil.exe "VirventSysLogService.exe" /i       (installation)
installutil.exe "VirventSysLogService.exe" /u       (removal)
```
