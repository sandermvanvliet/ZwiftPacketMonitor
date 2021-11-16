# ZwiftPacketMonitor

This project implements a TCP and UDP packet monitor for the Zwift cycling simulator. It listens for packets on a specific port of a local network adapter, and when found, deserializes the payload and dispatches events that can be consumed by the caller.

**NOTE**: Because this utilizes a network packet capture to intercept the UDP packets, your system may require this code to run using elevated privileges.

## Prerequisites

* Packet capture relies on [SharpPcap](https://github.com/chmorgan/sharppcap) and requires the installation of libpcap (Linux), Npcap (Windows) or similar packet capture library.

## Usage

See the included ZwiftPacketMonitor.Demo project for a complete working example.

```c#
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddZwiftPacketMonitoring();

    var serviceProvider = serviceCollection.BuildServiceProvider(); 
    var logger = serviceProvider.GetService<ILogger<Program>>();
    var monitor = serviceProvider.GetService<Monitor>();

    monitor.IncomingPlayerEvent += (s, e) => {
        logger.LogInformation($"INCOMING: {e.PlayerState}");
    };
    monitor.OutgoingPlayerEvent += (s, e) => {
        logger.LogInformation($"OUTGOING: {e.PlayerState}");
    };
    monitor.IncomingChatMessageEvent += (s, e) => {
        logger.LogInformation($"CHAT: {e.Message}");
    };
    monitor.IncomingPlayerEnteredWorldEvent += (s, e) => {
        logger.LogInformation($"WORLD: {e.PlayerUpdate}");
    };
    monitor.IncomingRideOnGivenEvent += (s, e) => {
        logger.LogInformation($"RIDEON: {e.RideOn}");
    };

    // network interface name or IP address
    monitor.StartCaptureAsync("en0").Wait();
    
    // This won't get called until the above Wait finishes
    monitor.StopCaptureAsync().Wait();
```

## Zwift Companion messages

In addition to capturing messages from the Zwift Desktop app (the actual game) it is also possible to listen for the messages sent to and from the Zwift Companion app running on a mobile device.

This is handled through the `CompanionPacketDecoder` class which exposes the various events that will be raised when the relevant message is captured.

```csharp
var serviceCollection = new ServiceCollection();
serviceCollection.AddZwiftPacketMonitoring();

var serviceProvider = serviceCollection.BuildServiceProvider(); 
var logger = serviceProvider.GetService<ILogger<Program>>();
var monitor = serviceProvider.GetService<Monitor>();
var companionPacketDecoder = serviceProvider.GetService<CompanionPacketDecoder>();

companionPacketDecoder.CommandAvailable += (_, eventArgs) =>
{
    logger.LogInformation("Command {type} is now available", eventArgs.CommandType);
};

companionPacketDecoder.CommandSent += (_, eventArgs) =>
{
    logger.LogInformation("Sent a {type} command", eventArgs.CommandType);
};

// network interface name or IP address
monitor.StartCaptureAsync("en0").Wait();

// This won't get called until the above Wait finishes
monitor.StopCaptureAsync().Wait();
```

A number of the messages have been reverse engineerd but not everything yet. See [zwiftCompanionMessages.proto](src/zwiftCompanionMessages.proto) for details.

## Replay Zwift messages

Debugging is hard but it's even harder when having to do a Zwift work out at the same time!

To make life easier you can replay Zwift messages from a PCAP capture file instead of a network device so that you can debug and analyse without having to run Zwift at the same time.
Capture packets using SharpCap or Wireshark and save the results to a file, then run `ZwiftPacketMonitor.Replay.exe <path to capture file>` and see the messages flow by.

You can also use the `Monitor` class directly by providing the full path to a capture file to the `StartCapture` method:

```csharp
// Replay the packet capture
var path = "c:\temp\zwift-captures\capture-session-1.pcap";
await monitor.StartCaptureAsync(path)
```

## Generating message classes

After making changes in the Protobuf files the C# classes can be regenerated by running the `generate-protos.ps1` or `generate-protos.sh` script.

For this you will need the Protobuf CLI tool which can be obtained via the Protobuf [GitHub release page](https://github.com/protocolbuffers/protobuf/releases)

## Credit

This project is a .NET port of the [zwift-packet-monitor](https://github.com/jeroni7100/zwift-packet-monitor) project and borrows heavily from its packet handling and protobuf implementation.

