﻿using System;
using System.Collections.Generic;
using System.Reflection;
using HandlebarsDotNet;
using Skeleton.Model;
using Serilog;

namespace Skeleton.Templating
{
    public abstract class GeneratorBase
    {
        public abstract List<CodeFile> Generate(Domain domain);
        private Dictionary<string, Func<object, string>> compiledTemplates = new Dictionary<string, Func<object, string>>();
        public abstract Assembly Assembly { get; }
        
        protected string GenerateFromTemplate(object model, string templateName)
        {
            Func<object, string> template = null;

            if (compiledTemplates.ContainsKey(templateName))
            {
                template = compiledTemplates[templateName];
            }
            else
            {
                template = Util.GetCompiledTemplate(templateName, Assembly);
                compiledTemplates.Add(templateName, template);
            }

            try
            {
                return template(model);
            }
            catch (Exception)
            {
                Log.Fatal("Error applying template {TemplateName} with model {Model}", templateName, model);
                throw;
            }
        }

        protected string FormatRelativePath(ApplicationType type, string path)
        {
            if (path.IndexOf("{") > -1)
            {
                var template = Handlebars.Compile(path);
                return template(type);
            }

            return path;
        }
    }
}
