# Network UPS Tools Service for Windows

WinNUT-Service is a Windows Service written in .NET. It monitors a UPS connected to a [Network UPS Tools] server and can be configured to shut down the PC when the target UPS switches to battery and falls below a specified charge or battery time remaining threshold. 

This repository also contains a Windows Forms application which lives in the notification area and provides a GUI interface for the service, with a UPS battery status icon and information. 

The NUT server communications in the service are provided via [gbakeman](https://github.com/gbakeman)'s [nutdotnet] repository. 

## Background

In my office, I have a Synology NAS and a desktop PC both connected to an APC uninterruptible power supply. The UPS has a USB connection directly to the Synology, which provides a built-in NUT server; allowlisted devices on the local network can query the NUT server for the status of the UPS battery, and the Synology can power itself down safely if the UPS spends too long on battery power (i.e., if the power in our home goes out). 

I also want the desktop PC to power down safely if I'm not present when the power goes out. While there are some desktop applications which can monitor a NUT server and shut down the PC when the power goes out, I couldn't find a background service implementation that did this. So I wrote this one.

## Parts

The Windows Service implementation lives in the WinNUT.ClientService project. It provides a Windows Service which can be installed with [sc.exe] and configured with a battery charge threshold and a "battery time remaining" threshold. The service queries the configured NUT server at a configurable interval for the status, charge, and remaining time of the target UPS. If the UPS is on battery and goes below either threshold, the service initiates a system shutdown (with a configurable grace period). 

The notification tray GUI lives in WinNUT.ClientGUI. It is a simple Windows Forms application which sits in the notification tray and displays an icon indicating the battery status. If also provides additional UPS information via a menu option. If the service initiates a shutdown, the GUI will pop up a message box allowing the user to request a shutdown delay to complete their current tasks. 

## Settings

The service settings live in [appsettings.json](https://github.com/hartez/WinNUT-Service/blob/e83eb40b43f8b7607ecf7b5eea6a7b643f20dbb2/WinNUT.ClientService/appsettings.json).

### NUTClientConfig

| Setting | Default | Description |
|---------|---------|-------------| 
| Host | localhost | The IP address of the NUT server |
| Port | 3493 | The port of the NUT server |

### MonitorConfig

| Setting | Default | Description |
|---------|---------|-------------| 
| PollingIntervalSeconds | 5 | How often the service will query the UPS status |
| ReconnectAttemptIntervalSeconds | 60 | If the service loses connection with the NUT server, it will attempt to reconnect after this interval |
| ChargeThreshold | 50 | The minimum charge at which a shutdown will be initiated |
| RemainingThresholdSeconds | 300 | If the UPS remaining battery power time drops below this value, a shutdown will be initiated |
| ShutdownDelaySeconds | 180 | The amount of time after shutdown is triggered before the machine will shut down |

## Other Stuff

Communication between the GUI and the service is handled by gRPC over named pipes.

The repository also contains a project called WinNUT.TestServerGUI --- a Windows Forms application which runs a mock NUT server with configurable UPS readings. Useful for testing service and client GUI behavior. 

This is a very specific implementation to address my (somewhat unusual) office configuration. I'm making it available here in case any part of it might be useful for someone else working with these technologies. 

[Network UPS Tools]: https://networkupstools.org/index.html
[nutdotnet]: https://github.com/nutdotnet/nutdotnet
[sc.exe]: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/sc-create
