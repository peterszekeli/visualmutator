﻿namespace PiotrTrzpil.VisualMutator_VSPackage.Model.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Reactive;
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Core;

    using PiotrTrzpil.VisualMutator_VSPackage.Infrastructure;
    using PiotrTrzpil.VisualMutator_VSPackage.Infrastructure.WpfUtils;
    using PiotrTrzpil.VisualMutator_VSPackage.Model.Mutations;

    public interface ITestsContainer
    {
        IEnumerable<TestNodeNamespace> LoadTests(IEnumerable<string> assemblies);

        void RunTests();

        TestsRootNode TestsRootNode { get; }
    }

    public class TestsContainer : ITestsContainer
    {
        private readonly IEnumerable<ITestService> _testServices;

        private TestsRootNode _testsRootNode;

        public TestsRootNode TestsRootNode
        {
            get
            {
                return _testsRootNode;
            }
        }
    

        public TestsContainer(NUnitTestService nunit, MsTestService ms)
        {
            _testServices = new List<ITestService>
            {
                nunit,ms
            };

           
        }

        public void Initialize()
        {
           

        }

        

        public IEnumerable<TestNodeNamespace> LoadTests(IEnumerable<string> assemblies)
        {
            _testsRootNode = new TestsRootNode();

            IEnumerable<TestNodeNamespace> namespaces =
                _testServices.AsParallel()
                .SelectMany(s => s.LoadTests(assemblies))
                .GroupBy(classNode => classNode.Namespace)
                .Select(group =>
                {
                    var ns = new TestNodeNamespace(_testsRootNode, group.Key);
                    group.Each(c => c.Parent = ns);
                    ns.Children.AddRange(group);
                    return ns;

                }).ToList();
            //Note: ToList() is important! Lack of it causes somehow to duplicate TestNodeNamespace objects..
            
            _testsRootNode.Children.AddRange(namespaces);
          
            _testsRootNode.State = TestNodeState.Inactive;

            return namespaces;
        }

        public void RunTests()
        {   
            _testsRootNode.State = TestNodeState.Running;
            Parallel.ForEach(_testServices, service =>
            {
                service.RunTests();
            });


        }
    }
}