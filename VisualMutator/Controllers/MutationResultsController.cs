﻿namespace VisualMutator.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using CommonUtilityInfrastructure;
    using CommonUtilityInfrastructure.WpfUtils;

    using VisualMutator.Infrastructure.Factories;
    using VisualMutator.Model.Mutations;
    using VisualMutator.Model.Mutations.Types;
    using VisualMutator.Model.Tests;
    using VisualMutator.ViewModels;

    public class ExecutedOperator : ModelElement
    {
        public List<Mutant> Mutants { get; set; }

        private MutantResultState _resultState;

        public MutantResultState ResultState
        {
            get
            {
                return _resultState;
            }
            set
            {
                SetAndRise(ref _resultState, value, () => ResultState);
            }
        }

        public string Name
        {
            get; set;
        }
    }

    public class MutationResultsController : Controller
    {
        private readonly MutationResultsViewModel _viewModel;

        private readonly IFactory<MutantsCreationController> _mutantsCreationFactory;

        private readonly IMutantsContainer _mutantsContainer;

        private readonly ITestsContainer _testsContainer;

        private readonly Services _services;

        public MutationResultsController(
            MutationResultsViewModel viewModel,
            IFactory<MutantsCreationController> mutantsCreationFactory,
            IMutantsContainer mutantsContainer,
            ITestsContainer testsContainer,
            Services services)
        {
            _viewModel = viewModel;
            _mutantsCreationFactory = mutantsCreationFactory;
            _mutantsContainer = mutantsContainer;
            _testsContainer = testsContainer;
            _services = services;

            _viewModel.CommandCreateNewMutants = new BasicCommand(CreateMutants,
                () => !_viewModel.AreOperationsOngoing);
            _viewModel.CommandCreateNewMutants.UpdateOnChanged(_viewModel, () => _viewModel.AreOperationsOngoing);

            _viewModel.CommandStop = new BasicCommand(Stop, () => _viewModel.AreOperationsOngoing);
            _viewModel.CommandStop.UpdateOnChanged(_viewModel, () => _viewModel.AreOperationsOngoing);

            _viewModel.Operators = new BetterObservableCollection<ExecutedOperator>();
        }

       
        

        public void CreateMutants()
        {
            var mutantsCreationController = _mutantsCreationFactory.Create();
            mutantsCreationController.Run();
            
            if (mutantsCreationController.HasResults)
            {
                MutationSessionChoices choices = mutantsCreationController.Result;

                _viewModel.AreOperationsOngoing = true;
                _viewModel.OperationsStateDescription = "Creating mutants...";


                var tasks = choices.SelectedOperators.Select(op =>
                {
                    return _services.Threading.ScheduleAsync(() =>
                        _mutantsContainer.GenerateMutantsForOperator(choices, op),
                        operatorWithMutants => _viewModel.Operators.Add(operatorWithMutants));

                }).ToArray();


                _services.Threading.ContinueOnGuiWhenAll(tasks, () => RunTests());
            }

           
        }


        public void RunTests()
        {
            _viewModel.OperationsStateDescription = "Running tests...";

            var allMutants = _viewModel.Operators.SelectMany(op => op.Mutants).ToList();
            _viewModel.InitTestingProgress(allMutants.Count);
            _services.Threading.ScheduleAsync(() =>
            {
                foreach (var mutant in allMutants)
                {
                    _testsContainer.RunTestsForMutant(mutant);
                    _viewModel.UpdateTestingProgress();
                }
            }, () =>
            {
                _viewModel.OperationsStateDescription = "Finished";
                _viewModel.AreOperationsOngoing = false;
            });
            //var tasks = _viewModel.Operators.SelectMany(op => op.Mutants).Select(mut =>
            //{
            //    return _services.Threading.ScheduleAsyncSequential(() => _testsContainer.RunTestsForMutant(mut));

            //}).ToArray();

            //_services.Threading.ContinueOnGuiWhenAll(tasks, () =>
            //{
            //    _viewModel.OperationsStateDescription = "Finished";
            //    _viewModel.AreOperationsOngoing = false;
            //});

            
        }

        public void Stop()
        {

        }

        public void Initialize()
        {
            _viewModel.IsVisible = true;
        }

        public void Deactivate()
        {
            Stop();
            Clean();
            _viewModel.IsVisible = false;
        }

        private void Clean()
        {
            _viewModel.Operators.Clear();
        }

        public MutationResultsViewModel ViewModel
        {
            get
            {
                return _viewModel;
            }
        }
    }
}