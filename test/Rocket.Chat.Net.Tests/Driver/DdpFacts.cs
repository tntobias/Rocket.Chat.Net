﻿namespace Rocket.Chat.Net.Tests.Driver
{
    using System;

    using FluentAssertions;

    using Newtonsoft.Json;

    using NSubstitute;

    using Ploeh.AutoFixture;

    using Rocket.Chat.Net.Driver;
    using Rocket.Chat.Net.Interfaces;
    using Rocket.Chat.Net.Tests.Helpers;

    using SuperSocket.ClientEngine;

    using WebSocket4Net;

    using Xunit;
    using Xunit.Abstractions;

    public class DdpFacts : IDisposable
    {
        private static readonly Fixture AutoFixture = new Fixture();

        private readonly XUnitLogger _helper;
        private readonly IWebSocketWrapper _socket = Substitute.For<IWebSocketWrapper>();

        public DdpFacts(ITestOutputHelper helper)
        {
            _helper = new XUnitLogger(helper);
        }

        [Fact]
        public void Connecting_should_set_session()
        {
            var client = new DdpClient(_socket, _helper);

            var sessionId = AutoFixture.Create<string>();
            var message = new
            {
                session = sessionId,
                msg = "connected"
            };
            var jsonMessage = new MessageReceivedEventArgs(JsonConvert.SerializeObject(message));

            // Act
            _socket.MessageReceived += Raise.Event<EventHandler<MessageReceivedEventArgs>>(jsonMessage);

            // Assert
            client.SessionId.Should().Be(sessionId);
        }

        [Fact]
        public void Can_reconnect()
        {
            var reconnected = false;
            var client = new DdpClient(_socket, _helper);
            client.DdpReconnect += () => reconnected = true;

            var firstMessage = new MessageReceivedEventArgs(JsonConvert.SerializeObject(new
            {
                session = AutoFixture.Create<string>(),
                msg = "connected"
            }));

            var secondMessage = new MessageReceivedEventArgs(JsonConvert.SerializeObject(new
            {
                session = AutoFixture.Create<string>(),
                msg = "connected"
            }));

            // Act
            _socket.MessageReceived += Raise.Event<EventHandler<MessageReceivedEventArgs>>(firstMessage);
            _socket.Closed += Raise.Event<EventHandler>();
            _socket.MessageReceived += Raise.Event<EventHandler<MessageReceivedEventArgs>>(secondMessage);

            // Assert
            _socket.Received().Open();
            reconnected.Should().BeTrue();
        }

        [Fact]
        public void On_error_log()
        {
            var logger = Substitute.For<ILogger>();
            // ReSharper disable once UnusedVariable
            var client = new DdpClient(_socket, logger);

            var message = AutoFixture.Create<string>();
            var exception = new Exception(message);

            // Act
            _socket.Error += Raise.Event<EventHandler<ErrorEventArgs>>(new ErrorEventArgs(exception));

            // Assert
            logger.Received().Info($"ERROR: {message}");
        }

        public void Dispose()
        {
            _helper.Dispose();
        }
    }
}