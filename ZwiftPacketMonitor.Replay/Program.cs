﻿using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ZwiftPacketMonitor.Replay
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ZwiftPacketMonitor capture replay");

            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: ZwiftPacketMonitor.Replay.exe <path to capture file>");
                Environment.Exit(1);
            }

            var path = args[0];

            if (!File.Exists(path))
            {
                Console.Error.WriteLine("Capture file not found");
                Environment.Exit(1);
            }
            
            var serviceProvider = DependencyRegistration.Register();

            var logger = serviceProvider.GetService<ILogger<Program>>();
            var replay = serviceProvider.GetService<Replayer>();
            
            // Register events for Zwift Companion messages
            var decoder = serviceProvider.GetRequiredService<CompanionPacketDecoder>();
            RegisterZwiftCompanionEvents(decoder, logger);

            // Register events for Zwift Desktop app messages
            RegisterZwiftDesktopEvents(replay, logger);

            // Replay the packet capture
            replay.FromCapture(path);
        }

        private static void RegisterZwiftCompanionEvents(CompanionPacketDecoder decoder, ILogger<Program>? logger)
        {
            decoder.CommandAvailable += (_, eventArgs) =>
            {
                logger.LogInformation("Command {type} is now available", eventArgs.CommandType);
            };

            decoder.CommandSent += (_, eventArgs) =>
            {
                logger.LogInformation("Sent a {type} command", eventArgs.CommandType);
            };
        }

        private static void RegisterZwiftDesktopEvents(Replayer replay, ILogger<Program> logger)
        {
            replay.IncomingPlayerEvent += (s, e) => { logger.LogInformation($"INCOMING: {e.PlayerState}"); };
            replay.OutgoingPlayerEvent += (s, e) => { logger.LogInformation($"OUTGOING: {e.PlayerState}"); };
            replay.IncomingChatMessageEvent += (s, e) => { logger.LogInformation($"CHAT: {e.Message}"); };
            replay.IncomingPlayerEnteredWorldEvent += (s, e) => { logger.LogInformation($"WORLD: {e.PlayerUpdate}"); };
            replay.IncomingRideOnGivenEvent += (s, e) => { logger.LogInformation($"RIDEON: {e.RideOn}"); };
        }
    }
}
