using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Common.Modules;

namespace PdfCardGenerator.Wyam
{
    public class PdfCardGenerator : IModule
    {
        private readonly IModule[] modules;
        private string workingDirectory;

        public PdfCardGenerator(IModule firstModule, params IModule[] modules)
        {
            this.modules = new IModule[(modules?.Length ?? 0) + 1];
            if ((modules?.Length ?? 0) > 0)
                Array.Copy(modules, 0, this.modules, 1, modules.Length);
            this.modules[0] = firstModule;
        }

        public PdfCardGenerator WithWorkingDirectory(string path)
        {
            this.workingDirectory = path;

            return this;
        }

        public IEnumerable<IDocument> Execute(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            var templates = context.Execute(this.modules);
            foreach (var template in templates)
            {
                using (var templateStream = template.GetStream())
                {
                    var project = Project.Load(templateStream, new DirectoryInfo(this.workingDirectory ?? Environment.CurrentDirectory));

                    foreach (var input in inputs)
                    {
                        using (var inputStream = input.GetStream())
                        {
                            var xml = XDocument.Load(inputStream);
                            var pdf = project.GetDocuments(xml);

                            var memoryStream = new MemoryStream();
                            pdf.Save(memoryStream);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            yield return context.GetDocument(input, memoryStream);
                        }
                    }


                }
            }
        }

    }
}
