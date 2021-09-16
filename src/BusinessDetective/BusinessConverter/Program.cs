using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessConverter
{
    //TODO@Burak Refactore yapalım
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static async Task Main(string[] args)
        {
            MSBuildWorkspace msWorkspace = MSBuildWorkspace.Create();
            msWorkspace.LoadMetadataForReferencedProjects = true;
            log.Info("Workspace created.");

            //TODO Komut satırından alalım
            var solutionPath = ConfigurationManager.AppSettings["SolutionPath"];
            var businessLibName = ConfigurationManager.AppSettings["BusinessProjectName"];
            var businessContractName = ConfigurationManager.AppSettings["BusinessContractProjectName"];
            var solution = await msWorkspace.OpenSolutionAsync(solutionPath);
            log.Info("Solution opened");

            var project = solution.Projects.FirstOrDefault(i => i.Name == businessLibName);
            var projectServiceContract = solution.Projects.FirstOrDefault(i => i.Name == businessContractName);
            var contractRootPath = Path.GetDirectoryName(projectServiceContract.FilePath);
            var counter = 1;
            var svcBuilder = new StringBuilder();
            var interfaceDocuments = new List<InterfaceType>();

            #region Find Interfaces

            log.Info("Find Interfaces phase started.");

            foreach (DocumentId docId in project.DocumentIds)
            {
                log.Info($"Project:{project.Name}.Document Count:{project.Documents.Count()}");
                var doc = project.GetDocument(docId);

                if (doc.Name.StartsWith("BC")) // Arama desenini parametrik alabilir miyiz?
                {
                    Console.WriteLine($"File :{counter}\t{doc.FilePath}");
                    counter++;
                    var syntaxTree = doc.GetSyntaxTreeAsync().Result;
                    var documentRoot = syntaxTree.GetRoot();

                    var classess = documentRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                    if (classess.Count > 1) // Birden fazla sınıf varsa nasıl bir strateji izlenmeli?
                    {
                        log.Warn($"There is file with multiple classes.{doc.FilePath}");
                    }
                    foreach (var innerClass in classess)
                    {
                        //TODO Bu kısmı bir yasaklılar listesinden kontrol ettirelim. Uygulama dışından gelsin.
                        if (innerClass.Identifier.ToFullString().Contains("DependencyInjection")
                            || innerClass.Identifier.ToFullString().Contains("BCCommon"))
                            continue;

                        if (innerClass.BaseList != null && innerClass.BaseList.Types[0].ToString() == "BCCommon")
                        {
                            StringBuilder interfaceFileBuilder = new StringBuilder();
                            StringBuilder namespaceBuilder = new StringBuilder();
                            StringBuilder endPointBuilder = new StringBuilder();

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
                                if (method.Modifiers.ToString().Contains("static")) //static harici hangi metotları atlatmak lazım. Mesela private olanlar?
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

                            #region Create ServiceEndPoints

                            endPointBuilder.AppendLine($"<service behaviorConfiguration=\"\" name=\"{nsName}.{innerClass.Identifier}\">");
                            endPointBuilder.AppendLine($"\t<endpoint address=\"\" binding=\"basicHttpsBinding\" contract=\"{nsName}.I{innerClass.Identifier}\"/>");
                            endPointBuilder.AppendLine($"\t<endpoint address=\"mex\" binding=\"mexHttpsBinding\" contract=\"IMetadataExchange\"/>");
                            endPointBuilder.AppendLine("</service>");
                            svcBuilder.Append(endPointBuilder.ToString());

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
                            log.Info($"Change.{innerClass.Identifier} to I{innerClass.Identifier}");
                        }
                        else
                        {
                            log.Warn($"BaseList is NULL.{innerClass.Identifier}");
                        }
                    }
                }
            }

            log.Info("Find Interfaces phase completed.");
            var changedSln = project.Solution;

            #endregion
            log.Info("Implement Interface to BusinessClass phase started.");

            #region Implement Interfaces to BusinessClass

            var changedProject = changedSln.Projects.FirstOrDefault(i => i.Name == businessLibName);
            counter = 1;

            foreach (DocumentId docId in changedProject.DocumentIds)
            {
                var doc = changedProject.GetDocument(docId);
                if (doc.Name.StartsWith("BC"))
                {
                    Console.WriteLine($"Changing:{counter}\t{doc.FilePath}");
                    counter++;
                    var syntaxTree = doc.GetSyntaxTreeAsync().Result;
                    var documentRoot = syntaxTree.GetRoot();
                    foreach (var innerClass in documentRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList())
                    {
                        if (innerClass.Identifier.ToFullString().Contains("DependencyInjection")
                            || innerClass.Identifier.ToFullString().Contains("BCCommon")
                            )
                            continue;

                        if (innerClass.BaseList != null &&
                            innerClass.BaseList.Types.Count == 1 &&
                            innerClass.BaseList.Types[0].ToString() == "BCCommon"
                            )
                        {
                            var searching = $"public class {innerClass.Identifier} : BCCommon";
                            var changing = $"public class {innerClass.Identifier} : BCCommon,I{innerClass.Identifier}";
                            var changed = documentRoot.GetText().ToString().Replace(searching, changing);
                            documentRoot = CSharpSyntaxTree.ParseText(changed).GetRoot();
                            var newDoc = doc.WithText(documentRoot.GetText());
                            changedProject = newDoc.Project;
                        }
                    }
                }
            }

            var lastSln = changedProject.Solution;

            #endregion

            log.Info("Implement Interface to BusinessClass phase completed.");

            log.Info("Create Interfaces phase started.");

            #region Create Interfaces on BusinessContracts

            var contractProject = lastSln.Projects.FirstOrDefault(i => i.Name == businessContractName);
            foreach (var doc in interfaceDocuments)
            {
                var added = contractProject.AddDocument(doc.Name, doc.FormattedFile, doc.DirectoryName);
                contractProject = added.Project;
            }

            var contractedSln = contractProject.Solution;
            msWorkspace.TryApplyChanges(contractedSln);
            msWorkspace.CloseSolution();

            #endregion

            log.Info("Create Interfaces phase completed.");

            var endpointFile = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.AppSettings["EndpointsFileName"]);
            File.WriteAllText(endpointFile, svcBuilder.ToString());
            log.Info($"Endpoints file created.{endpointFile}");
        }
    }
}
