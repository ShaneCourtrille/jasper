﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Baseline;
using Jasper.Util;
using Newtonsoft.Json;

namespace Jasper.Messaging.Runtime.Subscriptions
{
    public class Subscription
    {
        public Subscription(Type messageType, Uri destination)
        {
            // Okay to let destination be null here.
            Destination = destination;
            MessageType = messageType?.ToMessageAlias() ?? throw new ArgumentNullException(nameof(messageType));
            DotNetType = messageType;
        }

        [JsonIgnore]
        public Type DotNetType { get; }

        // for serialization
        public Subscription()
        {
        }

        public string GetId() => $"{MessageType}/{WebUtility.UrlEncode(Destination.ToString())}";

        public Uri Destination { get; set; }

        public string MessageType { get; set; }

        public string ServiceName { get; set; }

        private readonly IList<string> _accepts = new List<string>();

        public string[] Accept
        {
            get => _accepts.ToArray();
            set
            {
                _accepts.Clear();
                if (value != null)
                {
                    _accepts.AddRange(value.OrderBy(x => x));
                }
                else
                {
                    _accepts.Add("application/json");
                }
            }

        }

        protected bool Equals(Subscription other)
        {
            return Equals(Destination, other.Destination) && string.Equals(MessageType, other.MessageType) && string.Equals(ServiceName, other.ServiceName) && Accept.SequenceEqual(other.Accept);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Subscription) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Destination != null ? Destination.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (MessageType != null ? MessageType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ServiceName != null ? ServiceName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Destination)}: {Destination}, {nameof(MessageType)}: {MessageType}, {nameof(ServiceName)}: {ServiceName}, {nameof(Accept)}: {Accept}";
        }
    }
}
