﻿using System;
using System.Text;
using System.Threading.Tasks;
using Baseline;
using Baseline.Dates;
using Jasper.Conneg;
using Jasper.Messaging;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Tracking;
using Jasper.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace Jasper.Http.Testing.Transport
{
    public class http_transport_end_to_end : SendingContext
    {
        [Fact]
        public async Task http_transport_is_enabled_and_registered()
        {
            await StartTheReceiver(_ =>
            {
                _.Transports.Http.EnableListening(true);

                _.Hosting
                    .UseUrls("http://localhost:5002")
                    .UseKestrel();

                _.Handlers.IncludeType<MessageConsumer>();
                _.Handlers.IncludeType<RequestReplyHandler>();
            });

            var settings = theReceiver.Get<HttpTransportSettings>();
            settings.IsEnabled.ShouldBeTrue();

            theReceiver.Get<IChannelGraph>().ValidTransports.ShouldContain("http");
        }

        [Fact]
        public async Task send_messages_end_to_end_lightweight()
        {
            await StartTheReceiver(_ =>
            {
                _.Transports.Http.EnableListening(true);

                _.Hosting
                    .UseUrls("http://localhost:5002")
                    .UseKestrel();

                _.Handlers.IncludeType<MessageConsumer>();
                _.Handlers.IncludeType<RequestReplyHandler>();
            });

            await StartTheSender(_ =>
            {
                _.Publish.Message<Message1>().To("http://localhost:5002/messages");
                _.Publish.Message<Message2>().To("http://localhost:5002/messages");
            });

            var waiter1 = theTracker.WaitFor<Message1>();
            var waiter2 = theTracker.WaitFor<Message2>();

            var message1 = new Message1 {Id = Guid.NewGuid()};
            var message2 = new Message2 {Id = Guid.NewGuid()};

            await theSender.Messaging.Send(message1);
            await theSender.Messaging.Send(message2);

            waiter1.Wait(10.Seconds());
            waiter2.Wait(10.Seconds());

            waiter1.Result.Message.As<Message1>()
                .Id.ShouldBe(message1.Id);
        }
    }

    // SAMPLE: HttpTransportUsingApp
    public class HttpTransportUsingApp : JasperRegistry
    {
        public HttpTransportUsingApp()
        {
            Publish.AllMessagesTo("http://server1/messages");

            // Or

            Publish.AllMessagesTo("http://server1/messages/durable");
        }
    }
    // ENDSAMPLE

    public abstract class SendingContext : IDisposable
    {
        private readonly JasperRegistry receiverRegistry = new JasperRegistry();
        private readonly JasperRegistry senderRegistry = new JasperRegistry();
        protected JasperRuntime theReceiver;
        protected JasperRuntime theSender;
        protected MessageTracker theTracker;

        public SendingContext()
        {
            theTracker = new MessageTracker();
            receiverRegistry.Handlers
                .DisableConventionalDiscovery()
                .IncludeType<MessageConsumer>();

            receiverRegistry.Services.For<MessageTracker>().Use(theTracker);
        }


        public void Dispose()
        {
            theSender?.Dispose();
            theReceiver?.Dispose();
        }

        protected async Task StartTheSender(Action<JasperRegistry> configure)
        {
            configure(senderRegistry);
            theSender = await JasperRuntime.ForAsync(senderRegistry);
        }


        protected async Task StartTheReceiver(Action<JasperRegistry> configure)
        {
            configure(receiverRegistry);
            theReceiver = await JasperRuntime.ForAsync(receiverRegistry);
        }
    }

    public class RequestReplyHandler
    {
        public Reply1 Handle(Request1 request, Envelope envelope)
        {
            return new Reply1
            {
                Sum = request.One + request.Two
            };
        }
    }

    public class Request1
    {
        public int One { get; set; }
        public int Two { get; set; }
    }


    public class Reply1Reader : IMessageDeserializer
    {
        public static bool WasUsed { get; set; }
        public string MessageType { get; } = typeof(Reply1).ToMessageAlias();
        public Type DotNetType { get; } = typeof(Reply1);
        public string ContentType { get; } = "text/plain";

        public object ReadFromData(byte[] data)
        {
            WasUsed = true;

            var text = Encoding.UTF8.GetString(data);
            return new Reply1
            {
                Sum = int.Parse(text)
            };
        }

        public Task<T> ReadFromRequest<T>(HttpRequest request)
        {
            throw new NotSupportedException();
        }
    }

    public class Reply1Writer : IMessageSerializer
    {
        public static bool WasUsed { get; set; }
        public Type DotNetType { get; } = typeof(Reply1);
        public string ContentType { get; } = "text/plain";

        public byte[] Write(object model)
        {
            WasUsed = true;
            var text = model.As<Reply1>().Sum.ToString();
            return Encoding.UTF8.GetBytes(text);
        }

        public Task WriteToStream(object model, HttpResponse response)
        {
            throw new NotSupportedException();
        }
    }

    public class Reply1
    {
        public int Sum { get; set; }
    }

    public class MessageConsumer
    {
        private readonly MessageTracker _tracker;

        public MessageConsumer(MessageTracker tracker)
        {
            _tracker = tracker;
        }

        public void Consume(Envelope envelope, Message1 message)
        {
            _tracker.Record(message, envelope);
        }

        public void Consume(Envelope envelope, Message2 message)
        {
            if (envelope.Attempts < 2)
            {
                throw new DivideByZeroException();
            }

            _tracker.Record(message, envelope);
        }

        public void Consume(Envelope envelope, TimeoutsMessage message)
        {
            if (envelope.Attempts < 2)
            {
                throw new TimeoutException();
            }

            _tracker.Record(message, envelope);
        }
    }

    public class TimeoutsMessage
    {

    }
}
