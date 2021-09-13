using Microsoft.CodeAnalysis.MSBuild;
using System.Threading.Tasks;

namespace BusinessConverter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            MSBuildWorkspace msWorkspace = MSBuildWorkspace.Create();
            msWorkspace.LoadMetadataForReferencedProjects = true;            
        }
    }
}
