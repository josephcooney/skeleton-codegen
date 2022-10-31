using System;
using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;

namespace Skeleton.Templating.TestData
{
    public class TestDataGenerator : GeneratorBase
    {
        public override List<CodeFile> Generate(Domain domain)
        {
            var files = new List<CodeFile>();
            
            var orderedTables = domain.Types.Where(t => !t.Ignore).OrderBy(t => t.Fields.Count(f => f.ReferencesType != null));
            foreach (var applicationType in orderedTables)
            {
                var file = GenerateTestData(applicationType);
                files.Add(file); 
            }

            return files;
        }

        private CodeFile GenerateTestData(ApplicationType applicationType)
        {
            var adapter = new TestDataAdapter(applicationType);
            var file = new CodeFile
            {
                Name = applicationType.Name + "_testdata.sql"
            };

            var size = applicationType.Domain.Settings.TestDataSize;

            if (applicationType.Paged)
            {
                size *= 10; // 10x for paged data
            }
            
            for (var index = 0; index < size; index++)
            {
                file.Contents += GenerateFromTemplate(adapter, "TestData");
                adapter.NewTestData();
            }
            
            return file;
        }
    }
}