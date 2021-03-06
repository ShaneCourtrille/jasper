﻿using System.Threading.Tasks;
using Baseline;
using Jasper.Messaging;
using Jasper.Messaging.Tracking;
using StoryTeller;

namespace Jasper.Storyteller
{
    public abstract class MessagingFixture : Fixture
    {
        /// <summary>
        /// The service bus for the currently running application
        /// </summary>
        protected IMessageContext Bus => Context.Service<IMessageContext>();

        protected MessageHistory History => Context.Service<MessageHistory>();

        /// <summary>
        /// Send a message and wait for all detected activity within the bus
        /// to complete
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected Task SendMessageAndWaitForCompletion(object message)
        {
            return History.WatchAsync(() => Bus.Send(message));
        }

        /// <summary>
        /// Send a message from an external node and wait for all detected activity within the bus
        /// to complete
        /// </summary>
        /// <param name="message"></param>
        /// <param name="nodeName">The service name of another, external node</param>
        /// <returns></returns>
        protected Task SendMessageAndWaitForCompletion(string nodeName, object message)
        {
            return History.WatchAsync(() => NodeFor(nodeName).Send(message));
        }

        /// <summary>
        /// Find the
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        protected ExternalNode NodeFor(string nodeName)
        {
            return Context.Service<INodes>().NodeFor(nodeName);
        }
    }
}
