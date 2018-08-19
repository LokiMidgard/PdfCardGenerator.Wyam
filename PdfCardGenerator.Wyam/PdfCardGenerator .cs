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
        private IModule[] workingModules;

        public PdfCardGenerator(IModule firstModule, params IModule[] modules)
        {
            var m = Concat(firstModule, modules);
            this.modules = m;
        }

        private static IModule[] Concat(IModule firstModule, IModule[] modules)
        {
            var m = new IModule[(modules?.Length ?? 0) + 1];
            if ((modules?.Length ?? 0) > 0)
                Array.Copy(modules, 0, m, 1, modules.Length);
            m[0] = firstModule;
            return m;
        }

        public PdfCardGenerator WithWorkingDirectory(string path)
        {
            if (this.workingDirectory != null)
                throw new InvalidOperationException($"You can't use {nameof(WithDocumentsAsWorkingDirectory)} and {nameof(WithWorkingDirectory)} together");
            this.workingDirectory = path;
            return this;
        }

        public PdfCardGenerator WithDocumentsAsWorkingDirectory(IModule firstModule, params IModule[] modules)
        {
            if (this.workingDirectory != null)
                throw new InvalidOperationException($"You can't use {nameof(WithDocumentsAsWorkingDirectory)} and {nameof(WithWorkingDirectory)} together");
            this.workingModules = Concat(firstModule, modules);
            return this;
        }

        public IEnumerable<IDocument> Execute(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            var templates = context.Execute(this.modules);
            foreach (var template in templates)
            {
                using (var templateStream = template.GetStream())
                {

                    AbstractFileProvider fileProvider;
                    if (this.workingDirectory != null)
                        fileProvider = new WorkingDirectoryFileProvider(new DirectoryInfo(this.workingDirectory));
                    else if (this.workingModules != null)
                        fileProvider = new DocumentFileProvider(context.Execute(this.workingModules));
                    else
                        fileProvider = new WorkingDirectoryFileProvider(new DirectoryInfo(Environment.CurrentDirectory));

                    var project = Project.Load(templateStream, fileProvider);

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
