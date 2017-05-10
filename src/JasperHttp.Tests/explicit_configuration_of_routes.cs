﻿using System;
using System.Collections.Generic;
using System.Linq;
using Jasper.Codegen;
using Jasper.Codegen.Compilation;
using Jasper.Configuration;
using JasperHttp.Model;
using Shouldly;
using Xunit;

namespace JasperHttp.Tests
{
    public class explicit_configuration_of_routes
    {
        /*
         * Configure()
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         */

        [Fact]
        public void applies_the_Configure_RoutedChain_method()
        {
            var chain = RouteChain.For<ConfiguredEndpoint>(x => x.get_configured());

            var frames = chain.DetermineFrames();

            frames.OfType<FakeTransaction>().Any().ShouldBeTrue();
        }

        [Fact]
        public void applies_the_Configure_IChain_method()
        {
            var chain = RouteChain.For<ConfiguredEndpoint>(x => x.get_configured());

            var frames = chain.DetermineFrames();

            frames.OfType<FakeWrapper>().Any().ShouldBeTrue();
        }

        [Fact]
        public void applies_attributes_against_the_RouteChain()
        {
            var chain = RouteChain.For<ConfiguredEndpoint>(x => x.get_wrapper2());

            var frames = chain.DetermineFrames();

            frames.OfType<FakeWrapper2>().Any().ShouldBeTrue();
        }

        [Fact]
        public void applies_attributes_against_the_IChain()
        {
            var chain = RouteChain.For<ConfiguredEndpoint>(x => x.get_wrapper3());

            var frames = chain.DetermineFrames();

            frames.OfType<FakeWrapper3>().Any().ShouldBeTrue();
        }
    }

    public class ConfiguredEndpoint
    {
        public void get_configured()
        {
            
        }

        [FakeWrapper2]
        public void get_wrapper2()
        {
            
        }

        [FakeWrapper3]
        public void get_wrapper3()
        {
            
        }

        public static void Configure(RouteChain chain)
        {
            chain.Middleware.Add(new FakeTransaction());
        }

        public static void Configure(IChain chain)
        {
            chain.Middleware.Add(new FakeWrapper());
        }
    }


    public class FakeWrapper : Frame
    {
        public FakeWrapper() : base(false)
        {
        }

        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            
        }
    }

    public class FakeWrapper2Attribute : ModifyRouteAttribute
    {
        public override void Modify(RouteChain chain)
        {
            chain.Middleware.Add(new FakeWrapper2());
        }
    }

    public class FakeWrapper3Attribute : ModifyChainAttribute
    {
        public override void Modify(IChain chain)
        {
            chain.Middleware.Add(new FakeWrapper3());
        }
    }

    public class FakeWrapper2 : FakeWrapper { }
    public class FakeWrapper3 : FakeWrapper { }

    public class GenericFakeTransactionAttribute : ModifyChainAttribute
    {
        public override void Modify(IChain chain)
        {
            chain.Middleware.Add(new FakeTransaction());
        }
    }

    public class FakeTransactionAttribute : ModifyRouteAttribute
    {
        public override void Modify(RouteChain chain)
        {
            chain.Middleware.Add(new FakeTransaction());
        }
    }

    public class FakeTransaction : Frame
    {
        private Variable _store;
        private readonly Variable _session;

        public FakeTransaction() : base(false)
        {
            _session = new Variable(typeof(IFakeSession), "session", this);
        }

        protected override IEnumerable<Variable> resolveVariables(GeneratedMethod chain)
        {
            _store = chain.FindVariable(typeof(IFakeStore));
            yield return _store;
        }

        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            writer.Write($"BLOCK:using (var {_session.Usage} = {_store.Usage}.OpenSession())");
            Next?.GenerateCode(method, writer);
            writer.Write($"{_session.Usage}.{nameof(IFakeSession.SaveChanges)}();");
            writer.FinishBlock();
        }
    }


    public class Tracking
    {
        public bool DisposedTheSession;
        public bool OpenedSession;
        public bool CalledSaveChanges;
    }

    public interface IFakeStore
    {
        IFakeSession OpenSession();
    }

    public class FakeStore : IFakeStore
    {
        private readonly Tracking _tracking;

        public FakeStore(Tracking tracking)
        {
            _tracking = tracking;
        }

        public IFakeSession OpenSession()
        {
            _tracking.OpenedSession = true;
            return new FakeSession(_tracking);
        }
    }

    public interface IFakeSession : IDisposable
    {
        void SaveChanges();
    }

    public class FakeSession : IFakeSession
    {
        private readonly Tracking _tracking;

        public FakeSession(Tracking tracking)
        {
            _tracking = tracking;
        }

        public void Dispose()
        {
            _tracking.DisposedTheSession = true;
        }

        public void SaveChanges()
        {
            _tracking.CalledSaveChanges = true;
        }
    }
}
