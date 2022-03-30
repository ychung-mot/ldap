using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ldapmain
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            LdapSearch("sAMAccountName", "dso");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private AdAccount LdapSearch(string filterAttr, string value)
        {
            var server = Configuration["LdapServer"];
            var account = Configuration["Account"];
            var password = Configuration["Password"];
            var ldapPort = Convert.ToInt32(Configuration["LdapPort"]);

            using var conn = new LdapConnection() { SecureSocketLayer = false };
            conn.Connect(server, ldapPort);
            conn.UserDefinedServerCertValidationDelegate += Conn_UserDefinedServerCertValidationDelegate;
            conn.StartTls();
            conn.Bind(@$"IDIR\{account}", password);

            var filter = $"(&(objectCategory=person)(objectClass=user)({filterAttr}={value}))";
            var search = conn.Search("OU=BCGOV,DC=idir,DC=BCGOV", LdapConnection.ScopeSub, filter, new string[] { "sAMAccountName", "bcgovGUID", "givenName", "sn", "mail", "displayName", "company", "department", "memberOf" }, false);

            var entry = search.FirstOrDefault();

            if (entry == null)
                return null;

            var adAccount = new AdAccount
            {
                Username = GetAttributeStringValue(entry, "sAMAccountName"),
                UserGuid = new Guid(GetAttributeStringValue(entry, "bcgovGUID")),
                FirstName = GetAttributeStringValue(entry, "givenName"),
                LastName = GetAttributeStringValue(entry, "sn"),
                Email = GetAttributeStringValue(entry, "mail"),
                DisplayName = GetAttributeStringValue(entry, "displayName"),
                Company = GetAttributeStringValue(entry, "company"),
                Department = GetAttributeStringValue(entry, "department"),
                MemberOf = GetAttributeObjValue(entry, "memberOf"),
            };

            Console.WriteLine(JsonSerializer.Serialize(adAccount));

            return adAccount;
        }

        private bool Conn_UserDefinedServerCertValidationDelegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            if (chain.ChainElements == null)
                return false;

            return true;
        }
        private string GetAttributeStringValue(LdapEntry entry, string attribute)
        {
            return entry.GetAttributeSet().Any(x => x.Key == attribute) ? entry.GetAttribute(attribute).StringValue : "";
        }

        private List<string> GetAttributeObjValue(LdapEntry entry, string attribute)
        {
            var values = entry.GetAttributeSet().Any(x => x.Key == attribute) ? entry.GetAttribute(attribute).StringValueArray : Array.Empty<string>();

            var attValues = new List<string>();

            foreach(var value in values)
            {
                attValues.Add(Regex.Match(value, "^CN=(.+?),").Groups[1].Value);
            }

            return attValues;
        }
    }
}
