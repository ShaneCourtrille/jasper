﻿using System.Collections.Generic;
using Jasper.Messaging.Transports;
using Jasper.SqlServer.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace Jasper.SqlServer.Tests
{
    public class configuration_extension_methods
    {



        [Fact]
        public void bootstrap_with_connection_string()
        {
            using (var runtime = JasperRuntime.For(x =>
                x.Settings.PersistMessagesWithSqlServer(ConnectionSource.ConnectionString)))
            {
                runtime.Container.Model.DefaultTypeFor<IDurableMessagingFactory>()
                    .ShouldBe(typeof(SqlServerBackedDurableMessagingFactory));

                runtime.Get<SqlServerSettings>()
                    .ConnectionString.ShouldBe(ConnectionSource.ConnectionString);
            }
        }

        [Fact]
        public void bootstrap_with_configuration()
        {
            var registry = new JasperRegistry();
            registry.Configuration.AddInMemoryCollection(new Dictionary<string, string> {{"connection", ConnectionSource.ConnectionString}});

            registry.Settings.PersistMessagesWithSqlServer((c, s) =>
                {
                    s.ConnectionString = c.Configuration["connection"];
                });

            using (var runtime = JasperRuntime.For(registry))
            {
                runtime.Container.Model.DefaultTypeFor<IDurableMessagingFactory>()
                    .ShouldBe(typeof(SqlServerBackedDurableMessagingFactory));

                runtime.Get<SqlServerSettings>()
                    .ConnectionString.ShouldBe(ConnectionSource.ConnectionString);
            }
        }
    }

    // SAMPLE: AppUsingSqlServer
    public class AppUsingSqlServer : JasperRegistry
    {
        public AppUsingSqlServer()
        {
            // If you know the connection string
            Settings.PersistMessagesWithSqlServer("your connection string", schema:"my_app_schema");

            // Or using application configuration
            Settings.PersistMessagesWithSqlServer((context, settings) =>
            {
                if (context.HostingEnvironment.IsDevelopment())
                {
                    // if so desired, the context argument gives you
                    // access to both the IConfiguration and IHostingEnvironment
                    // of the running application, so you could do
                    // environment specific configuration here
                }

                settings.ConnectionString = context.Configuration["sqlserver"];

                // If your application uses a schema besides "dbo"
                settings.SchemaName = "my_app_schema";

                // If you're using a database principal that is NOT "dbo":
                settings.DatabasePrincipal = "not_dbo";
            });
        }
    }
    // ENDSAMPLE
}
