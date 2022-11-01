using System;
using System.IO.Abstractions;
using Skeleton.Model;
using Moq;
using Newtonsoft.Json.Linq;
using Skeleton.Model.NamingConventions;

namespace Skeleton.Tests
{
    public class TestUtil
    {
        public const string TestNamespace = "TestNs";

        public static Domain CreateTestDomain(IFileSystem fs)
        {
            var mockTypeProvider = new Mock<ITypeProvider>();
            var domain = new Domain(new Settings(fs), mockTypeProvider.Object, new SnakeCaseNamingConvention(null));
            var userType = new ApplicationType("user", TestNamespace, domain);
            var userIdField = new Field(userType) { Name = "id", ClrType = typeof(int), ProviderTypeName = "integer", IsKey = true, IsRequired = true };
            userType.Fields.Add(userIdField);
            userType.Fields.Add(new Field(userType) {Name= "name", ClrType = typeof(string), ProviderTypeName = "text", IsRequired = true });
            userType.Attributes = JToken.Parse("{'isSecurityPrincipal':true}");
            domain.Types.Add(userType);

            var customerType = new ApplicationType("customer", TestNamespace, domain);
            var customerIdField = new Field(customerType) { Name = "id", ClrType = typeof(int), IsKey = true, ProviderTypeName = "integer", IsRequired = true };
            customerType.Fields.Add(customerIdField);
            customerType.Fields.Add(new Field(customerType){Name = "name", ClrType = typeof(string), IsRequired = true, ProviderTypeName = "text"});
            customerType.Fields.Add(new Field(customerType){Name = "created_by", ClrType = typeof(int), ProviderTypeName = "integer", IsRequired = true, ReferencesType = userType, ReferencesTypeField = userIdField});
            domain.Types.Add(customerType);

            var orderType = new ApplicationType("order", TestNamespace, domain);
            var orderIdField = new Field(orderType){Name = "id", ClrType = typeof(System.Guid), ProviderTypeName = "uuid", IsKey = true, IsRequired = true};
            orderType.Fields.Add(orderIdField);
            orderType.Fields.Add(new Field(orderType){Name = Field.CreatedFieldName, ClrType = typeof(DateTime), ProviderTypeName = "timestamp with time zone", IsRequired = true});
            orderType.Fields.Add(new Field(orderType) { Name = "delivery_instructions", ClrType = typeof(string), ProviderTypeName = "text", IsRequired = false });
            orderType.Fields.Add(new Field(orderType) { Name = "customer_id", ClrType = typeof(int), ProviderTypeName = "integer", IsRequired = true, ReferencesType = customerType, ReferencesTypeField = customerIdField});
            domain.Types.Add(orderType);

            var orderLineType = new ApplicationType("order_line", TestNamespace, domain);
            var orderLineIdField = new Field(orderLineType){Name = "id", ClrType = typeof(System.Guid), ProviderTypeName = "uuid", IsKey = true, IsRequired = true};
            orderLineType.Fields.Add(orderLineIdField);
            orderLineType.Fields.Add(new Field(orderLineType){Name = "description", ClrType = typeof(string), IsRequired = false, IsKey = false, ProviderTypeName = "text"});
            orderLineType.Fields.Add(new Field(orderLineType) { Name = "order_id", ClrType = typeof(Guid), ProviderTypeName = "uuid", IsRequired = true, ReferencesType = orderType, ReferencesTypeField = orderIdField });
            domain.Types.Add(orderLineType);

            return domain;
        }
    }
}
