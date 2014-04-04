﻿namespace VisualMutator.Model.Tests
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Exceptions;
    using Infrastructure;
    using LinqLib.Operators;
    using log4net;
    using Mutations;
    using Mutations.MutantsTree;
    using Services;
    using StoringMutants;
    using Strilanc.Value;
    using TestsTree;
    using UsefulTools.CheckboxedTree;
    using UsefulTools.ExtensionMethods;
    using Verification;

    #endregion

    public interface ITestsContainer
    {
       // TestSession LoadTests(StoredMutantInfo mutant);

        ProjectFilesClone InitTestEnvironment();


        void CancelAllTesting();

        bool VerifyMutant( StoredMutantInfo storedMutantInfo, Mutant mutant);

        StoredMutantInfo StoreMutant(Mutant changelessMutant);
     //   TestsRootNode LoadTestsPublic(IEnumerable<string> paths);

   
        void CreateTestSelections(IList<TestNodeAssembly> testAssemblies);
    }

    public class TestsContainer : ITestsContainer
    {
        private readonly IMutantsFileManager _mutantsFileManager;
        private readonly IFileSystemManager _fileManager;

        private readonly IAssemblyVerifier _assemblyVerifier;
        private readonly MutationSessionChoices _choices;

        private readonly IEnumerable<ITestService> _testServices;

        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool _allTestingCancelled;
        private bool _testsLoaded;

        private readonly TestResultTreeCreator _testResultTreeCreator;

        public TestsContainer(
            NUnitXmlTestService nunit, 
            IMutantsFileManager mutantsFileManager,
            IFileSystemManager fileManager,
            IAssemblyVerifier assemblyVerifier,
            MutationSessionChoices choices)
        {
            _mutantsFileManager = mutantsFileManager;
            _fileManager = fileManager;
            _assemblyVerifier = assemblyVerifier;
            _choices = choices;
            _testServices = new List<ITestService>
            {
                nunit//,ms
            };
            _testResultTreeCreator = new TestResultTreeCreator();
        }
       

        public void CreateTestSelections(IList<TestNodeAssembly> testAssemblies)
        {
            var testsSelector = new TestsSelector();
            foreach (var testNodeAssembly in testAssemblies)
            {
                testNodeAssembly.TestsLoadContext.SelectedTests = 
                    testsSelector.GetIncludedTests(testNodeAssembly);
            }
        }


        public void VerifyAssemblies(List<string> assembliesPaths)
        {
            foreach (var assemblyPath in assembliesPaths)
            {
                _assemblyVerifier.Verify(assemblyPath);
            }
  
        }
        public ProjectFilesClone InitTestEnvironment()
        {
            return _fileManager.CreateClone("InitTestEnvironment");
        }



        public bool VerifyMutant( StoredMutantInfo storedMutantInfo, Mutant mutant)
        {

            try
            {
                VerifyAssemblies(storedMutantInfo.AssembliesPaths);
            }
            catch (AssemblyVerificationException e)
            {

                mutant.MutantTestSession.ErrorDescription = "Mutant assembly failed verification";
                mutant.MutantTestSession.ErrorMessage = e.Message;
                mutant.MutantTestSession.Exception = e;
                mutant.State = MutantResultState.Error;
                return false;
            }
            return true;
                

        }

        public StoredMutantInfo StoreMutant( Mutant mutant)
        {
            var clone = InitTestEnvironment();
            var result = new StoredMutantInfo(clone);
            _mutantsFileManager.StoreMutant(result, mutant);
            return result;
        }


        public IEnumerable<TestsRunContext> CreateTestContexts(
            List<string> mutatedPaths,
            IList<TestNodeAssembly> testAssemblies)
        {


            foreach (var testNodeAssembly in testAssemblies)
            {
                //todo: get rid of this ungly thing
                var mutatedPath = mutatedPaths.Single(p => Path.GetFileName(p) ==
                    Path.GetFileName(testNodeAssembly.AssemblyPath));

                var originalContext = testNodeAssembly.TestsLoadContext;
                var context = new TestsRunContext();
                context.SelectedTests = originalContext.SelectedTests;
                context.AssemblyPath = mutatedPath;

                //                testNodeAssembly.TestsLoadContext.SelectedTests = _currentSession.Choices
                //                    .TestAssemblies.Single(n => testNodeAssembly.Name == n.Name)
                //                    .TestsLoadContext.SelectedTests;
                yield return context;
            }
        }

    
        public void CancelAllTesting()
        {
            _log.Info("Request to cancel all testing.");
            _allTestingCancelled = true;
           // CancelCurrentTestRun();
        }

       

        public IEnumerable<TestNodeNamespace> CreateMutantTestTree(Mutant mutant)
        {
            List<TmpTestNodeMethod> nodeMethods = mutant.TestRunContexts
                .SelectMany(c => c.TestResults.ResultMethods).ToList();

            return _testResultTreeCreator.CreateMutantTestTree(nodeMethods);
        }
    }
}