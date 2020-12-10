using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ldapmain
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            LdapSearch();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ldapmain", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ldapmain v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void LdapSearch()
        {
            var server = Configuration["LdapServer"];
            var account = Configuration["Account"];
            var password = Configuration["Password"];
            var ldapPort = Convert.ToInt32(Configuration["LdapPort"]);

            using var conn = new LdapConnection() { SecureSocketLayer = false };
            conn.Connect(server, ldapPort);
            conn.Bind(account, password);

            var filter = "(&(objectCategory=person)(objectClass=user)(sAMAccountName=dso))";
            var search = conn.Search("OU=BCGOV,DC=idir,DC=BCGOV", LdapConnection.ScopeSub, filter, new string[] { "bcgovGUID", "sn", "givenname" }, false);

            while (search.HasMore())
            {
                var entry = search.Next();
                Console.WriteLine(entry.GetAttribute("bcgovGUID").StringValue);
                Console.WriteLine(entry.GetAttribute("sn").StringValue);
                Console.WriteLine(entry.GetAttribute("givenname").StringValue);
            }
        }
    }
}
