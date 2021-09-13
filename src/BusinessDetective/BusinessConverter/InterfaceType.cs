using Microsoft.CodeAnalysis;

namespace BusinessConverter
{
    internal class InterfaceType
    {
        public string Name { get; internal set; }
        public SyntaxNode FormattedFile { get; internal set; }
        public string[] DirectoryName { get; internal set; }
    }
}