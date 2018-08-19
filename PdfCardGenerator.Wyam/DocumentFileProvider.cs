using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wyam.Common.Documents;
using Wyam.Common.IO;

namespace PdfCardGenerator.Wyam
{
    internal class DocumentFileProvider : AbstractFileProvider
    {
        private readonly Dictionary<string, IDocument> documents;

        public DocumentFileProvider(IReadOnlyList<IDocument> documents)
        {
            this.documents = documents.ToDictionary(x =>
            {
                var data = x.Metadata[global::Wyam.Common.Meta.Keys.RelativeFilePath];
                if (data is FilePath file)
                {
                    return file.FullPath;
                }
                else if (data is string str)
                {
                    return str;
                }
                else
                    throw new System.NotSupportedException($"The type {data?.GetType()} is not supported for {nameof(global::Wyam.Common.Meta.Keys.RelativeFilePath)}");
            });
        }

        public override Stream Open(string path) => this.documents[path].GetStream();
    }
}