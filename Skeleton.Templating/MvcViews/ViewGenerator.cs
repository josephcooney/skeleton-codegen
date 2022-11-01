using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Templating.DatabaseFunctions;

namespace Skeleton.Templating.MvcViews
{
    public class ViewGenerator
    {
        public List<CodeFile> Generate(Domain domain)
        {
            Util.RegisterHelpers(domain);

            var files = new List<CodeFile>();

            foreach (var type in domain.Types)
            {
                if (type.GenerateUI)
                {
                    var index = new CodeFile { Name = "index.cshtml", Contents = GenerateIndex(type, domain), RelativePath = "Views\\" + Util.CSharpNameFromName(type.Name) };
                    files.Add(index);

                    var edit = new CodeFile { Name = "edit.cshtml", Contents = GenerateEdit(type, domain), RelativePath = "Views\\" + Util.CSharpNameFromName(type.Name) };
                    files.Add(edit);

                }
            }

            return files;
        }

        private string GenerateIndex(ApplicationType type, Domain domain)
        {
            var forDisplay = domain.ResultTypes.FirstOrDefault(r => r.Operations.Any(o => o.Name == type.Name + "_" + DbFunctionGenerator.SelectAllForDisplayFunctionName));
            var indexAdapter = new IndexViewAdapter(type, forDisplay, domain);
            return GenerateFromTemplate(indexAdapter, "IndexTemplate");
        }

        private string GenerateEdit(ApplicationType type, Domain domain)
        {
            return GenerateFromTemplate(new ViewAdapter(type, domain), "EditTemplate");
        }

        private string GenerateFromTemplate(ViewAdapter adapter, string templateName)
        {
            var template = Util.GetCompiledTemplate(templateName);
            return template(adapter);
        }
    }
}
