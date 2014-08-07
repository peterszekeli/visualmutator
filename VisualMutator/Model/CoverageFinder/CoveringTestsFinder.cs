﻿namespace VisualMutator.Model.CoverageFinder
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Exceptions;
    using log4net;
    using Microsoft.Cci;
    using Mutations.Types;
    using UsefulTools.ExtensionMethods;

    public class CoveringTestsFinder : ICoveringTestsFinder
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Task<List<MethodIdentifier>> FindCoveringTests(List<CciModuleSource> modules, ICodePartsMatcher matcher)
        {
            return Task.WhenAll(modules.Select(module => 
                    module.Modules.Select(m => 
                    Task.Run(() => FindCoveringTests(m, matcher)))).Flatten())
                        .ContinueWith(t => t.Result.Flatten().ToList());
        }

        private List<MethodIdentifier> FindCoveringTests(IModuleInfo module, ICodePartsMatcher targetsMatcher)
        {
            _log.Debug("Scanning " + module.Name + " for selected covering tests. ");
            var visitor = new CoveringTestsVisitor(targetsMatcher);

            var traverser = new CodeTraverser
            {
                PreorderVisitor = visitor
            };

            traverser.Traverse(module.Module);
            _log.Debug("Finished scanning module"+module.Name + ". Found " + visitor.FoundTests.Count+
                ". Scanned total: " + visitor.ScannedMethods + " methods and "+
                visitor.ScannedMethodCalls+" method calls.");

            _log.Debug("Listing found tests: ");
            foreach (var methodIdentifier in visitor.FoundTests)
            {
                _log.Debug("Test: "+ methodIdentifier);
            }

            if (visitor.IsChoiceError)
            {
                throw new TestWasSelectedToMutateException();
            }
            return visitor.FoundTests.ToList();
        } 
    }

    public interface ICoveringTestsFinder
    {
        Task<List<MethodIdentifier>> FindCoveringTests(List<CciModuleSource> modules, ICodePartsMatcher matcher);
    }
}