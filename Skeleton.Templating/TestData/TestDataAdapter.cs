using System;
using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Bogus;

namespace Skeleton.Templating.TestData
{
    public class TestDataAdapter
    {
        private readonly ApplicationType _applicationType;

        public TestDataAdapter(ApplicationType applicationType)
        {
            _applicationType = applicationType;
            TestData = _applicationType.Fields.Where(f => !f.IsIntegerAssignedIdentity).Select(f => new TestDataField(f)).ToList();
        }

        public List<TestDataField> TestData { get; }

        public Name Name => _applicationType.Name;

        public void NewTestData()
        {
            foreach (var field in TestData)
            {
                field.NewTestData();
            }
        }
    }

    public class TestDataField
    {
        private Faker faker = new Faker();
        
        public TestDataField(Field f)
        {
            Field = f;
            Value = $"/* {f.ClrType} */";
            SetTestValue();
        }

        public void NewTestData()
        {
            SetTestValue();
        }
        
        public Field Field { get;  }
        
        public string Value { get; private set; }

        private void SetTestValue()
        {
            if (!Field.IsRequired && faker.Random.Bool())
            {
                Value = "null";
                return;
            }
            
            if (Field.HasReferenceType)
            {
                Value =
                    $"(select {Util.EscapeSqlReservedWord(Field.ReferencesTypeField.Name)} from {Util.EscapeSqlReservedWord(Field.ReferencesType.Name.ToString())} order by random() limit 1)";
            }
            else if (Field.IsTrackingDate)
            {
                Value = "CURRENT_TIMESTAMP";
            }
            else
            {
                if (Field.ClrType == typeof(string))
                {
                    GenerateTestString();
                } else if (Field.ClrType == typeof(int) || Field.ClrType == typeof(int?))
                {
                    GenerateTestInt();
                } else if (Field.ClrType == typeof(DateTime) || Field.ClrType == typeof(DateTime?))
                {
                    Value = Quote(faker.Date.Past(1).ToString("yyyy-MM-dd hh:mm:ss"));
                }
                else if (Field.ClrType == typeof(bool) || Field.ClrType == typeof(bool?))
                {
                    Value = faker.Random.Bool().ToString();
                }
                else if (Field.ClrType == typeof(Decimal) || Field.ClrType == typeof(Decimal?))
                {
                    Value = faker.Random.Decimal().ToString();
                }
                else if (Field.ClrType == typeof(Double) || Field.ClrType == typeof(Double?))
                {
                    Value = faker.Random.Double().ToString();
                }
            }
        }

        private void GenerateTestInt()
        {
            Value = faker.Random.Int().ToString();
        }

        private void GenerateTestString()
        {
            if (Field.Size < 10)
            {
                Value = Quote(faker.Random.String2(Field.Size.Value, Field.Size.Value));
            }
            else
            {
                if (Field.Size.HasValue)
                {
                    var size = faker.Random.Number(Field.Size.Value / 2, Field.Size.Value);
                    var value = faker.Random.Words();
                    var sizeToTake = Math.Min(size, value.Length);
                    Value = Quote(value.Substring(0, sizeToTake));
                }
                else
                {
                    if (Field.IsLargeTextContent)
                    {
                        Value = Quote(faker.Lorem.Paragraphs());
                    }
                    else
                    {
                        Value = Quote(faker.Random.Words());
                    }
                }
            }
        }

        private string Quote(string value)
        {
            return $"'{value}'";
        }
    }
}