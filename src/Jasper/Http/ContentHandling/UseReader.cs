﻿using System;
using System.Reflection;
using Jasper.Conneg;
using Jasper.Http.Model;
using Lamar.Codegen.Frames;
using Lamar.Codegen.Variables;

namespace Jasper.Http.ContentHandling
{
    public class UseReader : MethodCall
    {
        private static MethodInfo selectMethod(Type inputType)
        {
            return typeof(IMessageDeserializer)
                .GetMethod(nameof(IMessageDeserializer.ReadFromRequest))
                .MakeGenericMethod(inputType);

        }

        public UseReader(RouteChain chain, bool isLocal) : base(typeof(IMessageDeserializer), selectMethod(chain.InputType))
        {

            if (isLocal)
            {
                Target = new Variable(typeof(IMessageDeserializer), nameof(RouteHandler.Reader));
            }

            creates.Add(ReturnVariable);
        }
    }
}
