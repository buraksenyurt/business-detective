using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessConverter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            MSBuildWorkspace msWorkspace = MSBuildWorkspace.Create();
            msWorkspace.LoadMetadataForReferencedProjects = true;

            var solutionPath = @"C:\Users\burak\source\repos\business-detective\src\BusinessDetective\BusinessDetective.sln";
            var solution = await msWorkspace.OpenSolutionAsync(solutionPath);

            Project project = solution.Projects.FirstOrDefault(i => i.Name == "BusinessLibrary");
            Project projectServiceContract = solution.Projects.FirstOrDefault(i => i.Name == "BusinessContracts");
            var contractRootPath = Path.GetDirectoryName(projectServiceContract.FilePath);
            var counter = 1;
            StringBuilder svcBuilder = new StringBuilder();
            StringBuilder reporter = new StringBuilder();
            List<InterfaceType> interfaceDocuments = new List<InterfaceType>();

            #region Find Interfaces

            foreach (DocumentId docId in project.DocumentIds)
            {
                Document doc = project.GetDocument(docId);

                if (doc.Name.StartsWith("BC"))
                {
                    Console.WriteLine($"File :{counter}\t{doc.FilePath}");
                    counter++;
                    var syntaxTree = doc.GetSyntaxTreeAsync().Result;
                    var documentRoot = syntaxTree.GetRoot();

                    var classess = documentRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                    if (classess.Count > 1)
                    {
                        reporter.AppendLine($"Birden fazla sınıf içeren dosya: {doc.FilePath}");
                        Console.WriteLine($"\t\tMore than one class{doc.FilePath}");
                    }
                    foreach (var innerClass in classess)
                    {
                        if (innerClass.Identifier.ToFullString().Contains("DependencyInjection")
                            || innerClass.Identifier.ToFullString().Contains("BCCommon"))
                            continue;

                        if (innerClass.BaseList != null && innerClass.BaseList.Types[0].ToString() == "BCCommon")
                        {
                            StringBuilder interfaceFileBuilder = new StringBuilder();
                            StringBuilder namespaceBuilder = new StringBuilder();

                            #region Find and Add Usings

                            var usings = ((CompilationUnitSyntax)documentRoot).Usings;
                            namespaceBuilder.Append(usings.ToFullString()
                                .Replace("#region Namespaces\r\n", "")
                                .Replace("#region Usings\r\n", ""));

                            if (!usings.ToFullString().Contains("using System.ServiceModel;"))
                                namespaceBuilder.AppendLine("using System.ServiceModel;");

                            var usingSection = namespaceBuilder.ToString();
                            interfaceFileBuilder.AppendLine(usingSection);

                            #endregion

                            #region Find and Add Root Namespace

                            var nsName = documentRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToList()[0].Name.ToString();
                            interfaceFileBuilder.AppendLine($"namespace {nsName}");
                            interfaceFileBuilder.AppendLine("{");

                            #endregion

                            #region Create Interface name

                            interfaceFileBuilder.AppendLine("[ServiceContract]");
                            interfaceFileBuilder.AppendLine($"public interface I{innerClass.Identifier}");
                            interfaceFileBuilder.AppendLine("{");

                            #endregion

                            #region Create interface methods

                            var methods = innerClass.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
                            foreach (var method in methods)
                            {
                                if (method.Modifiers.ToString().Contains("static"))
                                    continue;

                                if (method.Modifiers.ToString().Contains("public"))
                                {
                                    string methodSyntax = string.Empty;
                                    if (method.ParameterList.ToFullString().Contains("T ")
                                        || method.ParameterList.ToFullString().Contains("<T>")
                                        )
                                    {
                                        methodSyntax = $"{method.ReturnType} {method.Identifier}<T>{method.ParameterList};";
                                    }
                                    else
                                    {
                                        methodSyntax = $"{method.ReturnType} {method.Identifier}{method.ParameterList};";
                                    }
                                    interfaceFileBuilder.AppendLine("[OperationContract]");
                                    interfaceFileBuilder.AppendLine(methodSyntax);
                                }
                            }

                            #endregion

                            interfaceFileBuilder.AppendLine("}");
                            interfaceFileBuilder.AppendLine("}");
                            var newFile = CSharpSyntaxTree.ParseText(interfaceFileBuilder.ToString()).GetRoot().NormalizeWhitespace();
                            var formattedFile = Formatter.Format(newFile, new AdhocWorkspace());

                            var interfaceType = new InterfaceType
                            {
                                Name = $"I{innerClass.Identifier}",
                                FormattedFile = formattedFile,
                                DirectoryName = new string[] { Path.Combine(contractRootPath, Path.Combine(doc.Folders.ToArray())) }
                            };
                            interfaceDocuments.Add(interfaceType);
                            reporter.AppendLine($"{innerClass.Identifier}->I{innerClass.Identifier}");
                        }
                        else
                        {
                            reporter.AppendLine($"{innerClass.Identifier} BaseList NULL");
                            Console.WriteLine($"{innerClass.Identifier} BaseList NULL");
                        }
                    }
                }
            }

            #endregion

            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "InterfaceCreateReport.txt"), reporter.ToString());

            msWorkspace.CloseSolution();
        }
    }
}
